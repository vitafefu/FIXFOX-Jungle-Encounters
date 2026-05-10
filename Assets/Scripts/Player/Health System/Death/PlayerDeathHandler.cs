using UnityEngine;

public class PlayerDeathHandler : MonoBehaviour
{
    [Header("Death Settings")]
    [SerializeField] private bool disablePlayerController = true;
    [SerializeField] private bool disableColliders = true;
    [SerializeField] private bool stopRigidbodyOnDeath = true;
    [SerializeField] private bool respawnAfterDeath = true;

    [Header("Animation")]
    [SerializeField] private bool useDeathAnimation = false;
    [SerializeField] private string isDeadParameterName = "isDead";

    private PlayerHealth playerHealth;
    private PlayerRespawnHandler respawnHandler;
    private PlayerController playerController;
    private Rigidbody2D rb;
    private Collider2D[] colliders;
    private Animator animator;

    private int isDeadParam;

    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        respawnHandler = GetComponent<PlayerRespawnHandler>();
        playerController = GetComponent<PlayerController>();
        rb = GetComponent<Rigidbody2D>();
        colliders = GetComponents<Collider2D>();
        animator = GetComponentInChildren<Animator>();

        isDeadParam = Animator.StringToHash(isDeadParameterName);
    }

    private void OnEnable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnDied += HandleDeath;
            playerHealth.OnRevived += HandleRevive;
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnDied -= HandleDeath;
            playerHealth.OnRevived -= HandleRevive;
        }
    }

    private void HandleDeath()
    {
        ClearStatusEffectsEverywhere();

        if (useDeathAnimation && animator != null)
            animator.SetBool(isDeadParam, true);

        if (disablePlayerController && playerController != null)
            playerController.enabled = false;

        if (disableColliders)
            SetCollidersEnabled(false);

        if (stopRigidbodyOnDeath && rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        if (respawnAfterDeath && respawnHandler != null)
            respawnHandler.Respawn();
    }

    private void HandleRevive()
    {
        ClearStatusEffectsEverywhere();

        if (useDeathAnimation && animator != null)
            animator.SetBool(isDeadParam, false);
    }

    private void ClearStatusEffectsEverywhere()
    {
        PlayerStatusEffects[] effects = GetComponentsInChildren<PlayerStatusEffects>(true);

        for (int i = 0; i < effects.Length; i++)
        {
            if (effects[i] != null)
                effects[i].ClearAllEffects();
        }

        PlayerStatusEffects parentEffect = GetComponentInParent<PlayerStatusEffects>();

        if (parentEffect != null)
            parentEffect.ClearAllEffects();
    }

    private void SetCollidersEnabled(bool enabled)
    {
        if (colliders == null)
            return;

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
                colliders[i].enabled = enabled;
        }
    }
}