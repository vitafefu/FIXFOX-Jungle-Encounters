using UnityEngine;

public class BossPlayerSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject playerPrefab;   // префаб игрока
    [SerializeField] private Transform spawnPoint;      // пустой объект PlayerSpawn

    private void Start()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("Player Prefab not assigned in BossPlayerSpawner!");
            return;
        }

        if (spawnPoint == null)
        {
            Debug.LogError("Spawn Point not assigned in BossPlayerSpawner!");
            return;
        }

        // Создаём игрока в точке спавна
        Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
    }
}