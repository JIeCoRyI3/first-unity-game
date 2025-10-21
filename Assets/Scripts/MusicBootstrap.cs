using UnityEngine;
using UnityEngine.SceneManagement;

public static class MusicBootstrap
{
    private static bool s_Registered;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        if (s_Registered) return;
        s_Registered = true;
        EnsureMusicManager();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureMusicManager();
        // Music should only play during the actual game scene
        if (scene.name != "Snake" && MusicManager.Instance != null)
        {
            MusicManager.Instance.StopImmediate();
        }
    }

    private static void EnsureMusicManager()
    {
        if (MusicManager.Instance == null)
        {
            var go = new GameObject("MusicManager");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<MusicManager>();
        }
    }
}
