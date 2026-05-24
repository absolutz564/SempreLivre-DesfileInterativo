using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Video;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class FinalScreenController : MonoBehaviour
{
    [Header("Preview do Vídeo")]
    public VideoPlayer videoPlayer;
    public RawImage videoDisplay;
    public RenderTexture videoRenderTexture;

    [Header("QR Code")]
    public GameObject qrCodePanel;
    public RawImage qrCodeDisplay;

    [Header("Estados de UI")]
    public GameObject loadingPanel;
    public GameObject contentPanel;

    [Header("Configuração do Servidor")]
    public string serverUrl;
    public string uploadEndpoint;
    public string redirectLink;
    public string token;
    public string accessToken;

    // ---------------------------------------------------------

    private string base64QRCode;

    void Start()
    {
        loadingPanel.SetActive(true);
        contentPanel.SetActive(false);
        qrCodePanel.SetActive(false);

        LoadCredentials();

        string videoPath = Path.Combine(Application.persistentDataPath, "capture.mp4");

        // Se o FFmpeg estiver salvando em subpasta, ajuste aqui:
        // string videoPath = Path.Combine(Application.persistentDataPath, "ExportedVideos", "capture.mp4");

        StartCoroutine(WaitThenProcess(videoPath));
    }

    IEnumerator WaitThenProcess(string videoPath)
    {
        yield return new WaitForSeconds(1f);

        // Aguarda o arquivo existir (timeout de 30s para não travar infinito)
        float waitTime = 0f;
        while (!File.Exists(videoPath) && waitTime < 30f)
        {
            Debug.Log("Aguardando arquivo de vídeo em: " + videoPath);
            yield return new WaitForSeconds(0.5f);
            waitTime += 0.5f;
        }

        if (!File.Exists(videoPath))
        {
            Debug.LogError("Arquivo de vídeo não encontrado após 30s: " + videoPath);
            loadingPanel.SetActive(false);
            contentPanel.SetActive(true);
            yield break;
        }

        // Aguarda o arquivo não estar mais em uso pelo FFmpeg
        bool accessible = false;
        while (!accessible)
        {
            bool locked = false;
            try
            {
                using (var fs = File.Open(videoPath, FileMode.Open, FileAccess.Read, FileShare.None)) { }
                accessible = true;
            }
            catch (IOException)
            {
                locked = true;
            }

            if (locked)
                yield return new WaitForSeconds(1f);
        }

        loadingPanel.SetActive(false);
        contentPanel.SetActive(true);

        InitVideoPreview(videoPath);

        if (!string.IsNullOrEmpty(serverUrl))
            StartCoroutine(UploadVideo(videoPath));
        else
            Debug.Log("serverUrl não configurada — upload ignorado.");
    }

    void LoadCredentials()
    {
        token = PlayerPrefs.GetString("token", "");
        accessToken = PlayerPrefs.GetString("accessToken", "");

        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(accessToken))
            Debug.LogWarning("Credenciais ausentes no PlayerPrefs — verifique se o TokenLoader rodou antes desta cena.");
    }

    void InitVideoPreview(string videoPath)
    {
        if (videoRenderTexture == null)
            videoRenderTexture = new RenderTexture(1080, 1920, 0);

        videoDisplay.texture = videoRenderTexture;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = videoRenderTexture;
        videoPlayer.source = VideoSource.Url;

        // Uri.AbsoluteUri codifica espaços automaticamente (ex: "Jailson S" → "Jailson%20S")
        videoPlayer.url = new Uri(videoPath).AbsoluteUri;

        videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
        videoPlayer.isLooping = true;

        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.errorReceived += (vp, msg) => Debug.LogError("VideoPlayer error: " + msg);
        videoPlayer.Prepare();
    }

    void OnVideoPrepared(VideoPlayer vp)
    {
        vp.prepareCompleted -= OnVideoPrepared;
        vp.Play();
    }

    IEnumerator UploadVideo(string videoPath)
    {
        byte[] videoBytes = File.ReadAllBytes(videoPath);

        WWWForm form = new WWWForm();
        form.AddField("isFileIdentify", "false");
        form.AddField("identify", "false");
        form.AddField("url_redirect", redirectLink);
        form.AddBinaryData("file", videoBytes, "capture.mp4", "video/mp4");

        string fullUrl = serverUrl + uploadEndpoint;
        Debug.Log("Enviando vídeo para: " + fullUrl);

        using (UnityWebRequest req = UnityWebRequest.Post(fullUrl, form))
        {
            req.SetRequestHeader("Authorization", "Bearer " + token);
            req.SetRequestHeader("access_token", accessToken);

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Erro no upload: {req.error} | {req.downloadHandler?.text}");
            }
            else
            {
                Debug.Log("Upload concluído: " + req.downloadHandler.text);
                ParseAndShowQRCode(req.downloadHandler.text);
            }
        }
    }

    void ParseAndShowQRCode(string json)
    {
        QRCodeData data = JsonUtility.FromJson<QRCodeData>(json);
        if (data == null || string.IsNullOrEmpty(data.qrcode))
        {
            Debug.LogError("QR code não encontrado na resposta.");
            return;
        }

        string raw = data.qrcode;
        base64QRCode = raw.Contains(",") ? raw.Substring(raw.IndexOf(',') + 1) : raw;
        StartCoroutine(DisplayQRCode());
    }

    IEnumerator DisplayQRCode()
    {
        byte[] bytes = Convert.FromBase64String(base64QRCode);
        Texture2D tex = new Texture2D(1, 1);
        tex.LoadImage(bytes);

        qrCodeDisplay.texture = tex;
        qrCodePanel.SetActive(true);
        yield return null;
    }

    // Botão "Reiniciar" / "Obrigado"
    public void OnRestartClick()
    {
        SceneManager.LoadScene(0);
    }

    // ---------------------------------------------------------

    [Serializable]
    public class QRCodeData
    {
        public string qrcode;
        public string image;
    }
}
