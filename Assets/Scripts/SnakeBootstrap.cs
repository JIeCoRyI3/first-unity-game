using UnityEngine;
using UnityEngine.SceneManagement;

public static class SnakeBootstrap
{
    private static bool s_Registered;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterCallbacks()
    {
        if (s_Registered) return;
        s_Registered = true;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "Snake") return;
        var existing = Object.FindObjectOfType<SnakeGame>();
        if (existing != null) return;
        var controller = new GameObject("SnakeGame");
        controller.AddComponent<SnakeGame>();
    }
}
