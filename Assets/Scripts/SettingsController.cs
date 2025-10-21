using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public class SettingsController : MonoBehaviour
{
    private void Start()
    {
        EnsureEventSystem();
        EnsureSettingsManager();
        BuildSettingsUI();
    }

    private void EnsureSettingsManager()
    {
        if (SettingsManager.Instance == null)
        {
            var go = new GameObject("SettingsManager");
            go.AddComponent<SettingsManager>();
        }
    }

    private void EnsureEventSystem()
    {
        if (Object.FindObjectOfType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
        }
    }

    private void BuildSettingsUI()
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
        title.text = "НАСТРОЙКИ";
        title.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        title.fontSize = 50;
        title.alignment = TextAnchor.MiddleCenter;
        title.color = new Color(0.92f, 0.96f, 1f, 1f);
        var titleLE = titleGO.AddComponent<LayoutElement>();
        titleLE.minHeight = 72f;

        // Sliders
        float soundInit = SettingsManager.Instance != null ? SettingsManager.Instance.SoundVolume : 0.8f;
        float brightInit = SettingsManager.Instance != null ? SettingsManager.Instance.BrightnessEV100 : 0f; // -2..2
        float contrastInit = SettingsManager.Instance != null ? SettingsManager.Instance.Contrast : 0f; // -100..100

        // Sound 0..1 directly
        CreateLabeledSlider(panelGO.transform, "Звук", 0f, 1f, soundInit, (v) => {
            SettingsManager.Instance?.SetSoundVolume(v);
        });

        // Brightness: map -2..2 EV
        CreateLabeledSlider(panelGO.transform, "Яркость", -2f, 2f, brightInit, (ev) => {
            SettingsManager.Instance?.SetBrightness(ev);
        });

        // Contrast: -100..100
        CreateLabeledSlider(panelGO.transform, "Контраст", -100f, 100f, contrastInit, (c) => {
            SettingsManager.Instance?.SetContrast(c);
        });

        // Back button
        var backBtn = CreateButton(panelGO.transform, "Назад");
        backBtn.onClick.AddListener(() => SceneManager.LoadScene("Menu"));
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

    private void CreateLabeledSlider(Transform parent, string label, float min, float max, float initial, System.Action<float> onChanged)
    {
        // Container
        var group = new GameObject(label + "Group");
        group.transform.SetParent(parent, false);
        var groupLayout = group.AddComponent<VerticalLayoutGroup>();
        groupLayout.childAlignment = TextAnchor.MiddleCenter;
        groupLayout.spacing = 6f;
        var groupLE = group.AddComponent<LayoutElement>();
        groupLE.minHeight = 80f;
        groupLE.minWidth = 500f;

        // Label
        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(group.transform, false);
        var labelText = labelGO.AddComponent<Text>();
        labelText.text = label;
        labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelText.fontSize = 24;
        labelText.alignment = TextAnchor.MiddleCenter;
        labelText.color = new Color(0.9f, 0.95f, 1f, 1f);
        var labelLE = labelGO.AddComponent<LayoutElement>();
        labelLE.minHeight = 34f;

        // Slider root
        var sliderGO = new GameObject("Slider");
        sliderGO.transform.SetParent(group.transform, false);
        var slider = sliderGO.AddComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.wholeNumbers = false;
        slider.value = Mathf.Clamp(initial, min, max);

        var srt = sliderGO.GetComponent<RectTransform>();
        srt.sizeDelta = new Vector2(520, 28);

        // Background
        var bg = new GameObject("Background");
        bg.transform.SetParent(sliderGO.transform, false);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.1f, 0.12f, 0.16f, 1f);
        bgImg.sprite = Sprite.Create(new Texture2D(1,1), new Rect(0,0,1,1), new Vector2(0.5f,0.5f));
        var bgRT = bg.GetComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0, 0.25f);
        bgRT.anchorMax = new Vector2(1, 0.75f);
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;

        // Fill Area
        var fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderGO.transform, false);
        var faRT = fillArea.AddComponent<RectTransform>();
        faRT.anchorMin = new Vector2(0, 0.25f);
        faRT.anchorMax = new Vector2(1, 0.75f);
        faRT.offsetMin = new Vector2(6, 0);
        faRT.offsetMax = new Vector2(-6, 0);

        var fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        var fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.2f, 0.6f, 1.0f, 0.9f);
        fillImg.sprite = bgImg.sprite;
        var fillRT = fill.GetComponent<RectTransform>();
        fillRT.anchorMin = new Vector2(0, 0);
        fillRT.anchorMax = new Vector2(1, 1);
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;

        // Handle
        var handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(sliderGO.transform, false);
        var haRT = handleArea.AddComponent<RectTransform>();
        haRT.anchorMin = new Vector2(0, 0);
        haRT.anchorMax = new Vector2(1, 1);
        haRT.offsetMin = new Vector2(6, 0);
        haRT.offsetMax = new Vector2(-6, 0);

        var handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        var handleImg = handle.AddComponent<Image>();
        handleImg.color = new Color(0.9f, 0.95f, 1f, 1f);
        handleImg.sprite = bgImg.sprite;
        var handleRT = handle.GetComponent<RectTransform>();
        handleRT.sizeDelta = new Vector2(16, 24);

        // Wire slider refs
        slider.fillRect = fillImg.rectTransform;
        slider.targetGraphic = handleImg;
        slider.handleRect = handleImg.rectTransform;
        slider.direction = Slider.Direction.LeftToRight;

        slider.onValueChanged.AddListener((val) => { onChanged?.Invoke(val); });
    }
}
