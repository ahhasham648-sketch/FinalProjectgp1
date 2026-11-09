using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GameUIController : MonoBehaviour
{
    public static GameUIController Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private GameObject hudRoot;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject deathPanel;
    [SerializeField] private GameObject victoryPanel;

    [Header("Boss Bar")]
    [SerializeField] private GameObject bossBarRoot;
    [SerializeField] private Image bossBarFill;
    [SerializeField] private TMP_Text bossNameText;

    [Header("HUD")]
    [SerializeField] private Image healthFill;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text livesText;
    [SerializeField] private TMP_Text objectiveText;
    [SerializeField] private string livesFormat = "LIVES  {0}";
    [SerializeField, Tooltip("Shows active potion boosts, e.g. 'ATTACK UP 7s'.")]
    private TMP_Text buffText;

    [Header("First Selected Buttons (keyboard / gamepad)")]
    [SerializeField] private GameObject pauseFirstSelected;
    [SerializeField] private GameObject deathFirstSelected;
    [SerializeField] private GameObject victoryFirstSelected;

    [Header("Input")]
    [SerializeField] private KeyCode pauseKey = KeyCode.Escape;

    [Header("Scenes")]
    [SerializeField] private string mainMenuScene = "MainMenu";
    [SerializeField, Tooltip("If true, Play Again restarts from the first level instead of the current one.")]
    private bool playAgainFromFirstLevel = false;
    [SerializeField] private string firstLevelScene = "Level1";

    [Header("Sounds")]
    [SerializeField] private AudioClip pauseSound;
    [SerializeField] private AudioClip deathScreenSound;
    [SerializeField] private AudioClip victorySound;
    [SerializeField, Range(0f, 1f)] private float soundVolume = 0.8f;

    public bool IsPaused { get; private set; }
    public bool IsDeathScreenOpen { get; private set; }

    private Health playerHealth;
    private respawn playerRespawn;

    private void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
        if (pausePanel != null) pausePanel.SetActive(false);
        if (deathPanel != null) deathPanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (bossBarRoot != null) bossBarRoot.SetActive(false);
        if (hudRoot != null) hudRoot.SetActive(true);
    }

    private void Start()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerHealth = player.GetComponent<Health>();
            playerRespawn = player.GetComponent<respawn>();
        }

        if (playerHealth != null)
        {
            playerHealth.HealthChanged += OnHealthChanged;
            OnHealthChanged(playerHealth.currentHealth, playerHealth.MaxHealth);
        }
        else
        {
            SetHealthHudVisible(false);
        }

        if (playerRespawn != null)
        {
            playerRespawn.LivesChanged += OnLivesChanged;
            OnLivesChanged(playerRespawn.LivesLeft, playerRespawn.MaxLives);
        }
        else if (livesText != null)
        {
            livesText.gameObject.SetActive(false);
        }

        var goals = LevelGoalManager.Instance;
        if (goals != null)
        {
            goals.ObjectiveChanged += OnObjectiveChanged;
            OnObjectiveChanged(goals.CurrentObjective);
        }
        else if (objectiveText != null)
        {
            objectiveText.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (playerHealth != null) playerHealth.HealthChanged -= OnHealthChanged;
        if (playerRespawn != null) playerRespawn.LivesChanged -= OnLivesChanged;
        if (LevelGoalManager.Instance != null) LevelGoalManager.Instance.ObjectiveChanged -= OnObjectiveChanged;
    }

    private void Update()
    {
        if (Input.GetKeyDown(pauseKey) && !IsDeathScreenOpen)
        {
            if (IsPaused) Resume();
            else Pause();
        }

        UpdateBuffText();
    }

    private PlayerBuffs playerBuffs;

    private void UpdateBuffText()
    {
        if (buffText == null) return;
        if (playerBuffs == null && playerHealth != null) playerBuffs = playerHealth.GetComponent<PlayerBuffs>();

        string text = "";
        if (playerBuffs != null)
        {
            if (playerBuffs.AttackTimeLeft > 0f) text += $"<color=#FF9A3C>ATTACK UP {Mathf.CeilToInt(playerBuffs.AttackTimeLeft)}s</color>   ";
            if (playerBuffs.SpeedTimeLeft > 0f) text += $"<color=#D98CFF>SPEED UP {Mathf.CeilToInt(playerBuffs.SpeedTimeLeft)}s</color>";
        }
        buffText.text = text;
    }

    private void OnHealthChanged(float current, float max)
    {
        if (healthFill != null) healthFill.fillAmount = max > 0f ? current / max : 0f;
        if (healthText != null) healthText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
    }

    private void OnLivesChanged(int lives, int max)
    {
        if (livesText != null) livesText.text = string.Format(livesFormat, lives, max);
    }

    private void OnObjectiveChanged(string objective)
    {
        if (objectiveText != null) objectiveText.text = objective;
    }

    private void SetHealthHudVisible(bool visible)
    {
        if (healthFill != null) healthFill.transform.parent.gameObject.SetActive(visible);
        if (healthText != null) healthText.gameObject.SetActive(visible);
    }

    public void Pause()
    {
        if (IsPaused || IsDeathScreenOpen) return;
        IsPaused = true;
        Time.timeScale = 0f;
        if (pausePanel != null) pausePanel.SetActive(true);
        Select(pauseFirstSelected);
        Sfx.Play(pauseSound, soundVolume);
    }

    public void Resume()
    {
        if (!IsPaused) return;
        IsPaused = false;
        Time.timeScale = 1f;
        if (pausePanel != null) pausePanel.SetActive(false);
        Sfx.Play(pauseSound, soundVolume);
    }

    public void ShowDeathScreen()
    {
        if (IsDeathScreenOpen) return;
        IsPaused = false;
        IsDeathScreenOpen = true;
        if (pausePanel != null) pausePanel.SetActive(false);
        if (deathPanel != null) deathPanel.SetActive(true);
        Time.timeScale = 0f;
        Select(deathFirstSelected);
        Sfx.Play(deathScreenSound, soundVolume);
    }

    public bool IsVictoryOpen { get; private set; }

    public void ShowVictoryScreen()
    {
        if (IsVictoryOpen) return;
        IsVictoryOpen = true;
        IsPaused = false;
        IsDeathScreenOpen = true;
        HideBossBar();
        if (pausePanel != null) pausePanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(true);
        Time.timeScale = 0f;
        Select(victoryFirstSelected);
        Sfx.Play(victorySound, soundVolume);
    }

    public void PlayFromStart()
    {
        SceneLoader.LoadWithLoadingScreen(firstLevelScene);
    }

    private EnemyHealth boss;

    public void ShowBossBar(EnemyHealth bossHealth, string title)
    {
        if (bossBarRoot == null || bossHealth == null) return;
        if (boss != null) boss.HealthChanged -= OnBossHealthChanged;
        boss = bossHealth;
        boss.HealthChanged += OnBossHealthChanged;
        if (bossNameText != null) bossNameText.text = title;
        OnBossHealthChanged(boss.CurrentHealth, boss.MaxHealth);
        bossBarRoot.SetActive(true);
    }

    public void HideBossBar()
    {
        if (boss != null) boss.HealthChanged -= OnBossHealthChanged;
        boss = null;
        if (bossBarRoot != null) bossBarRoot.SetActive(false);
    }

    private void OnBossHealthChanged(float current, float max)
    {
        if (bossBarFill != null) bossBarFill.fillAmount = max > 0f ? current / max : 0f;
    }

    public void PlayAgain()
    {
        if (playAgainFromFirstLevel) SceneLoader.LoadWithLoadingScreen(firstLevelScene);
        else SceneLoader.ReloadCurrentScene();
    }

    public void GoToMainMenu()
    {
        SceneLoader.LoadWithLoadingScreen(mainMenuScene);
    }

    public void ExitGame()
    {
        SceneLoader.QuitGame();
    }

    private static void Select(GameObject go)
    {
        if (go == null || EventSystem.current == null) return;
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(go);
    }
}
