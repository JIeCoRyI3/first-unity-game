using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class LevelUpController : MonoBehaviour
{
    private List<Button> buttons;
    private int selectedIndex;

    public void Initialize(List<Button> buttons)
    {
        this.buttons = buttons ?? new List<Button>();
        selectedIndex = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, this.buttons.Count - 1));
        UpdateVisuals();
    }

    private void Update()
    {
        if (buttons == null || buttons.Count == 0) return;
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.upArrowKey.wasPressedThisFrame)
            {
                MoveSelection(-1);
            }
            else if (kb.downArrowKey.wasPressedThisFrame)
            {
                MoveSelection(1);
            }
            else if (kb.enterKey.wasPressedThisFrame)
            {
                ActivateSelected();
            }
        }
#else
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            MoveSelection(-1);
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            MoveSelection(1);
        }
        else if (Input.GetKeyDown(KeyCode.Return))
        {
            ActivateSelected();
        }
#endif
    }

    private void MoveSelection(int delta)
    {
        selectedIndex = (selectedIndex + delta + buttons.Count) % buttons.Count;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            var btn = buttons[i];
            if (btn == null) continue;
            var colors = btn.colors;
            if (i == selectedIndex)
            {
                colors.normalColor = new Color(0.26f, 0.32f, 0.42f, 1f);
            }
            else
            {
                colors.normalColor = new Color(0.18f, 0.22f, 0.28f, 1f);
            }
            btn.colors = colors;
        }
    }

    private void ActivateSelected()
    {
        if (selectedIndex < 0 || selectedIndex >= buttons.Count) return;
        var btn = buttons[selectedIndex];
        if (btn != null)
        {
            btn.onClick?.Invoke();
        }
    }
}
