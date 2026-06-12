using UnityEngine;
using TMPro;

public class LevelFinishZone : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject levelCompletePanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI resultText;

    [Header("Messages")]
    [SerializeField] private string gameOverTitle = "GAME OVER";

    [Header("Boss Area")]
    [SerializeField] private Vector2 bossTeleportPosition = new Vector2(-85.2f, -341f);

    [Header("Camera")]
    [SerializeField] private bool moveCameraWithPlayer = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private bool teleportedToBoss;
    private bool gameOverShown;

    private GameObject currentPlayer;
    private PlayerWallet currentWallet;
    private PlayerKillCounter currentKillCounter;
    private PlayerRespawnHandler currentRespawnHandler;

    private Boss boss;
    private Transform bossRespawnPoint;

    private void Start()
    {
        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(false);

        CreateBossRespawnPoint();
        FindAndSubscribeToBoss();

        if (showDebugLogs)
        {
            Debug.Log("LevelFinishZone started.");
            Debug.Log("Boss found automatically: " + (boss != null));
        }
    }

    private void Update()
    {
        if (boss == null && !gameOverShown)
            FindAndSubscribeToBoss();
    }

    private void OnDestroy()
    {
        if (boss != null)
            boss.OnDied -= OnBossDied;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (teleportedToBoss)
            return;

        if (!other.CompareTag("Player"))
            return;

        TryTeleportToBoss(other.gameObject);
    }

    private void TryTeleportToBoss(GameObject player)
    {
        currentPlayer = player;
        currentWallet = player.GetComponent<PlayerWallet>();
        currentKillCounter = player.GetComponent<PlayerKillCounter>();
        currentRespawnHandler = player.GetComponent<PlayerRespawnHandler>();

        if (currentWallet == null)
        {
            Debug.LogError("LevelFinishZone: Player does not have PlayerWallet.");
            return;
        }

        if (currentKillCounter == null)
        {
            Debug.LogError("LevelFinishZone: Player does not have PlayerKillCounter.");
            return;
        }

        int remainingCoins = CountRemainingCoins();
        int remainingRegularEnemies = CountRegularEnemiesOnly();

        bool allCoinsCollected = remainingCoins == 0;
        bool allEnemiesKilled = remainingRegularEnemies == 0;

        if (showDebugLogs)
        {
            Debug.Log("Trying to teleport to boss...");
            Debug.Log("Remaining coins: " + remainingCoins);
            Debug.Log("Remaining regular enemies without boss: " + remainingRegularEnemies);
            Debug.Log("Player coins: " + currentWallet.Coins);
            Debug.Log("Player kills: " + currentKillCounter.KilledEnemies);
        }

        if (!allCoinsCollected || !allEnemiesKilled)
            return;

        teleportedToBoss = true;

        UpdatePlayerRespawnPoint();
        TeleportPlayer(player);

        if (moveCameraWithPlayer)
            ForceCameraSnapToPlayer(player);

        if (showDebugLogs)
            Debug.Log("Player teleported instantly to boss area.");
    }

    private int CountRemainingCoins()
    {
        Coin[] coins = FindObjectsByType<Coin>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        return coins.Length;
    }

    private int CountRegularEnemiesOnly()
    {
        Enemy[] enemies = FindObjectsByType<Enemy>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        int count = 0;

        foreach (Enemy enemy in enemies)
        {
            if (enemy == null)
                continue;

            // الزعيم يرث من Enemy، لذلك نستثنيه من شرط أعداء المرحلة الأولى
            if (enemy is Boss)
                continue;

            if (!enemy.IsDead)
                count++;
        }

        return count;
    }

    private void CreateBossRespawnPoint()
    {
        GameObject point = new GameObject("BossRespawnPoint_Runtime");

        point.transform.position = new Vector3(
            bossTeleportPosition.x,
            bossTeleportPosition.y,
            0f
        );

        bossRespawnPoint = point.transform;
    }

    private void UpdatePlayerRespawnPoint()
    {
        if (currentRespawnHandler == null)
        {
            if (showDebugLogs)
                Debug.LogWarning("LevelFinishZone: PlayerRespawnHandler not found on player.");

            return;
        }

        currentRespawnHandler.SetSpawnPoint(bossRespawnPoint);

        if (showDebugLogs)
            Debug.Log("Player respawn point updated to boss area.");
    }

    private void TeleportPlayer(GameObject player)
    {
        Vector3 targetPosition = new Vector3(
            bossTeleportPosition.x,
            bossTeleportPosition.y,
            player.transform.position.z
        );

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            RigidbodyInterpolation2D oldInterpolation = rb.interpolation;

            rb.interpolation = RigidbodyInterpolation2D.None;
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;

            rb.position = targetPosition;
            player.transform.position = targetPosition;

            Physics2D.SyncTransforms();

            rb.interpolation = oldInterpolation;
            rb.WakeUp();
        }
        else
        {
            player.transform.position = targetPosition;
            Physics2D.SyncTransforms();
        }
    }

    private void ForceCameraSnapToPlayer(GameObject player)
    {
        Camera mainCamera = Camera.main;

        if (mainCamera == null)
        {
            if (showDebugLogs)
                Debug.LogWarning("LevelFinishZone: Main Camera not found.");

            return;
        }

        // هذا يستدعي دالة ForceSnapToTarget داخل سكربت الكاميرا إذا كانت موجودة
        mainCamera.gameObject.SendMessage(
            "ForceSnapToTarget",
            player.transform,
            SendMessageOptions.DontRequireReceiver
        );

        // احتياط إضافي: ننقل Transform الكاميرا نفسه
        Vector3 cameraPosition = mainCamera.transform.position;

        mainCamera.transform.position = new Vector3(
            player.transform.position.x,
            player.transform.position.y,
            cameraPosition.z
        );

        Physics2D.SyncTransforms();

        if (showDebugLogs)
            Debug.Log("Camera force snap command sent to camera follow script.");
    }

    private void FindAndSubscribeToBoss()
    {
        Boss foundBoss = FindFirstObjectByType<Boss>();

        if (foundBoss == null)
            return;

        if (boss == foundBoss)
            return;

        if (boss != null)
            boss.OnDied -= OnBossDied;

        boss = foundBoss;
        boss.OnDied += OnBossDied;

        if (showDebugLogs)
            Debug.Log("LevelFinishZone subscribed to Boss death automatically.");
    }

    private void OnBossDied()
    {
        if (gameOverShown)
            return;

        ShowGameOver();
    }

    private void ShowGameOver()
    {
        gameOverShown = true;

        if (levelCompletePanel == null)
        {
            Debug.LogError("LevelFinishZone: Level Complete Panel is not assigned.");
            return;
        }

        if (currentPlayer == null)
            currentPlayer = GameObject.FindGameObjectWithTag("Player");

        if (currentWallet == null && currentPlayer != null)
            currentWallet = currentPlayer.GetComponent<PlayerWallet>();

        if (currentKillCounter == null && currentPlayer != null)
            currentKillCounter = currentPlayer.GetComponent<PlayerKillCounter>();

        int coins = currentWallet != null ? currentWallet.Coins : 0;
        int kills = currentKillCounter != null ? currentKillCounter.KilledEnemies : 0;

        if (titleText != null)
            titleText.text = gameOverTitle;

        if (resultText != null)
        {
            resultText.text =
                $"Алмазы: {coins}\n" +
                $"Убито врагов: {kills}";
        }

        FixGameOverTextLayout();

        levelCompletePanel.SetActive(true);
    }

    private void FixGameOverTextLayout()
    {
        if (titleText != null)
        {
            RectTransform titleRect = titleText.GetComponent<RectTransform>();

            titleRect.anchorMin = new Vector2(0.5f, 0.5f);
            titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.anchoredPosition = new Vector2(0f, 90f);
            titleRect.sizeDelta = new Vector2(700f, 90f);

            titleText.alignment = TextAlignmentOptions.Center;
            titleText.fontSize = 48f;
        }

        if (resultText != null)
        {
            RectTransform resultRect = resultText.GetComponent<RectTransform>();

            resultRect.anchorMin = new Vector2(0.5f, 0.5f);
            resultRect.anchorMax = new Vector2(0.5f, 0.5f);
            resultRect.pivot = new Vector2(0.5f, 0.5f);
            resultRect.anchoredPosition = new Vector2(0f, -40f);
            resultRect.sizeDelta = new Vector2(700f, 180f);

            resultText.alignment = TextAlignmentOptions.Center;
            resultText.fontSize = 32f;
        }
    }
}