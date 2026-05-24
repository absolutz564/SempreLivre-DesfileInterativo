using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Scene02Controller : MonoBehaviour
{
    [Header("Botões de Moldura")]
    public Button[] molduraButtons;

    [Header("Configuração Visual")]
    public Color selectedBorderColor = new Color(0.96f, 0.17f, 0.56f, 1f);
    public float borderThickness = 5f;

    private int selectedIndex = 0;
    // 4 strips por botão (Top, Bottom, Left, Right)
    private Image[][] borderStrips;

    void Start()
    {
        borderStrips = new Image[molduraButtons.Length][];
        for (int i = 0; i < molduraButtons.Length; i++)
            borderStrips[i] = CreateBorderFor(molduraButtons[i]);

        UpdateVisuals();
    }

    // Cria 4 imagens finas como filhos do botão formando uma borda retangular
    Image[] CreateBorderFor(Button btn)
    {
        Image[] strips = new Image[4];
        string[] names = { "Top", "Bottom", "Left", "Right" };

        for (int s = 0; s < 4; s++)
        {
            var go = new GameObject("_Border_" + names[s]);
            go.transform.SetParent(btn.transform, false);
            go.transform.SetAsLastSibling();

            var img = go.AddComponent<Image>();
            img.raycastTarget = false; // não bloqueia cliques no botão
            img.color = Color.clear;
            strips[s] = img;

            var rt = go.GetComponent<RectTransform>();

            switch (s)
            {
                case 0: // Top
                    rt.anchorMin = new Vector2(0f, 1f);
                    rt.anchorMax = new Vector2(1f, 1f);
                    rt.pivot     = new Vector2(0.5f, 1f);
                    rt.anchoredPosition = Vector2.zero;
                    rt.sizeDelta = new Vector2(0f, borderThickness);
                    break;
                case 1: // Bottom
                    rt.anchorMin = new Vector2(0f, 0f);
                    rt.anchorMax = new Vector2(1f, 0f);
                    rt.pivot     = new Vector2(0.5f, 0f);
                    rt.anchoredPosition = Vector2.zero;
                    rt.sizeDelta = new Vector2(0f, borderThickness);
                    break;
                case 2: // Left
                    rt.anchorMin = new Vector2(0f, 0f);
                    rt.anchorMax = new Vector2(0f, 1f);
                    rt.pivot     = new Vector2(0f, 0.5f);
                    rt.anchoredPosition = Vector2.zero;
                    rt.sizeDelta = new Vector2(borderThickness, 0f);
                    break;
                case 3: // Right
                    rt.anchorMin = new Vector2(1f, 0f);
                    rt.anchorMax = new Vector2(1f, 1f);
                    rt.pivot     = new Vector2(1f, 0.5f);
                    rt.anchoredPosition = Vector2.zero;
                    rt.sizeDelta = new Vector2(borderThickness, 0f);
                    break;
            }
        }

        return strips;
    }

    // Chamado pelos botões: OnClick -> SelectMoldura(0) ou SelectMoldura(1)
    public void SelectMoldura(int index)
    {
        selectedIndex = index;
        UpdateVisuals();
    }

    void UpdateVisuals()
    {
        for (int i = 0; i < molduraButtons.Length; i++)
        {
            bool sel = i == selectedIndex;

            // Borda colorida quando selecionado, invisível quando não
            Color c = sel ? selectedBorderColor : Color.clear;
            foreach (var strip in borderStrips[i])
                strip.color = c;

            // Leve escala para reforçar o destaque
            molduraButtons[i].transform.localScale = sel ? Vector3.one * 1.06f : Vector3.one;
        }
    }

    // Botão "Confirmar Escolha"
    public void OnConfirmarClick()
    {
        if (GameData.Instance != null)
            GameData.Instance.SelectedMolduraIndex = selectedIndex;

        SceneManager.LoadScene("Scene_03_Recording");
    }
}
