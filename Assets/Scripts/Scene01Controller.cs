using UnityEngine;
using UnityEngine.SceneManagement;

public class Scene01Controller : MonoBehaviour
{
    public GameObject comoJogarModal;

    void Start()
    {
        comoJogarModal.SetActive(false);
        EnsureGameData();
    }

    // Garante que o GameData persiste mesmo ao iniciar direto nesta cena
    void EnsureGameData()
    {
        if (GameData.Instance == null)
            new GameObject("GameData").AddComponent<GameData>();
    }

    // Botão "Tocar para começar" / "Como Jogar"
    public void OnAbrirModalClick()
    {
        comoJogarModal.SetActive(true);
    }

    // Botão "Avançar" dentro do modal
    public void OnAvancarClick()
    {
        SceneManager.LoadScene("Scene_02_FrameSelection");
    }
}
