using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public event Action<int, int> OnHealthChanged;
    public event Action<DamageData> OnDamaged;
    public event Action OnDied;
    public event Action OnRevived;

    [Header("Health Units")]
    [SerializeField] private int unitsPerHeart = 4;
    [SerializeField] private int maxHealthUnits = 12;
    [SerializeField] private int startHealthUnits = 12;

    [Header("Invincibility")]
    [SerializeField] private float invincibilityDuration = 1f;

    [Header("Debug")]
    [SerializeField] private bool debugHealthLogs = true;

    private int currentHealthUnits;
    private float invincibilityTimer;
    private bool isDead;

    public int UnitsPerHeart => unitsPerHeart;
    public int CurrentHealthUnits => currentHealthUnits;
    public int MaxHealthUnits => maxHealthUnits;
    public bool IsInvincible => invincibilityTimer > 0f;
    public bool IsDead => isDead;

    private void Awake()
    {
        unitsPerHeart = Mathf.Max(1, unitsPerHeart);
        maxHealthUnits = Mathf.Max(1, maxHealthUnits);
        startHealthUnits = Mathf.Clamp(startHealthUnits, 0, maxHealthUnits);

        currentHealthUnits = startHealthUnits;
    }

    private void Start()
    {
        NotifyHealthChanged();
    }

    private void Update()
    {
        UpdateInvincibilityTimer();
    }

    public bool TakeDamage(DamageData damageData)
    {
        if (isDead)
            return false;

        if (IsInvincible)
            return false;

        if (damageData.AmountUnits <= 0)
            return false;

        ApplyHealthLoss(damageData);

        if (!isDead)
            invincibilityTimer = invincibilityDuration;

        return true;
    }

    public bool TakeHazardDamage(DamageData damageData)
    {
        if (isDead)
            return false;

        if (damageData.AmountUnits <= 0)
            return false;

        ApplyHealthLoss(damageData);
        return true;
    }

    public bool TakeStatusDamage(DamageData damageData)
    {
        if (isDead)
            return false;

        if (damageData.AmountUnits <= 0)
            return false;

        ApplyHealthLoss(damageData);
        return true;
    }

    private void ApplyHealthLoss(DamageData damageData)
    {
        currentHealthUnits -= damageData.AmountUnits;
        currentHealthUnits = Mathf.Clamp(currentHealthUnits, 0, maxHealthUnits);

        OnDamaged?.Invoke(damageData);
        NotifyHealthChanged();

        if (debugHealthLogs)
            Debug.Log("Player Health: " + currentHealthUnits + "/" + maxHealthUnits);

        if (currentHealthUnits <= 0)
            Die();
    }

    public void Heal(int amountUnits)
    {
        if (isDead)
            return;

        if (amountUnits <= 0)
            return;

        currentHealthUnits += amountUnits;
        currentHealthUnits = Mathf.Clamp(currentHealthUnits, 0, maxHealthUnits);

        NotifyHealthChanged();
    }

    public void SetHealth(int healthUnits)
    {
        currentHealthUnits = Mathf.Clamp(healthUnits, 0, maxHealthUnits);
        NotifyHealthChanged();

        if (currentHealthUnits <= 0 && !isDead)
            Die();
    }

    public void SetMaxHealth(int newMaxHealthUnits, bool fillHealth)
    {
        maxHealthUnits = Mathf.Max(1, newMaxHealthUnits);

        if (fillHealth)
            currentHealthUnits = maxHealthUnits;
        else
            currentHealthUnits = Mathf.Clamp(currentHealthUnits, 0, maxHealthUnits);

        NotifyHealthChanged();

        if (currentHealthUnits <= 0 && !isDead)
            Die();
    }

    public void ResetInvincibility()
    {
        invincibilityTimer = 0f;
    }

    public void Revive(bool fillHealth = true)
    {
        isDead = false;
        invincibilityTimer = 0f;

        if (fillHealth)
            currentHealthUnits = maxHealthUnits;
        else
            currentHealthUnits = Mathf.Clamp(currentHealthUnits, 1, maxHealthUnits);

        NotifyHealthChanged();
        OnRevived?.Invoke();

        if (debugHealthLogs)
            Debug.Log("Player revived. Health: " + currentHealthUnits + "/" + maxHealthUnits);
    }

    private void UpdateInvincibilityTimer()
    {
        if (invincibilityTimer <= 0f)
            return;

        invincibilityTimer -= Time.deltaTime;

        if (invincibilityTimer < 0f)
            invincibilityTimer = 0f;
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;
        invincibilityTimer = 0f;

        if (debugHealthLogs)
            Debug.Log("Player died");

        OnDied?.Invoke();
    }

    private void NotifyHealthChanged()
    {
        OnHealthChanged?.Invoke(currentHealthUnits, maxHealthUnits);
    }
}