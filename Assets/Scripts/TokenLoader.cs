using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class TokenLoader : MonoBehaviour
{
    [Header("API Settings")]
    private string apiUrl = "https://api.tmj.dilis.com.br";
    private string credentialsEndpoint = "/agent/unity/credentials";
    private string masterKey = "Dy4APyw0CFHSnwrUSW1QrCV27zFetyQ2";

    [Header("Identificação do Booth")]
    [SerializeField] private string activationKey = "JOAO_DE_TAL";

    [Header("Debug")]
    [Tooltip("Se ativo, mostra a master key completa no log. Use apenas em debug local.")]
    [SerializeField] private bool logMasterKeyInPlainText = false;

    [Header("Credenciais Retornadas (read-only)")]
    public string token;         // bearer
    public string accessToken;   // access_token
    public string experienceName;
    public string experienceLogo;
    public string clientLabel;
    public string userName;
    public string expiresAt;

    void Start()
    {
        StartCoroutine(LoadCredentialsFromApi());
    }

    private IEnumerator LoadCredentialsFromApi()
    {
        string url = apiUrl + credentialsEndpoint;
        string body = JsonUtility.ToJson(new CredentialsRequest
        {
            activation_key = activationKey
        }, prettyPrint: true);

        byte[] bodyBytes = Encoding.UTF8.GetBytes(body);

        // Cabeçalhos da request (rastreamos manualmente porque UnityWebRequest
        // não expõe um getter geral de headers enviados).
        var requestHeaders = new Dictionary<string, string>
        {
            { "Content-Type",       "application/json" },
            { "x-unity-master-key", logMasterKeyInPlainText ? masterKey : Mask(masterKey) },
        };

        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(bodyBytes);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("x-unity-master-key", masterKey);

            LogRequest("POST", url, requestHeaders, body, bodyBytes.Length);

            float t0 = Time.realtimeSinceStartup;
            yield return req.SendWebRequest();
            float elapsedMs = (Time.realtimeSinceStartup - t0) * 1000f;

            LogResponse(req, elapsedMs);

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"❌ Erro ao buscar credenciais: {req.error} | {req.downloadHandler?.text}");
                yield break;
            }

            try
            {
                CredentialsResponse data = JsonUtility.FromJson<CredentialsResponse>(req.downloadHandler.text);

                token        = data.bearer;
                accessToken  = data.access_token;
                expiresAt    = data.expires_at;
                experienceName = data.experience != null ? data.experience.name : null;
                experienceLogo = data.experience != null ? data.experience.logo : null;
                clientLabel    = data.client_token != null ? data.client_token.label : null;
                userName       = data.user != null ? data.user.name : null;

                PlayerPrefs.SetString("token", token ?? "");
                PlayerPrefs.SetString("accessToken", accessToken ?? "");
                PlayerPrefs.Save();

                Debug.Log("✅ Credenciais recebidas e salvas no PlayerPrefs");
                Debug.Log($"  Token (bearer):  {Mask(token)}");
                Debug.Log($"  AccessToken:     {Mask(accessToken)}");
                Debug.Log($"  Experience:      {experienceName}");
                Debug.Log($"  Client:          {clientLabel}");
                Debug.Log($"  User:            {userName}");
                Debug.Log($"  Expira em:       {expiresAt}");
            }
            catch (Exception ex)
            {
                Debug.LogError("Erro ao parsear resposta da API: " + ex.Message + "\nPayload: " + req.downloadHandler.text);
            }
        }
    }

    private static string Mask(string s)
    {
        if (string.IsNullOrEmpty(s)) return "(vazio)";
        return s.Length > 15 ? s.Substring(0, 15) + "..." : s;
    }

    // -----------------------------------------------------------
    // Logging estilo "Network tab" do navegador

    private static void LogRequest(string method, string url, Dictionary<string, string> headers, string body, int bodyBytes)
    {
        var sb = new StringBuilder();
        sb.AppendLine("════════════════════════════════════════════════════════");
        sb.AppendLine("📤 [TokenLoader] REQUEST");
        sb.AppendLine("────────────────────────────────────────────────────────");
        sb.AppendLine($"Method:  {method}");
        sb.AppendLine($"URL:     {url}");
        sb.AppendLine("Headers:");
        foreach (var kv in headers)
            sb.AppendLine($"  {kv.Key}: {kv.Value}");
        sb.AppendLine($"Body ({bodyBytes} bytes):");
        sb.AppendLine(string.IsNullOrEmpty(body) ? "  (vazio)" : body);
        sb.Append("════════════════════════════════════════════════════════");
        Debug.Log(sb.ToString());
    }

    private static void LogResponse(UnityWebRequest req, float elapsedMs)
    {
        var sb = new StringBuilder();
        string payload = req.downloadHandler != null ? req.downloadHandler.text : "";
        int bytes = req.downloadHandler != null && req.downloadHandler.data != null ? req.downloadHandler.data.Length : 0;

        sb.AppendLine("════════════════════════════════════════════════════════");
        sb.AppendLine($"📥 [TokenLoader] RESPONSE  ({elapsedMs:F0} ms)");
        sb.AppendLine("────────────────────────────────────────────────────────");
        sb.AppendLine($"Status:  {req.responseCode}  ({req.result})");
        sb.AppendLine($"URL:     {req.url}");

        var respHeaders = req.GetResponseHeaders();
        sb.AppendLine("Headers:");
        if (respHeaders != null && respHeaders.Count > 0)
        {
            foreach (var kv in respHeaders)
                sb.AppendLine($"  {kv.Key}: {kv.Value}");
        }
        else
        {
            sb.AppendLine("  (nenhum)");
        }

        sb.AppendLine($"Body ({bytes} bytes):");
        sb.AppendLine(string.IsNullOrEmpty(payload) ? "  (vazio)" : payload);
        sb.Append("════════════════════════════════════════════════════════");
        Debug.Log(sb.ToString());
    }

    // -----------------------------------------------------------
    // DTOs do payload da API

    [Serializable]
    private class CredentialsRequest
    {
        public string activation_key;
    }

    [Serializable]
    private class CredentialsResponse
    {
        public string activation_key;
        public ExperienceInfo experience;
        public string access_token;
        public string bearer;
        public string expires_at;
        public string access_token_expires_at;
        public ClientTokenInfo client_token;
        public UserInfo user;
    }

    [Serializable]
    private class ExperienceInfo
    {
        public string id;
        public string name;
        public string logo;
    }

    [Serializable]
    private class ClientTokenInfo
    {
        public string id;
        public string label;
    }

    [Serializable]
    private class UserInfo
    {
        public string id;
        public string name;
        public string email;
    }
}
