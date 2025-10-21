using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class LevelUpController : MonoBehaviour
{
    private List<Button> buttons;
    private int selectedIndex;
    private bool isKeyboardMode;

    public void Initialize(List<Button> buttons)
    {
        this.buttons = buttons ?? new List<Button>();
        selectedIndex = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, this.buttons.Count - 1));
        // Bind hover to move selection by mouse
        for (int i = 0; i < this.buttons.Count; i++)
        {
            BindPointerEnter(this.buttons[i], i);
        }
        UpdateVisuals();
    }

    private void BindPointerEnter(Button btn, int index)
    {
        if (btn == null) return;
        var et = btn.gameObject.GetComponent<EventTrigger>();
        if (et == null) et = btn.gameObject.AddComponent<EventTrigger>();
        var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        entry.callback = new EventTrigger.TriggerEvent();
        entry.callback.AddListener((_) => { isKeyboardMode = false; SetSelectedIndex(index); });
        et.triggers.Add(entry);
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
                isKeyboardMode = true; MoveSelection(-1);
            }
            else if (kb.downArrowKey.wasPressedThisFrame)
            {
                isKeyboardMode = true; MoveSelection(1);
            }
            else if (kb.enterKey.wasPressedThisFrame)
            {
                isKeyboardMode = true; ActivateSelected();
            }
        }
#else
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            isKeyboardMode = true; MoveSelection(-1);
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            isKeyboardMode = true; MoveSelection(1);
        }
        else if (Input.GetKeyDown(KeyCode.Return))
        {
            isKeyboardMode = true; ActivateSelected();
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
                // Ensure visible white outline on selected
                var img = btn.GetComponent<Image>();
                if (img != null)
                {
                    var ol = img.GetComponent<Outline>();
                    if (ol == null) ol = img.gameObject.AddComponent<Outline>();
                    ol.effectColor = new Color(1f, 1f, 1f, 1f);
                    ol.effectDistance = new Vector2(2f, 2f);
                    ol.useGraphicAlpha = false;
                }
            }
            else
            {
                colors.normalColor = new Color(0.18f, 0.22f, 0.28f, 1f);
                // Hide outline when not selected
                var img = btn.GetComponent<Image>();
                if (img != null)
                {
                    var ol = img.GetComponent<Outline>();
                    if (ol == null) ol = img.gameObject.AddComponent<Outline>();
                    ol.effectColor = new Color(1f, 1f, 1f, 0f);
                    ol.effectDistance = new Vector2(2f, 2f);
                    ol.useGraphicAlpha = false;
                }
            }
            btn.colors = colors;
        }
    }

    private void SetSelectedIndex(int index)
    {
        selectedIndex = Mathf.Clamp(index, 0, Mathf.Max(0, buttons.Count - 1));
        UpdateVisuals();
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
