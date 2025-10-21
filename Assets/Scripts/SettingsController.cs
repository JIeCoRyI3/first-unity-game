using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public class SettingsController : MonoBehaviour
{
    private static Sprite unitSprite;

    private static Sprite GetUnitSprite()
    {
        if (unitSprite == null)
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            unitSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
            unitSprite.name = "UnitSprite1x1";
        }
        return unitSprite;
    }
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
        // World Space canvas so post-processing affects UI
        canvas.renderMode = RenderMode.WorldSpace;
        var canvasScaler = canvasGO.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1280, 720);
        var cam = Camera.main;
        var crt = canvasGO.GetComponent<RectTransform>();
        if (cam != null)
        {
            float heightUnits = cam.orthographicSize * 2f;
            float widthUnits = heightUnits * cam.aspect;
            crt.sizeDelta = new Vector2(widthUnits, heightUnits);
            canvas.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 0f);
        }
        canvas.overrideSorting = true;
        canvas.sortingOrder = 1000;
        
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
        title.text = "SETTINGS";
        title.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        title.fontSize = 46;
        title.alignment = TextAnchor.MiddleCenter;
        title.color = new Color(0.92f, 0.96f, 1f, 1f);
        var titleLE = titleGO.AddComponent<LayoutElement>();
        titleLE.minHeight = 64f;

        // Sliders
        float soundInit = SettingsManager.Instance != null ? SettingsManager.Instance.SoundVolume : 0.8f;
        float brightInit = SettingsManager.Instance != null ? SettingsManager.Instance.BrightnessPercent : 50f; // 0..100
        float contrastInit = SettingsManager.Instance != null ? SettingsManager.Instance.ContrastPercent : 50f; // 0..100

        // Volume slider snapping to 10% steps with percent label
        CreatePercentVolumeSlider(panelGO.transform, soundInit);

        // Brightness 0..100%
        CreateLabeledSlider(panelGO.transform, "Яркость", 0f, 100f, brightInit, (val) => {
            SettingsManager.Instance?.SetBrightnessPercent(val);
        });

        // Contrast 0..100%
        CreateLabeledSlider(panelGO.transform, "Контраст", 0f, 100f, contrastInit, (val) => {
            SettingsManager.Instance?.SetContrastPercent(val);
        });

        // Single snake preview below all sliders
        AddSnakePreview(panelGO.transform);

        // Back button
        var backBtn = CreateButton(panelGO.transform, "Назад");
        backBtn.onClick.AddListener(() => SceneManager.LoadScene("Menu"));
    }

    private void AddSnakePreview(Transform parent)
    {
        // Simple static snake preview (13 cells) using UI Images
        var previewGO = new GameObject("SnakePreview");
        previewGO.transform.SetParent(parent, false);
        var layout = previewGO.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 2f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        var le = previewGO.AddComponent<LayoutElement>();
        le.minHeight = 22f;
        le.minWidth = 300f;
        int length = 13;
        for (int i = 0; i < length; i++)
        {
            var cell = new GameObject("Cell" + i);
            cell.transform.SetParent(previewGO.transform, false);
            var img = cell.AddComponent<Image>();
            // Alternate colors to resemble segments
            img.color = (i == 0) ? new Color(0.2f, 0.9f, 0.3f, 1f) : new Color(0.2f, 0.7f, 0.25f, 1f);
            img.sprite = GetUnitSprite();
            var rt = img.rectTransform;
            rt.sizeDelta = new Vector2(16, 16);
        }
    }

    private void CreatePercentVolumeSlider(Transform parent, float initial01)
    {
        // Container
        var group = new GameObject("ЗвукGroup");
        group.transform.SetParent(parent, false);
        var v = group.AddComponent<VerticalLayoutGroup>();
        v.childAlignment = TextAnchor.MiddleCenter;
        v.spacing = 6f;
        var le = group.AddComponent<LayoutElement>();
        le.minHeight = 80f;
        le.minWidth = 500f;

        // Label with percent
        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(group.transform, false);
        var label = labelGO.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 24;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = new Color(0.9f, 0.95f, 1f, 1f);

        // Slider root
        var sliderGO = new GameObject("Slider");
        sliderGO.transform.SetParent(group.transform, false);
        var slider = sliderGO.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 100f;
        slider.wholeNumbers = true; // restrict to integer percent values
        int initialPercent = Mathf.RoundToInt(Mathf.Clamp01(initial01) * 100f);
        slider.value = initialPercent;

        var srt = sliderGO.GetComponent<RectTransform>();
        srt.sizeDelta = new Vector2(420, 20);

        // Background
        var bg = new GameObject("Background");
        bg.transform.SetParent(sliderGO.transform, false);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.1f, 0.12f, 0.16f, 1f);
        bgImg.sprite = GetUnitSprite();
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
        fillImg.sprite = GetUnitSprite();
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
        handleImg.sprite = GetUnitSprite();
        var handleRT = handle.GetComponent<RectTransform>();
        handleRT.sizeDelta = new Vector2(12, 18);

        // Wire slider refs
        slider.fillRect = fillImg.rectTransform;
        slider.targetGraphic = handleImg;
        slider.handleRect = handleImg.rectTransform;
        slider.direction = Slider.Direction.LeftToRight;

        // Percent snapping to predefined checkpoints only
        int[] checkpoints = new int[] {0,10,20,30,40,50,60,70,80,90,100};
        int SnapToCheckpoint(int val)
        {
            int best = checkpoints[0];
            int bestDist = Mathf.Abs(val - best);
            for (int i = 1; i < checkpoints.Length; i++)
            {
                int d = Mathf.Abs(val - checkpoints[i]);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = checkpoints[i];
                }
            }
            return best;
        }

        void UpdateLabelAndApply(int valPercent)
        {
            label.text = $"Звук: {valPercent}%";
            SettingsManager.Instance?.SetSoundVolume(valPercent / 100f);
        }

        // Initialize label
        UpdateLabelAndApply(initialPercent);

        // Enforce snapping and application on value changes
        slider.onValueChanged.AddListener((val) => {
            int intVal = Mathf.RoundToInt(val);
            int snapped = SnapToCheckpoint(intVal);
            if (snapped != intVal)
            {
                slider.SetValueWithoutNotify(snapped);
            }
            UpdateLabelAndApply(snapped);
        });
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
        srt.sizeDelta = new Vector2(420, 20);

        // Background
        var bg = new GameObject("Background");
        bg.transform.SetParent(sliderGO.transform, false);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.1f, 0.12f, 0.16f, 1f);
        bgImg.sprite = GetUnitSprite();
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
        fillImg.sprite = GetUnitSprite();
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
        handleImg.sprite = GetUnitSprite();
        var handleRT = handle.GetComponent<RectTransform>();
        handleRT.sizeDelta = new Vector2(12, 18);

        // Wire slider refs
        slider.fillRect = fillImg.rectTransform;
        slider.targetGraphic = handleImg;
        slider.handleRect = handleImg.rectTransform;
        slider.direction = Slider.Direction.LeftToRight;

        slider.onValueChanged.AddListener((val) => { onChanged?.Invoke(val); });
    }
}
