using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FfmpegUnity;
using Nexweron.WebCamPlayer;
using UnityEngine.SceneManagement;

public class RecordingController : MonoBehaviour
{
    [Header("Webcam")]
    public WebCamStream cameraStream;

    [Header("Molduras — CanvasGravacao (exibição ao jogador, Overlay)")]
    public GameObject[] molduraOverlays;

    [Header("Molduras — CanvasCaptura (captura FFmpeg, oculto)")]
    public GameObject[] molduraCaptureOverlays;

    [Header("Fase: Posicionamento")]
    public GameObject positioningPanel;

    [Header("Fase: Contagem")]
    public GameObject countdownPanel;
    public TextMeshProUGUI countdownText;

    [Header("Fase: Gravação")]
    public GameObject recordingPanel;
    public Slider progressSlider;
    public GameObject recordingIcon;

    [Header("FFmpeg")]
    public FfmpegCaptureCommand ffmpegCapture;

    [Header("Configuração")]
    public int videoDuration = 10;

    // ---------------------------------------------------------

    void Start()
    {
        cameraStream.Play();

        int idx = GameData.Instance != null ? GameData.Instance.SelectedMolduraIndex : 0;
        ActivateMoldura(idx);

        SetPhase(positioningPanel, true);
        SetPhase(countdownPanel, false);
        SetPhase(recordingPanel, false);
        recordingIcon.SetActive(false);

        DeleteOldCapture();
    }

    // Botão "Iniciar" na tela de posicionamento
    public void OnIniciarClick()
    {
        SetPhase(positioningPanel, false);
        StartCoroutine(CountdownThenRecord());
    }

    IEnumerator CountdownThenRecord()
    {
        SetPhase(countdownPanel, true);

        for (int i = 3; i > 0; i--)
        {
            countdownText.text = i.ToString();
            yield return new WaitForSeconds(1f);
        }

        countdownText.text = "";
        SetPhase(countdownPanel, false);

        SetPhase(recordingPanel, true);
        recordingIcon.SetActive(true);
        progressSlider.value = 0f;

        StartCoroutine(StartFFmpegNextFrame());

        float elapsed = 0f;
        while (elapsed < videoDuration)
        {
            elapsed += Time.deltaTime;
            progressSlider.value = Mathf.Clamp01(elapsed / videoDuration);
            yield return null;
        }

        recordingIcon.SetActive(false);
        StartCoroutine(StopFFmpegAndProceed());
    }

    IEnumerator StartFFmpegNextFrame()
    {
        yield return null;
        ffmpegCapture.PrintStdErr = true;
        ffmpegCapture.StartFfmpeg();
        Debug.Log("[Recording] FFmpeg iniciado. Opções: " + ffmpegCapture.CaptureOptions);
    }

    IEnumerator StopFFmpegAndProceed()
    {
        Debug.Log("[Recording] Parando gravação...");

        ffmpegCapture.StopFfmpeg();

        float timeout = 0f;
        while (ffmpegCapture.IsRunning && timeout < 15f)
        {
            timeout += Time.deltaTime;
            yield return null;
        }

        Debug.Log("[Recording] FFmpeg finalizado.");

        string videoPath = Path.Combine(Application.persistentDataPath, "capture.mp4").Replace('\\', '/');

        while (!File.Exists(videoPath))
        {
            Debug.LogWarning("[Recording] Aguardando arquivo...");
            yield return new WaitForSeconds(0.5f);
        }

        Debug.Log("[Recording] Arquivo encontrado.");

        bool ready = false;
        while (!ready)
        {
            bool shouldWait = false;
            try
            {
                using (FileStream fs = File.Open(videoPath, FileMode.Open, FileAccess.Read, FileShare.None)) { }
                ready = true;
            }
            catch (IOException)
            {
                shouldWait = true;
            }

            if (!ready && shouldWait)
                yield return new WaitForSeconds(1f);
        }
        cameraStream.Stop();
        Debug.Log($"[Recording] Arquivo pronto ({new FileInfo(videoPath).Length / 1024} KB)");

        SceneManager.LoadScene("Scene_04_Final");
    }

    void OnDestroy()
    {
        if (ffmpegCapture != null && ffmpegCapture.IsRunning)
            ffmpegCapture.StopFfmpeg();

        if (cameraStream != null)
            cameraStream.Stop();
    }

    // ---------------------------------------------------------

    void ActivateMoldura(int index)
    {
        for (int i = 0; i < molduraOverlays.Length; i++)
            molduraOverlays[i].SetActive(i == index);

        for (int i = 0; i < molduraCaptureOverlays.Length; i++)
            molduraCaptureOverlays[i].SetActive(i == index);
    }

    void SetPhase(GameObject panel, bool active)
    {
        if (panel != null) panel.SetActive(active);
    }

    void DeleteOldCapture()
    {
        string path = Path.Combine(Application.persistentDataPath, "capture.mp4");
        if (File.Exists(path))
        {
            try { File.Delete(path); }
            catch (IOException ex) { Debug.LogWarning("Não foi possível apagar capture anterior: " + ex.Message); }
        }
    }
}
