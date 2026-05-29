using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonKeyTrigger : MonoBehaviour
{
    public enum TriggerKey { PageUp, PageDown, AnyKey, AnyKeyExceptPaging }

    public TriggerKey key = TriggerKey.PageDown;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void Update()
    {
        bool pressed = key switch
        {
            TriggerKey.PageUp             => Input.GetKeyDown(KeyCode.PageUp),
            TriggerKey.PageDown           => Input.GetKeyDown(KeyCode.PageDown),
            TriggerKey.AnyKey             => AnyKeyboardKeyDown(),
            TriggerKey.AnyKeyExceptPaging => AnyKeyboardKeyDown()
                                             && !Input.GetKeyDown(KeyCode.PageUp)
                                             && !Input.GetKeyDown(KeyCode.PageDown),
            _                             => false,
        };

        if (pressed && button.interactable)
            button.onClick.Invoke();
    }

    // Detecta qualquer tecla do teclado, ignorando cliques de mouse (botões 0–6).
    private bool AnyKeyboardKeyDown()
    {
        if (!Input.anyKeyDown) return false;
        for (int i = 0; i < 7; i++)
        {
            if (Input.GetMouseButtonDown(i)) return false;
        }
        return true;
    }
}
