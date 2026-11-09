using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Menu Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private GameObject storyPanel;

    [Header("Settings")]
    [SerializeField] private Slider masterVolumeSlider;

    [Header("Game Start")]
    [SerializeField] private string firstLevelScene = "Level1";
    [SerializeField, Tooltip("Show the loading screen before the first level.")]
    private bool useLoadingScreen = true;

    private const string VolumeKey = "MasterVolume";

    private void Start()
    {
        Time.timeScale = 1f;

        float savedVolume = PlayerPrefs.GetFloat(VolumeKey, 0.8f);

        AudioListener.volume = savedVolume;

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.SetValueWithoutNotify(savedVolume);
        }

        ShowMainMenu();
    }

    public void ShowMainMenu() => ShowOnly(mainMenuPanel);

    public void OpenSettings() => ShowOnly(settingsPanel);

    public void OpenCredits() => ShowOnly(creditsPanel);

    public void OpenStory() => ShowOnly(storyPanel);

    private void ShowOnly(GameObject panel)
    {
        foreach (var p in new[] { mainMenuPanel, settingsPanel, creditsPanel, storyPanel })
        {
            if (p != null) p.SetActive(p == panel);
        }
    }

    public void SetMasterVolume(float volume)
    {
        AudioListener.volume = volume;

        PlayerPrefs.SetFloat(VolumeKey, volume);
        PlayerPrefs.Save();
    }

    public void StartNewGame()
    {
        if (useLoadingScreen)
        {
            SceneLoader.LoadWithLoadingScreen(firstLevelScene);
        }
        else
        {
            SceneManager.LoadScene(firstLevelScene);
        }
    }

    public void QuitGame()
    {
        SceneLoader.QuitGame();
    }
}