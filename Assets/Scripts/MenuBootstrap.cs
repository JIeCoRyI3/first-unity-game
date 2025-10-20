using UnityEngine;
using UnityEngine.SceneManagement;

public static class MenuBootstrap
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
        if (scene.name != "Menu") return;
        if (Object.FindObjectOfType<MenuController>() != null) return;
        var go = new GameObject("Menu");
        go.AddComponent<MenuController>();
    }
}
