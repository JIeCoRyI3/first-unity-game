using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem;
#endif

public class SettingsController : MonoBehaviour
{
    private static Sprite unitSprite;
    private List<Selectable> selectables;
    private int selectedIndex;

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
        CacheSelectables();
        UpdateSelectionVisuals();
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
        // Scale canvas so 100 px = 1 world unit
        // Match the canvas size to the current camera view so it stretches full scene
        float unitsPerPixel = 0.01f; // because we set localScale to 0.01
        Vector2 desiredCanvasPixels = canvasScaler.referenceResolution;
        if (cam != null)
        {
            if (cam.orthographic)
            {
                float worldHeight = cam.orthographicSize * 2f;
                float worldWidth = worldHeight * Mathf.Max(0.0001f, cam.aspect);
                desiredCanvasPixels = new Vector2(worldWidth / unitsPerPixel, worldHeight / unitsPerPixel);
            }
            else
            {
                // Approximate size at canvas Z (z = 0) for perspective cameras
                float distance = Mathf.Abs(cam.transform.position.z - 0f);
                float worldHeight = 2f * distance * Mathf.Tan(0.5f * cam.fieldOfView * Mathf.Deg2Rad);
                float worldWidth = worldHeight * Mathf.Max(0.0001f, cam.aspect);
                desiredCanvasPixels = new Vector2(worldWidth / unitsPerPixel, worldHeight / unitsPerPixel);
            }
        }
        crt.sizeDelta = desiredCanvasPixels;
        if (cam != null)
        {
            canvas.worldCamera = cam; // needed for UI raycasts in world space
            canvas.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 0f);
        }
        canvas.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
        canvas.overrideSorting = true;
        canvas.sortingOrder = 1000;
        
        canvasGO.AddComponent<GraphicRaycaster>();

        // Background Panel (stretched)
        var panelGO = new GameObject("Panel");
        panelGO.transform.SetParent(canvasGO.transform, false);
        var panelImage = panelGO.AddComponent<Image>();
        panelImage.color = new Color(0.05f, 0.06f, 0.08f, 1f);
        // Ensure solid fill regardless of sprite state
        panelImage.sprite = GetUnitSprite();
        var panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 0);
        panelRect.anchorMax = new Vector2(1, 1);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Scrollable content area to fit all sliders/buttons on any screen
        var scrollGO = new GameObject("Scroll");
        scrollGO.transform.SetParent(panelGO.transform, false);
        var scrollRT = scrollGO.AddComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0, 0);
        scrollRT.anchorMax = new Vector2(1, 1);
        scrollRT.offsetMin = new Vector2(0, 0);
        scrollRT.offsetMax = new Vector2(0, 0);
        var scroll = scrollGO.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.scrollSensitivity = 20f;

        var viewportGO = new GameObject("Viewport");
        viewportGO.transform.SetParent(scrollGO.transform, false);
        var viewportRT = viewportGO.AddComponent<RectTransform>();
        viewportRT.anchorMin = new Vector2(0, 0);
        viewportRT.anchorMax = new Vector2(1, 1);
        viewportRT.offsetMin = Vector2.zero;
        viewportRT.offsetMax = Vector2.zero;
        viewportGO.AddComponent<RectMask2D>();
        var viewportImg = viewportGO.AddComponent<Image>();
        viewportImg.color = new Color(0, 0, 0, 0); // transparent
        scroll.viewport = viewportRT;

        var contentGO = new GameObject("Content");
        contentGO.transform.SetParent(viewportGO.transform, false);
        var contentRT = contentGO.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1f);
        contentRT.offsetMin = Vector2.zero;
        contentRT.offsetMax = Vector2.zero;
        scroll.content = contentRT;

        var vLayout = contentGO.AddComponent<VerticalLayoutGroup>();
        vLayout.childAlignment = TextAnchor.UpperCenter;
        vLayout.spacing = 22f;
        vLayout.padding = new RectOffset(40, 40, 60, 60);
        vLayout.childControlWidth = true;
        vLayout.childForceExpandWidth = true;
        vLayout.childControlHeight = true;
        vLayout.childForceExpandHeight = false;
        var fitter = contentGO.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Title
        var titleGO = new GameObject("Title");
        titleGO.transform.SetParent(contentGO.transform, false);
        var title = titleGO.AddComponent<Text>();
        title.text = "SETTINGS";
        title.font = PixelFontProvider.Get();
        title.fontSize = 72;
        title.fontStyle = FontStyle.Bold;
        title.alignment = TextAnchor.MiddleCenter;
        title.color = new Color(0.92f, 0.96f, 1f, 1f);
        var titleLE = titleGO.AddComponent<LayoutElement>();
        titleLE.minHeight = 72f;

        // Sliders
        float masterInit  = SettingsManager.Instance != null ? SettingsManager.Instance.MasterVolume : 1.0f;
        float sfxInit     = SettingsManager.Instance != null ? SettingsManager.Instance.SfxVolume : 1.0f;
        float musicInit   = SettingsManager.Instance != null ? SettingsManager.Instance.MusicVolume : 0.5f;
        float brightInit  = SettingsManager.Instance != null ? SettingsManager.Instance.BrightnessPercent : 50f; // 0..100
        float contrastInit = SettingsManager.Instance != null ? SettingsManager.Instance.ContrastPercent : 50f; // 0..100

        // Volume sliders (with 10% snapping)
        CreatePercentSlider(contentGO.transform, "Master", masterInit, (v01) => {
            SettingsManager.Instance?.SetMasterVolume(v01);
        });
        CreatePercentSlider(contentGO.transform, "SFX", sfxInit, (v01) => {
            SettingsManager.Instance?.SetSfxVolume(v01);
        });
        CreatePercentSlider(contentGO.transform, "Music", musicInit, (v01) => {
            SettingsManager.Instance?.SetMusicVolume(v01);
        });

        // Brightness 0..100%
        CreateLabeledSlider(contentGO.transform, "Brightness", 0f, 100f, brightInit, (val) => {
            SettingsManager.Instance?.SetBrightnessPercent(val);
        });

        // Contrast 0..100%
        CreateLabeledSlider(contentGO.transform, "Contrast", 0f, 100f, contrastInit, (val) => {
            SettingsManager.Instance?.SetContrastPercent(val);
        });

        // Single snake preview below all sliders
        AddSnakePreview(contentGO.transform);

        // Back button
        var backBtn = CreateButton(contentGO.transform, "Back");
        backBtn.onClick.AddListener(() => SceneManager.LoadScene("Menu"));
    }

    private void CacheSelectables()
    {
        var all = new List<Selectable>(FindObjectsOfType<Selectable>());
        all.Sort((a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
        selectables = new List<Selectable>();
        foreach (var s in all)
        {
            if (s == null) continue;
            if (s.GetComponentInParent<Canvas>() != null) selectables.Add(s);
        }
        selectedIndex = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, selectables.Count - 1));
    }

    private void Update()
    {
        if (selectables == null || selectables.Count == 0) return;
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.upArrowKey.wasPressedThisFrame) { MoveSelection(1); }
            else if (kb.downArrowKey.wasPressedThisFrame) { MoveSelection(-1); }
            else if (kb.leftArrowKey.wasPressedThisFrame) { AdjustCurrent(-1); }
            else if (kb.rightArrowKey.wasPressedThisFrame) { AdjustCurrent(1); }
            else if (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) { ActivateCurrent(); }
        }
#else
        if (Input.GetKeyDown(KeyCode.UpArrow)) MoveSelection(1);
        else if (Input.GetKeyDown(KeyCode.DownArrow)) MoveSelection(-1);
        else if (Input.GetKeyDown(KeyCode.LeftArrow)) AdjustCurrent(-1);
        else if (Input.GetKeyDown(KeyCode.RightArrow)) AdjustCurrent(1);
        else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) ActivateCurrent();
#endif
    }

    private void MoveSelection(int delta)
    {
        if (selectables == null || selectables.Count == 0) return;
        selectedIndex = (selectedIndex + delta + selectables.Count) % selectables.Count;
        UpdateSelectionVisuals();
    }

    private void ActivateCurrent()
    {
        var s = GetCurrent();
        if (s == null) return;
        var btn = s as Button;
        if (btn != null) btn.onClick?.Invoke();
    }

    private void AdjustCurrent(int direction)
    {
        var s = GetCurrent();
        if (s == null) return;
        var slider = s as Slider;
        if (slider != null)
        {
            // Use larger step for integer percent sliders, small step otherwise
            float step = slider.wholeNumbers ? 10f : Mathf.Max(0.0001f, (slider.maxValue - slider.minValue) / 20f);
            slider.value = Mathf.Clamp(slider.value + step * direction, slider.minValue, slider.maxValue);
        }
    }

    private Selectable GetCurrent()
    {
        if (selectedIndex < 0 || selectedIndex >= (selectables?.Count ?? 0)) return null;
        return selectables[selectedIndex];
    }

    private void UpdateSelectionVisuals()
    {
        if (selectables == null) return;
        for (int i = 0; i < selectables.Count; i++)
        {
            var s = selectables[i];
            if (s == null) continue;
            var img = s.targetGraphic as Image;
            if (img == null)
            {
                // Fallback: try Image on same object
                img = s.GetComponent<Image>();
            }
            if (img == null) continue;
            if (i == selectedIndex)
            {
                var ol = img.GetComponent<Outline>();
                if (ol == null) ol = img.gameObject.AddComponent<Outline>();
                ol.effectColor = Color.white;
                ol.effectDistance = new Vector2(2f, 2f);
                ol.useGraphicAlpha = false;
            }
            else
            {
                var ol = img.GetComponent<Outline>();
                if (ol == null) ol = img.gameObject.AddComponent<Outline>();
                ol.effectColor = new Color(1f, 1f, 1f, 0f);
                ol.effectDistance = new Vector2(2f, 2f);
                ol.useGraphicAlpha = false;
            }
        }
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
        var group = new GameObject("SoundGroup");
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
        label.font = PixelFontProvider.Get();
        label.fontSize = 36;
        label.fontStyle = FontStyle.Bold;
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
            label.text = $"Sound: {valPercent}%";
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

    private void CreatePercentSlider(Transform parent, string labelPrefix, float initial01, System.Action<float> onChanged)
    {
        // Container
        var group = new GameObject(labelPrefix + "Group");
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
        label.font = PixelFontProvider.Get();
        label.fontSize = 36;
        label.fontStyle = FontStyle.Bold;
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
            label.text = $"{labelPrefix}: {valPercent}%";
            onChanged?.Invoke(valPercent / 100f);
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
        // Ensure consistent UI rendering
        image.sprite = GetUnitSprite();

        var btn = btnGO.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.2f, 0.24f, 0.3f, 1f);
        colors.pressedColor = new Color(0.12f, 0.14f, 0.18f, 1f);
        colors.selectedColor = colors.highlightedColor;
        btn.colors = colors;

        var rt = btnGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(360, 80);

        // Prevent layout from shrinking the button and keep text visible
        var le = btnGO.AddComponent<LayoutElement>();
        le.minWidth = 360f;
        le.minHeight = 72f;

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(btnGO.transform, false);
        var t = textGO.AddComponent<Text>();
        t.text = text;
        t.font = PixelFontProvider.Get();
        t.fontSize = 48;
        t.fontStyle = FontStyle.Bold;
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
        labelText.font = PixelFontProvider.Get();
        labelText.fontSize = 32;
        labelText.fontStyle = FontStyle.Bold;
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
