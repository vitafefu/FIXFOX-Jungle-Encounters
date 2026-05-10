using UnityEngine;

public class TopTouchTilemapHazard2D : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private string playerTag = "Player";

    [Header("Top Touch Check")]
    [SerializeField] private float bottomContactTolerance = 0.25f;
    [SerializeField] private bool requirePlayerFalling = true;

    [Header("Hazard Damage")]
    [SerializeField] private int damageUnits = 2;
    [SerializeField] private DamageType damageType = DamageType.Spike;

    [Tooltip("Damage interval for spikes only. This is separate from enemy invincibility.")]
    [SerializeField] private float hazardDamageInterval = 0.25f;

    [Header("Poison")]
    [SerializeField] private bool applyPoison = true;
    [SerializeField] private int poisonDamageUnits = 1;
    [SerializeField] private int poisonTicks = 3;
    [SerializeField] private float poisonTickInterval = 1f;

    [Header("Knockback")]
    [SerializeField] private float knockbackForceX = 8f;
    [SerializeField] private float knockbackForceY = 10f;

    [Tooltip("How often the hazard can push the player while contact continues.")]
    [SerializeField] private float knockbackRepeatInterval = 0.12f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    private float lastHazardDamageTime = -Mathf.Infinity;
    private float lastKnockbackTime = -Mathf.Infinity;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryHandlePlayerContact(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryHandlePlayerContact(collision);
    }

    private void TryHandlePlayerContact(Collision2D collision)
    {
        GameObject target = collision.gameObject;

        if (!target.CompareTag(playerTag))
            return;

        PlayerHealth playerHealth = FindPlayerHealth(target);

        if (playerHealth == null)
            return;

        if (playerHealth.IsDead || playerHealth.CurrentHealthUnits <= 0)
            return;

        if (!IsPlayerTouchingFromTop(collision))
            return;

        Vector2 sourcePosition = GetContactSourcePosition(collision);

        DamageData hazardDamageData = new DamageData(
            amountUnits: damageUnits,
            type: damageType,
            sourcePosition: sourcePosition,
            sourceObject: gameObject,
            knockbackForceX: knockbackForceX,
            knockbackForceY: knockbackForceY
        );

        TryApplyKnockback(target, hazardDamageData, playerHealth);
        TryApplyHazardDamageAndPoison(target, sourcePosition, hazardDamageData, playerHealth);
    }

    private PlayerHealth FindPlayerHealth(GameObject target)
    {
        PlayerHealth playerHealth = target.GetComponent<PlayerHealth>();

        if (playerHealth == null)
            playerHealth = target.GetComponentInParent<PlayerHealth>();

        if (playerHealth == null)
            playerHealth = target.GetComponentInChildren<PlayerHealth>();

        return playerHealth;
    }

    private void TryApplyKnockback(GameObject target, DamageData damageData, PlayerHealth playerHealth)
    {
        if (playerHealth == null || playerHealth.IsDead || playerHealth.CurrentHealthUnits <= 0)
            return;

        if (Time.time < lastKnockbackTime + knockbackRepeatInterval)
            return;

        KnockbackReceiver2D knockbackReceiver = target.GetComponent<KnockbackReceiver2D>();

        if (knockbackReceiver == null)
            knockbackReceiver = target.GetComponentInParent<KnockbackReceiver2D>();

        if (knockbackReceiver == null)
            knockbackReceiver = target.GetComponentInChildren<KnockbackReceiver2D>();

        if (knockbackReceiver == null)
            return;

        knockbackReceiver.ApplyKnockback(damageData);
        lastKnockbackTime = Time.time;

        if (showDebugLogs)
            Debug.Log("TopTouchTilemapHazard2D: knockback applied.");
    }

    private void TryApplyHazardDamageAndPoison(
        GameObject target,
        Vector2 sourcePosition,
        DamageData damageData,
        PlayerHealth playerHealth)
    {
        if (playerHealth == null || playerHealth.IsDead || playerHealth.CurrentHealthUnits <= 0)
            return;

        if (Time.time < lastHazardDamageTime + hazardDamageInterval)
            return;

        PlayerDamageReceiver damageReceiver = target.GetComponent<PlayerDamageReceiver>();

        if (damageReceiver == null)
            damageReceiver = target.GetComponentInParent<PlayerDamageReceiver>();

        if (damageReceiver == null)
            damageReceiver = target.GetComponentInChildren<PlayerDamageReceiver>();

        if (damageReceiver == null)
            return;

        bool damageApplied = damageReceiver.ReceiveHazardDamage(damageData);

        if (!damageApplied)
            return;

        lastHazardDamageTime = Time.time;

        if (showDebugLogs)
            Debug.Log("TopTouchTilemapHazard2D: hazard damage applied.");

        // مهم جدًا:
        // إذا ضربة الشوك قتلت اللاعب، ممنوع نطبّق السم بعدها.
        if (playerHealth.IsDead || playerHealth.CurrentHealthUnits <= 0)
            return;

        if (applyPoison)
        {
            PlayerStatusEffects statusEffects = TryApplyPoison(target, sourcePosition);

            // أول ضربة من الشوك نفسها تعمل وميض أحمر ثم يرجع أخضر.
            if (statusEffects != null && statusEffects.IsPoisonActive)
                statusEffects.PlayPoisonDamageFlash();
        }
    }

    private bool IsPlayerTouchingFromTop(Collision2D collision)
    {
        Collider2D playerCollider = collision.collider;

        if (playerCollider == null)
            return false;

        if (requirePlayerFalling)
        {
            Rigidbody2D rb = collision.rigidbody;

            if (rb != null && rb.velocity.y > 0.05f)
                return false;
        }

        float playerBottomY = playerCollider.bounds.min.y;

        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint2D contact = collision.GetContact(i);

            if (contact.point.y <= playerBottomY + bottomContactTolerance)
                return true;
        }

        return false;
    }

    private Vector2 GetContactSourcePosition(Collision2D collision)
    {
        if (collision.contactCount > 0)
            return collision.GetContact(0).point;

        return transform.position;
    }

    private PlayerStatusEffects TryApplyPoison(GameObject target, Vector2 sourcePosition)
    {
        PlayerStatusEffects statusEffects = target.GetComponent<PlayerStatusEffects>();

        if (statusEffects == null)
            statusEffects = target.GetComponentInParent<PlayerStatusEffects>();

        if (statusEffects == null)
            statusEffects = target.GetComponentInChildren<PlayerStatusEffects>();

        if (statusEffects == null)
        {
            if (showDebugLogs)
                Debug.LogWarning("TopTouchTilemapHazard2D: PlayerStatusEffects not found. Poison skipped.");

            return null;
        }

        if (!statusEffects.CanApplyPoison())
            return statusEffects;

        DamageData poisonData = new DamageData(
            amountUnits: poisonDamageUnits,
            type: DamageType.Poison,
            sourcePosition: sourcePosition,
            sourceObject: gameObject,
            knockbackForceX: 0f,
            knockbackForceY: 0f
        );

        statusEffects.ApplyPoison(poisonData, poisonTicks, poisonTickInterval);

        return statusEffects;
    }
}