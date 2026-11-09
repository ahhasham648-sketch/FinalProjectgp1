using UnityEngine;

public class LevelExit : MonoBehaviour
{
    [SerializeField] private string nextSceneName = "Level2";
    [SerializeField, Tooltip("If a LevelGoalManager is in the scene, the exit only works once every enemy is defeated.")]
    private bool requireAllEnemiesDefeated = true;

    private bool isLoading;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isLoading || !other.CompareTag("Player"))
        {
            return;
        }

        var goals = LevelGoalManager.Instance;
        if (requireAllEnemiesDefeated && goals != null && !goals.AllEnemiesDefeated)
        {
            return;
        }

        isLoading = true;
        SceneLoader.LoadWithLoadingScreen(nextSceneName);
    }
}
