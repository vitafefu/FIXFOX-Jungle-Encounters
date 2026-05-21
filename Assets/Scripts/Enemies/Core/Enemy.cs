using System;
using UnityEngine;

/// <summary>
/// Base class for all enemies.
/// 
/// Health and damage use units:
/// 1 unit = quarter heart
/// 2 units = half heart
/// 4 units = full heart
/// </summary>
public class Enemy : MonoBehaviour, IDamageable
{
    public event Action<int, int> OnHealthChanged;
    public event Action<DamageData> OnDamaged;
    public event Action OnDied;

    [Header("Enemy Health")]
    [SerializeField] protected int maxHealthUnits = 4;
    [SerializeField] protected int startHealthUnits = 4;

    [Header("Contact Damage")]
    [SerializeField] protected int contactDamageUnits = 2; // 2 = half heart
    [SerializeField] protected float damageCooldown = 0.5f;

    [Header("Movement")]
    [SerializeField] protected float moveSpeed = 3f;

    [Header("Knockback Given To Player")]
    [SerializeField] protected float playerKnockbackForceX = 7f;
    [SerializeField] protected float playerKnockbackForceY = 2.5f;

    protected Rigidbody2D rb;
    protected KnockbackReceiver2D knockbackReceiver;

    private int currentHealthUnits;
    private bool isDead;
    private float lastDamageTime = -Mathf.Infinity;

    public int CurrentHealthUnits => currentHealthUnits;
    public int MaxHealthUnits => maxHealthUnits;
    public bool IsDead => isDead;
    public float MoveSpeed => moveSpeed;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        knockbackReceiver = GetComponent<KnockbackReceiver2D>();

        maxHealthUnits = Mathf.Max(1, maxHealthUnits);
        startHealthUnits = Mathf.Clamp(startHealthUnits, 1, maxHealthUnits);

        currentHealthUnits = startHealthUnits;
    }

    protected virtual void Start()
    {
        NotifyHealthChanged();
    }

    public virtual bool ReceiveDamage(DamageData damageData)
    {
        Debug.Log($"Enemy.ReceiveDamage called. Amount: {damageData.AmountUnits}, Source: {damageData.SourceObject?.name}, IsDead: {isDead}");

        if (isDead)
            return false;

        if (damageData.AmountUnits <= 0)
            return false;

        currentHealthUnits -= damageData.AmountUnits;
        currentHealthUnits = Mathf.Clamp(currentHealthUnits, 0, maxHealthUnits);

        Debug.Log($"Enemy health: {currentHealthUnits}/{maxHealthUnits}");

        OnDamaged?.Invoke(damageData);
        NotifyHealthChanged();

        if (knockbackReceiver != null)
            knockbackReceiver.ApplyKnockback(damageData);

        if (currentHealthUnits <= 0)
            Die();

        return true;
    }

    public virtual bool TakeDamage(int amountUnits)
    {
        DamageData damageData = new DamageData(
            amountUnits: amountUnits,
            type: DamageType.Normal,
            sourcePosition: transform.position,
            sourceObject: null,
            knockbackForceX: 0f,
            knockbackForceY: 0f
        );

        return ReceiveDamage(damageData);
    }
    protected virtual void Die()
    {
        if (isDead)
            return;

        isDead = true;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            PlayerKillCounter killCounter = player.GetComponent<PlayerKillCounter>();
            if (killCounter != null)
                killCounter.AddKill();
        }

        OnDied?.Invoke();
        Destroy(gameObject);
    }

    protected virtual bool CanDealContactDamage()
    {
        return Time.time >= lastDamageTime + damageCooldown;
    }

    protected virtual void TryDealContactDamage(GameObject targetObject)
    {
        if (isDead)
            return;

        if (targetObject == null)
            return;

        if (!targetObject.CompareTag("Player"))
            return;

        if (!CanDealContactDamage())
            return;

        PlayerDamageReceiver playerDamageReceiver = targetObject.GetComponent<PlayerDamageReceiver>();

        if (playerDamageReceiver == null)
            return;

        DamageData damageData = new DamageData(
            amountUnits: contactDamageUnits,
            type: DamageType.EnemyContact,
            sourcePosition: transform.position,
            sourceObject: gameObject,
            knockbackForceX: playerKnockbackForceX,
            knockbackForceY: playerKnockbackForceY
        );

        bool damageApplied = playerDamageReceiver.ReceiveDamage(damageData);

        if (damageApplied)
            lastDamageTime = Time.time;
    }

    protected virtual void OnCollisionEnter2D(Collision2D collision)
    {
        TryDealContactDamage(collision.gameObject);
    }

    protected virtual void OnCollisionStay2D(Collision2D collision)
    {
        TryDealContactDamage(collision.gameObject);
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        TryDealContactDamage(other.gameObject);
    }

    protected virtual void OnTriggerStay2D(Collider2D other)
    {
        TryDealContactDamage(other.gameObject);
    }

    private void NotifyHealthChanged()
    {
        OnHealthChanged?.Invoke(currentHealthUnits, maxHealthUnits);
    }
}