using UnityEngine;
using TMPro;

public class LevelFinishZone : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject levelCompletePanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI resultText;

    [Header("Messages")]
    [SerializeField] private string completeTitle = "Поздравляем!";
    [SerializeField] private string completeMessage = "Первый этап завершён";

    private int totalEnemiesAtStart;
    private bool levelCompleted;

    private void Start()
    {
        totalEnemiesAtStart = FindObjectsByType<Enemy>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        ).Length;

        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (levelCompleted)
            return;

        if (!other.CompareTag("Player"))
            return;

        TryCompleteLevel(other.gameObject);
    }

    private void TryCompleteLevel(GameObject player)
    {
        if (levelCompletePanel == null)
        {
            Debug.LogError("Level Complete Panel is not assigned!");
            return;
        }

        PlayerKillCounter killCounter = player.GetComponent<PlayerKillCounter>();
        PlayerWallet wallet = player.GetComponent<PlayerWallet>();

        if (killCounter == null)
        {
            Debug.LogError("Player does not have PlayerKillCounter!");
            return;
        }

        if (wallet == null)
        {
            Debug.LogError("Player does not have PlayerWallet!");
            return;
        }

        int remainingCoins = FindObjectsByType<Coin>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        ).Length;

        bool allCoinsCollected = remainingCoins == 0;
        bool allEnemiesKilled = killCounter.KilledEnemies >= totalEnemiesAtStart;

        if (!allCoinsCollected || !allEnemiesKilled)
            return;

        levelCompleted = true;

        if (titleText != null)
            titleText.text = completeTitle;

        if (resultText != null)
        {
            resultText.text =
                $"{completeMessage}\n" +
                $"Алмазы: {wallet.Coins}\n" +
                $"Убито врагов: {killCounter.KilledEnemies}";
        }

        levelCompletePanel.SetActive(true);
    }
}