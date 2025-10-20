using UnityEngine;
using UnityEngine.SceneManagement;

public static class MenuBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureMenuExists()
    {
        var activeScene = SceneManager.GetActiveScene();
        if (activeScene.name != "Menu")
        {
            return;
        }

        if (Object.FindObjectOfType<MenuController>() != null)
        {
            return;
        }

        var go = new GameObject("Menu");
        go.AddComponent<MenuController>();
    }
}
