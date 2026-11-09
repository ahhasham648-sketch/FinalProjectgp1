using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneLoader
{
    public const string LoadingSceneName = "LoadingScreen";

    public static string TargetScene { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Time.timeScale = 1f;
    }

    public static void LoadWithLoadingScreen(string sceneName)
    {
        Time.timeScale = 1f;
        TargetScene = sceneName;

        if (Application.CanStreamedLevelBeLoaded(LoadingSceneName))
        {
            SceneManager.LoadScene(LoadingSceneName);
        }
        else
        {
            Debug.LogWarning($"[SceneLoader] '{LoadingSceneName}' is not in Build Settings, loading '{sceneName}' directly.");
            SceneManager.LoadScene(sceneName);
        }
    }

    public static void ReloadCurrentScene()
    {
        LoadWithLoadingScreen(SceneManager.GetActiveScene().name);
    }

    public static void QuitGame()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
