using System;
using UnityEngine;
using MySql.Data.MySqlClient; // precisa do MySQL Connector

public class TokenLoader : MonoBehaviour
{
    [Header("DB Settings")]
    private string dbHost = "145.14.134.34";
    private string dbPort = "3306";
    private string dbUser = "jailsonDev";
    private string dbPass = "jailsonDev#321#";
    private string dbName = "projectTokens";

    [Header("Project Info")]
    [SerializeField] private string projectId = "spatenWanderleyPopo"; // nomeProjeto+buildNumber
    public string token;
    public string accessToken;
    public string projectVersion;
    string unityVersion = Application.version;

    void Start()
    {
        LoadTokensFromDB();
    }

    private void LoadTokensFromDB()
    {
        string connStr = $"Server={dbHost};Port={dbPort};Database={dbName};Uid={dbUser};Pwd={dbPass};";

        try
        {
            using (MySqlConnection conn = new MySqlConnection(connStr))
            {
                conn.Open();
                Debug.Log("✅ Conectado ao banco com sucesso!");

                string query = "SELECT token, accessToken, projectVersion FROM tokens WHERE projectId = @projectId LIMIT 1";
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@projectId", projectId);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            token = reader.GetString("token");
                            accessToken = reader.GetString("accessToken");
                            PlayerPrefs.SetString("token", token); // salva localmente
                            PlayerPrefs.SetString("accessToken", accessToken); // salva localmente
                            projectVersion = reader.GetString("projectVersion");

                            Debug.Log($"Token carregado: {token.Substring(0, 15)}...");
                            Debug.Log($"AccessToken carregado: {accessToken.Substring(0, 15)}...");
                            Debug.Log($"Versão do projeto: {projectVersion}");
                        }
                        else
                        {
                            Debug.LogError($"❌ Nenhum registro encontrado para projectId={projectId}");
                        }
                    }
                }
            }
            UpdateProjectVersion();
        }
        catch (Exception ex)
        {
            Debug.LogError("Erro ao conectar no banco: " + ex.Message);
        }
    }

    private void UpdateProjectVersion()
    {
        string connStr = $"Server={dbHost};Port={dbPort};Database={dbName};Uid={dbUser};Pwd={dbPass};";
        string unityVersion = Application.version; // pega do PlayerSettings

        try
        {
            using (MySqlConnection conn = new MySqlConnection(connStr))
            {
                conn.Open();
                Debug.Log("🔄 Conectado para atualizar versão!");

                string updateQuery = "UPDATE tokens SET projectVersion = @newVersion WHERE projectId = @projectId";
                using (MySqlCommand cmd = new MySqlCommand(updateQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@newVersion", unityVersion);
                    cmd.Parameters.AddWithValue("@projectId", projectId);

                    int rowsAffected = cmd.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        Debug.Log($"✅ Versão atualizada no banco para {unityVersion}");
                        projectVersion = unityVersion; // também atualiza localmente
                    }
                    else
                    {
                        Debug.LogError($"❌ Nenhum registro encontrado para projectId={projectId}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Erro ao atualizar versão: " + ex.Message);
        }
    }


}
