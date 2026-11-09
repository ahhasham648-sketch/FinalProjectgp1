using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class LevelGoalManager : MonoBehaviour
{
    [Serializable]
    public class EnemyEntry
    {
        public EnemyHealth enemy;
        [Tooltip("Object shown when this enemy's turn comes (e.g. its patrol parent). Defaults to the enemy itself.")]
        public GameObject revealRoot;
    }

    public static LevelGoalManager Instance { get; private set; }

    [Header("Enemies (in order)")]
    [SerializeField] private List<EnemyEntry> enemiesInOrder = new List<EnemyEntry>();
    [SerializeField, Tooltip("Hide each enemy until the previous one is defeated.")]
    private bool revealEnemiesOneByOne = true;

    [Header("Next Level")]
    [SerializeField] private string nextSceneName = "Level2";
    [SerializeField] private float delayBeforeLoading = 2.5f;
    [SerializeField, Tooltip("Final level: show the Victory screen instead of loading the next scene.")]
    private bool showVictoryScreen = false;

    [Header("Objective Text")]
    [SerializeField] private string objectiveFormat = "Defeat the {0}  ({1}/{2})";
    [SerializeField] private string levelCompleteText = "Level Complete!";

    [Header("Sounds")]
    [SerializeField] private AudioClip enemyAppearSound;
    [SerializeField] private AudioClip victorySound;
    [SerializeField, Range(0f, 1f)] private float soundVolume = 0.9f;

    [Header("Events")]
    public UnityEvent onLevelComplete;

    public event Action<string> ObjectiveChanged;

    public bool AllEnemiesDefeated { get; private set; }
    public int DefeatedCount { get; private set; }
    public int TotalCount => enemiesInOrder.Count;
    public string CurrentObjective { get; private set; } = "";

    private int currentIndex;

    private void Awake()
    {
        Instance = this;
        enemiesInOrder.RemoveAll(e => e == null || e.enemy == null);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        for (int i = 0; i < enemiesInOrder.Count; i++)
        {
            enemiesInOrder[i].enemy.Died += OnEnemyDied;
            if (revealEnemiesOneByOne) RevealRoot(i).SetActive(i == 0);
        }

        if (enemiesInOrder.Count == 0)
        {
            Debug.LogWarning("[LevelGoalManager] No enemies assigned.");
            return;
        }

        UpdateObjective();
    }

    private GameObject RevealRoot(int i)
    {
        var e = enemiesInOrder[i];
        return e.revealRoot != null ? e.revealRoot : e.enemy.gameObject;
    }

    private void OnEnemyDied(EnemyHealth enemy)
    {
        enemy.Died -= OnEnemyDied;
        DefeatedCount++;

        while (currentIndex < enemiesInOrder.Count && enemiesInOrder[currentIndex].enemy.IsDead) currentIndex++;

        if (currentIndex >= enemiesInOrder.Count)
        {
            StartCoroutine(CompleteLevel());
            return;
        }

        if (revealEnemiesOneByOne)
        {
            var root = RevealRoot(currentIndex);
            if (!root.activeSelf)
            {
                root.SetActive(true);
                Sfx.Play(enemyAppearSound, soundVolume);
            }
        }

        UpdateObjective();
    }

    private void UpdateObjective()
    {
        var current = enemiesInOrder[currentIndex].enemy;
        SetObjective(string.Format(objectiveFormat, current.DisplayName, DefeatedCount, enemiesInOrder.Count));
    }

    private void SetObjective(string text)
    {
        CurrentObjective = text;
        ObjectiveChanged?.Invoke(text);
    }

    private IEnumerator CompleteLevel()
    {
        AllEnemiesDefeated = true;
        SetObjective(levelCompleteText);
        Sfx.Play(victorySound, soundVolume);
        onLevelComplete?.Invoke();

        yield return new WaitForSeconds(delayBeforeLoading);

        if (showVictoryScreen && GameUIController.Instance != null)
        {
            GameUIController.Instance.ShowVictoryScreen();
        }
        else
        {
            SceneLoader.LoadWithLoadingScreen(nextSceneName);
        }
    }
}
