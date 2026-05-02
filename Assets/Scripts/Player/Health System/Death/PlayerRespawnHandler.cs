using System.Collections;
using UnityEngine;

public class PlayerRespawnHandler : MonoBehaviour
{
    [Header("Respawn Settings")]
    [SerializeField] private Transform currentSpawnPoint;

    [Tooltip("0 = instant respawn. Use delay later if you add death animation.")]
    [SerializeField] private float respawnDelay = 0f;

    [SerializeField] private bool useStartPositionIfNoSpawnPoint = true;
    [SerializeField] private bool fillHealthAfterRespawn = true;

    [Header("Components To Restore")]
    [SerializeField] private MonoBehaviour[] behavioursToEnableAfterRespawn;
    [SerializeField] private Collider2D[] collidersToEnableAfterRespawn;

    private Vector3 startPosition;
    private PlayerHealth playerHealth;
    private Rigidbody2D rb;
    private Coroutine respawnRoutine;

    private void Awake()
    {
        startPosition = transform.position;
        playerHealth = GetComponent<PlayerHealth>();
        rb = GetComponent<Rigidbody2D>();
    }

    public void SetSpawnPoint(Transform newSpawnPoint)
    {
        currentSpawnPoint = newSpawnPoint;
    }

    public void Respawn()
    {
        if (respawnRoutine != null)
        {
            StopCoroutine(respawnRoutine);
            respawnRoutine = null;
        }

        if (respawnDelay <= 0f)
        {
            RespawnNow();
            return;
        }

        respawnRoutine = StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnDelay);

        RespawnNow();

        respawnRoutine = null;
    }

    private void RespawnNow()
    {
        Vector3 respawnPosition = GetRespawnPosition();

        if (rb != null)
        {
            RigidbodyInterpolation2D oldInterpolation = rb.interpolation;

            // نوقف أي تأثير فيزيائي أو interpolation مؤقتاً
            rb.interpolation = RigidbodyInterpolation2D.None;
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;

            // Teleport حقيقي
            rb.position = respawnPosition;
            transform.position = respawnPosition;

            Physics2D.SyncTransforms();

            // نرجع الإعداد القديم
            rb.interpolation = oldInterpolation;
            rb.WakeUp();
        }
        else
        {
            transform.position = respawnPosition;
            Physics2D.SyncTransforms();
        }

        EnableColliders(true);
        EnableBehaviours(true);

        if (playerHealth != null)
            playerHealth.Revive(fillHealthAfterRespawn);
    }

    private Vector3 GetRespawnPosition()
    {
        if (currentSpawnPoint != null)
            return currentSpawnPoint.position;

        if (useStartPositionIfNoSpawnPoint)
            return startPosition;

        return transform.position;
    }

    private void EnableBehaviours(bool enabled)
    {
        if (behavioursToEnableAfterRespawn == null)
            return;

        for (int i = 0; i < behavioursToEnableAfterRespawn.Length; i++)
        {
            if (behavioursToEnableAfterRespawn[i] != null)
                behavioursToEnableAfterRespawn[i].enabled = enabled;
        }
    }

    private void EnableColliders(bool enabled)
    {
        if (collidersToEnableAfterRespawn == null)
            return;

        for (int i = 0; i < collidersToEnableAfterRespawn.Length; i++)
        {
            if (collidersToEnableAfterRespawn[i] != null)
                collidersToEnableAfterRespawn[i].enabled = enabled;
        }
    }
}