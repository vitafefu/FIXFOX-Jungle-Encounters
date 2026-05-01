using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Temporary Health")]
    [SerializeField] private int maxHealth = 3;

    [Header("Temporary Invincibility")]
    [SerializeField] private float invincibilityTime = 1f;

    private int currentHealth;
    private bool isInvincible;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => currentHealth <= 0;

    // مهم: هذا الاسم مطلوب لأن Enemy.cs يستعمله
    public bool IsInvincible => isInvincible;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (IsDead)
            return;

        if (isInvincible)
            return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        Debug.Log("Player damage: " + damage + ". Health: " + currentHealth + "/" + maxHealth);

        if (IsDead)
        {
            Debug.Log("Player died");
            return;
        }

        StartCoroutine(InvincibilityRoutine());
    }

    public void Heal(int amount)
    {
        if (IsDead)
            return;

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
    }

    private IEnumerator InvincibilityRoutine()
    {
        isInvincible = true;
        yield return new WaitForSeconds(invincibilityTime);
        isInvincible = false;
    }
}