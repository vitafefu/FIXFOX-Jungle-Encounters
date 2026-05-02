using System.Collections;
using UnityEngine;

public class PlayerDamageReceiver : MonoBehaviour
{
    [Header("Damage Flash")]
    [SerializeField] private bool useDamageFlash = true;
    [SerializeField] private Color damageFlashColor = new Color(1f, 0.35f, 0.35f, 1f);
    [SerializeField] private float damageFlashDuration = 0.12f;

    private PlayerHealth playerHealth;
    private KnockbackReceiver2D knockbackReceiver;

    private SpriteRenderer[] spriteRenderers;
    private Color[] originalColors;
    private Coroutine damageFlashRoutine;

    public bool CanReceiveDamage
    {
        get
        {
            if (playerHealth == null)
                return false;

            return !playerHealth.IsDead && !playerHealth.IsInvincible;
        }
    }

    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        knockbackReceiver = GetComponent<KnockbackReceiver2D>();

        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        originalColors = new Color[spriteRenderers.Length];

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
                originalColors[i] = spriteRenderers[i].color;
        }
    }

    public bool ReceiveDamage(DamageData damageData)
    {
        if (playerHealth == null)
            return false;

        bool damageApplied = playerHealth.TakeDamage(damageData);

        if (!damageApplied)
            return false;

        if (useDamageFlash)
            StartDamageFlash();

        if (knockbackReceiver != null)
            knockbackReceiver.ApplyKnockback(damageData);

        return true;
    }

    private void StartDamageFlash()
    {
        if (damageFlashRoutine != null)
            StopCoroutine(damageFlashRoutine);

        damageFlashRoutine = StartCoroutine(DamageFlashRoutine());
    }

    private IEnumerator DamageFlashRoutine()
    {
        SetSpriteColor(damageFlashColor);

        yield return new WaitForSeconds(damageFlashDuration);

        RestoreSpriteColors();

        damageFlashRoutine = null;
    }

    private void SetSpriteColor(Color color)
    {
        if (spriteRenderers == null)
            return;

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
                spriteRenderers[i].color = color;
        }
    }

    private void RestoreSpriteColors()
    {
        if (spriteRenderers == null || originalColors == null)
            return;

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null && i < originalColors.Length)
                spriteRenderers[i].color = originalColors[i];
        }
    }

    private void OnDisable()
    {
        if (damageFlashRoutine != null)
        {
            StopCoroutine(damageFlashRoutine);
            damageFlashRoutine = null;
        }

        RestoreSpriteColors();
    }
}