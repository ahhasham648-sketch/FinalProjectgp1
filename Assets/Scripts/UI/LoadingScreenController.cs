using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingScreenController : MonoBehaviour
{
    [System.Serializable]
    public class SceneTitle
    {
        public string sceneName;
        public string title;
    }

    [Header("UI")]
    [SerializeField] private TMP_Text levelTitleText;
    [SerializeField, Tooltip("Environment name shown while each scene loads (design doc: Forest, Caves, Spire).")]
    private SceneTitle[] sceneTitles =
    {
        new SceneTitle { sceneName = "Level1", title = "THE FOREST" },
        new SceneTitle { sceneName = "Level2", title = "THE CAVES" },
        new SceneTitle { sceneName = "Level3", title = "THE SPIRE" },
        new SceneTitle { sceneName = "MainMenu", title = "" }
    };
    [SerializeField] private Image progressFill;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text tipText;

    [Header("Timing")]
    [SerializeField, Tooltip("Keeps the screen up at least this long so it doesn't just flash.")]
    private float minimumDisplayTime = 1.5f;
    [SerializeField] private float barFillSpeed = 1.5f;

    [Header("Fallback")]
    [SerializeField, Tooltip("Loaded if this scene is opened directly (no target set).")]
    private string fallbackScene = "Level1";

    [Header("Tips")]
    [SerializeField, TextArea]
    private string[] tips =
    {
        "Press J or Left Mouse to attack.",
        "Press ESC to pause the game.",
        "Defeat the enemies in order to open the way forward.",
        "Enemies hit hard - strike, then step back."
    };

    [Header("Sounds")]
    [SerializeField] private AudioClip loadCompleteSound;
    [SerializeField, Range(0f, 1f)] private float soundVolume = 0.7f;

    private void Start()
    {
        Time.timeScale = 1f;
        if (tipText != null && tips != null && tips.Length > 0) tipText.text = tips[Random.Range(0, tips.Length)];
        StartCoroutine(Load());
    }

    private IEnumerator Load()
    {
        string target = string.IsNullOrEmpty(SceneLoader.TargetScene) ? fallbackScene : SceneLoader.TargetScene;
        SetProgress(0f);

        if (levelTitleText != null)
        {
            string title = "";
            foreach (var t in sceneTitles)
            {
                if (t != null && t.sceneName == target) { title = t.title; break; }
            }
            levelTitleText.text = title;
            levelTitleText.gameObject.SetActive(!string.IsNullOrEmpty(title));
        }

        yield return null;

        AsyncOperation op = SceneManager.LoadSceneAsync(target);
        if (op == null)
        {
            Debug.LogError($"[LoadingScreen] Scene '{target}' could not be loaded. Is it in Build Settings?");
            yield break;
        }
        op.allowSceneActivation = false;

        float shown = 0f;
        float elapsed = 0f;
        while (true)
        {
            elapsed += Time.unscaledDeltaTime;
            float real = Mathf.Clamp01(op.progress / 0.9f);
            float timeCap = minimumDisplayTime > 0f ? Mathf.Clamp01(elapsed / minimumDisplayTime) : 1f;
            shown = Mathf.MoveTowards(shown, Mathf.Min(real, timeCap), barFillSpeed * Time.unscaledDeltaTime);
            SetProgress(shown);

            if (shown >= 1f && op.progress >= 0.9f) break;
            yield return null;
        }

        Sfx.Play(loadCompleteSound, soundVolume);
        yield return new WaitForSecondsRealtime(0.15f);
        op.allowSceneActivation = true;
    }

    private void SetProgress(float value)
    {
        if (progressFill != null) progressFill.fillAmount = value;
        if (progressText != null) progressText.text = $"LOADING...  {Mathf.RoundToInt(value * 100f)}%";
    }
}
