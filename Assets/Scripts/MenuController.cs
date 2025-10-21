using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    private void Start()
    {
        EnsureEventSystem();
        BuildMenuUI();
    }

    private void EnsureEventSystem()
    {
        if (Object.FindObjectOfType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            // Use the correct UI input module depending on active input backend
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
        }
    }

    private void BuildMenuUI()
    {
        // Canvas
        var canvasGO = new GameObject("Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        // Background Panel
        var panelGO = new GameObject("Panel");
        panelGO.transform.SetParent(canvasGO.transform, false);
        var panelImage = panelGO.AddComponent<Image>();
        panelImage.color = new Color(0.05f, 0.06f, 0.08f, 1f);
        var panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 0);
        panelRect.anchorMax = new Vector2(1, 1);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Layout
        var layout = panelGO.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 18f;
        layout.padding = new RectOffset(20, 20, 40, 40);

        // Title
        var titleGO = new GameObject("Title");
        titleGO.transform.SetParent(panelGO.transform, false);
        var title = titleGO.AddComponent<Text>();
        title.text = "SNAKE";
        title.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        title.fontSize = 56;
        title.alignment = TextAnchor.MiddleCenter;
        title.color = new Color(0.92f, 0.96f, 1f, 1f);
        var titleLE = titleGO.AddComponent<LayoutElement>();
        titleLE.minHeight = 80f;

        // Play button
        var playBtn = CreateButton(panelGO.transform, "Играть");
        playBtn.onClick.AddListener(() => SceneManager.LoadScene("Snake"));

        // Settings button
        var settingsBtn = CreateButton(panelGO.transform, "Настройки");
        settingsBtn.onClick.AddListener(() => SceneManager.LoadScene("Settings"));

        // Quit button
        var quitBtn = CreateButton(panelGO.transform, "Выход");
        quitBtn.onClick.AddListener(QuitApp);
    }

    private Button CreateButton(Transform parent, string text)
    {
        var btnGO = new GameObject(text + "Button");
        btnGO.transform.SetParent(parent, false);
        var image = btnGO.AddComponent<Image>();
        image.color = new Color(0.15f, 0.18f, 0.22f, 1f);

        var btn = btnGO.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.2f, 0.24f, 0.3f, 1f);
        colors.pressedColor = new Color(0.12f, 0.14f, 0.18f, 1f);
        colors.selectedColor = colors.highlightedColor;
        btn.colors = colors;

        var rt = btnGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(280, 64);

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(btnGO.transform, false);
        var t = textGO.AddComponent<Text>();
        t.text = text;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = 28;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = new Color(0.9f, 0.95f, 0.9f, 1f);
        var textRT = t.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        return btn;
    }

    private void QuitApp()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
