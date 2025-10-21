using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    public float SoundVolume { get; private set; } = 0.8f;           // 0..1
    // New percent-based controls (0..100). 50% equals neutral defaults
    public float BrightnessPercent { get; private set; } = 50f;      // 0..100
    public float ContrastPercent  { get; private set; } = 50f;       // 0..100

    private Volume globalVolume;
    private VolumeProfile globalProfile;
    private ColorAdjustments colorAdjustments;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureGlobalPostFX();
        ApplyAllSettings();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureGlobalPostFX();
        EnablePostProcessingOnMainCamera();
        ApplyAllSettings();
    }

    private void EnsureGlobalPostFX()
    {
        if (globalVolume == null)
        {
            var go = GameObject.Find("GlobalPostFX");
            if (go == null)
            {
                go = new GameObject("GlobalPostFX");
                DontDestroyOnLoad(go);
            }
            globalVolume = go.GetComponent<Volume>();
            if (globalVolume == null)
            {
                globalVolume = go.AddComponent<Volume>();
            }
            globalVolume.isGlobal = true;
            globalVolume.priority = 10f;
        }

        if (globalProfile == null)
        {
            globalProfile = globalVolume.profile;
            if (globalProfile == null)
            {
                globalProfile = ScriptableObject.CreateInstance<VolumeProfile>();
                globalVolume.profile = globalProfile;
            }
        }

        if (!globalProfile.TryGet(out colorAdjustments))
        {
            colorAdjustments = globalProfile.Add<ColorAdjustments>(true);
            colorAdjustments.postExposure.overrideState = true;
            colorAdjustments.contrast.overrideState = true;
            colorAdjustments.saturation.overrideState = true;
            colorAdjustments.colorFilter.overrideState = false;
            colorAdjustments.hueShift.overrideState = false;
        }

        EnablePostProcessingOnMainCamera();
    }

    private void EnablePostProcessingOnMainCamera()
    {
        // Ensure post-processing is enabled on all active cameras in the scene
        var cams = Object.FindObjectsOfType<Camera>();
        foreach (var cam in cams)
        {
            if (cam == null || !cam.enabled) continue;
            var additional = cam.GetComponent<UniversalAdditionalCameraData>();
            if (additional == null)
            {
                additional = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            }
            additional.renderPostProcessing = true;
        }
    }

    private void ApplyAllSettings()
    {
        ApplySoundVolume(SoundVolume);
        ApplyBrightnessPercent(BrightnessPercent);
        ApplyContrastPercent(ContrastPercent);
    }

    public void SetSoundVolume(float value)
    {
        SoundVolume = Mathf.Clamp01(value);
        ApplySoundVolume(SoundVolume);
    }

    public void SetBrightnessPercent(float percent)
    {
        BrightnessPercent = Mathf.Clamp(percent, 0f, 100f);
        ApplyBrightnessPercent(BrightnessPercent);
    }

    public void SetContrastPercent(float percent)
    {
        ContrastPercent = Mathf.Clamp(percent, 0f, 100f);
        ApplyContrastPercent(ContrastPercent);
    }

    private static void ApplySoundVolume(float volume)
    {
        AudioListener.volume = Mathf.Clamp01(volume);
    }

    private void ApplyBrightnessPercent(float percent)
    {
        if (colorAdjustments == null) EnsureGlobalPostFX();
        if (colorAdjustments != null)
        {
            // Map 0..100% -> EV range [-4..+4], 50% => 0 EV (neutral)
            float ev = Mathf.Lerp(-4f, 4f, Mathf.Clamp01(percent / 100f));
            colorAdjustments.postExposure.value = ev;
        }
    }

    private void ApplyContrastPercent(float percent)
    {
        if (colorAdjustments == null) EnsureGlobalPostFX();
        if (colorAdjustments != null)
        {
            // Interpret "contrast" as perceived color intensity:
            // 0% => grayscale, 50% => normal, 100% => very saturated
            float saturation = Mathf.Lerp(-100f, 100f, Mathf.Clamp01(percent / 100f));
            colorAdjustments.saturation.value = saturation;
            // Optionally bias contrast slightly with saturation for more impact
            colorAdjustments.contrast.value = Mathf.Lerp(-10f, 10f, Mathf.Clamp01(percent / 100f));
        }
    }
}
