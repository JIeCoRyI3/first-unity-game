using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    public float SoundVolume { get; private set; } = 0.8f;           // 0..1
    public float BrightnessEV100 { get; private set; } = 0.0f;       // -2..+2 typical
    public float Contrast { get; private set; } = 0.0f;              // -100..+100

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
            colorAdjustments.saturation.overrideState = false;
            colorAdjustments.colorFilter.overrideState = false;
            colorAdjustments.hueShift.overrideState = false;
        }

        EnablePostProcessingOnMainCamera();
    }

    private void EnablePostProcessingOnMainCamera()
    {
        var cam = Camera.main;
        if (cam == null) return;
        var additional = cam.GetComponent<UniversalAdditionalCameraData>();
        if (additional == null)
        {
            additional = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
        }
        additional.renderPostProcessing = true;
    }

    private void ApplyAllSettings()
    {
        ApplySoundVolume(SoundVolume);
        ApplyBrightness(BrightnessEV100);
        ApplyContrast(Contrast);
    }

    public void SetSoundVolume(float value)
    {
        SoundVolume = Mathf.Clamp01(value);
        ApplySoundVolume(SoundVolume);
    }

    public void SetBrightness(float ev100)
    {
        BrightnessEV100 = Mathf.Clamp(ev100, -2f, 2f);
        ApplyBrightness(BrightnessEV100);
    }

    public void SetContrast(float contrast)
    {
        Contrast = Mathf.Clamp(contrast, -100f, 100f);
        ApplyContrast(Contrast);
    }

    private static void ApplySoundVolume(float volume)
    {
        AudioListener.volume = Mathf.Clamp01(volume);
    }

    private void ApplyBrightness(float ev100)
    {
        if (colorAdjustments == null) EnsureGlobalPostFX();
        if (colorAdjustments != null)
        {
            colorAdjustments.postExposure.value = ev100;
        }
    }

    private void ApplyContrast(float contrast)
    {
        if (colorAdjustments == null) EnsureGlobalPostFX();
        if (colorAdjustments != null)
        {
            colorAdjustments.contrast.value = contrast;
        }
    }
}
