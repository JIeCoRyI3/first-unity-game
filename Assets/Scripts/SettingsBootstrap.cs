using UnityEngine;
using UnityEngine.SceneManagement;

public static class SettingsBootstrap
{
    private static bool s_Registered;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterCallbacks()
    {
        if (s_Registered) return;
        s_Registered = true;
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Ensure SettingsManager exists even before first scene fully loads
        EnsureSettingsManager();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureSettingsManager();
        if (scene.name == "Settings")
        {
            if (Object.FindObjectOfType<SettingsController>() == null)
            {
                var go = new GameObject("Settings");
                go.AddComponent<SettingsController>();
            }
        }
    }

    private static void EnsureSettingsManager()
    {
        if (Object.FindObjectOfType<SettingsManager>() == null)
        {
            var go = new GameObject("SettingsManager");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<SettingsManager>();
        }
    }
}
