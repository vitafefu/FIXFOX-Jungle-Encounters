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
    private PlayerStatusEffects statusEffects;

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

        statusEffects = GetComponent<PlayerStatusEffects>();

        if (statusEffects == null)
            statusEffects = GetComponentInParent<PlayerStatusEffects>();

        if (statusEffects == null)
            statusEffects = GetComponentInChildren<PlayerStatusEffects>();

        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        originalColors = new Color[spriteRenderers.Length];

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
                originalColors[i] = spriteRenderers[i].color;
        }
    }

    // للأعداء والضرر العادي
    public bool ReceiveDamage(DamageData damageData)
    {
        if (playerHealth == null)
            return false;

        bool damageApplied = playerHealth.TakeDamage(damageData);

        if (!damageApplied)
            return false;

        // أي ضرر ناجح لازم يعمل فلاش أحمر
        PlayDamageFlash();

        if (knockbackReceiver != null)
            knockbackReceiver.ApplyKnockback(damageData);

        return true;
    }

    // للأشواك والـ hazards
    public bool ReceiveHazardDamage(DamageData damageData)
    {
        if (playerHealth == null)
            return false;

        bool damageApplied = playerHealth.TakeHazardDamage(damageData);

        if (!damageApplied)
            return false;

        // مهم:
        // حتى ضرر الشوك لازم يعمل فلاش أحمر.
        // إذا اللاعب مسموم، PlayDamageFlash سيطلب من PlayerStatusEffects
        // أن يعمل الفلاش ثم يرجع اللون أخضر.
        PlayDamageFlash();

        if (knockbackReceiver != null)
            knockbackReceiver.ApplyKnockback(damageData);

        return true;
    }

    public void PlayDamageFlash()
    {
        if (!useDamageFlash)
            return;

        if (playerHealth != null && playerHealth.IsDead)
            return;

        // إذا اللاعب مسموم:
        // لا نستخدم الفلاش العادي، لأن الفلاش العادي يرجع اللون للأصلي.
        // نحن نريد أحمر ثم أخضر.
        if (statusEffects != null && statusEffects.IsPoisonActive)
        {
            statusEffects.PlayPoisonDamageFlash();
            return;
        }

        // إذا اللاعب غير مسموم:
        // فلاش أحمر ثم رجوع للون الأصلي.
        if (damageFlashRoutine != null)
            StopCoroutine(damageFlashRoutine);

        damageFlashRoutine = StartCoroutine(DamageFlashRoutine(damageFlashColor, damageFlashDuration));
    }

    public void PlayCustomFlash(Color flashColor, float duration)
    {
        if (playerHealth != null && playerHealth.IsDead)
            return;

        // إذا اللاعب مسموم، لا نسمح لفلاش خارجي أن يرجعه للون الأصلي.
        // نعطيه فلاش السم الأحمر ثم يرجع أخضر.
        if (statusEffects != null && statusEffects.IsPoisonActive)
        {
            statusEffects.PlayPoisonDamageFlash();
            return;
        }

        if (damageFlashRoutine != null)
            StopCoroutine(damageFlashRoutine);

        damageFlashRoutine = StartCoroutine(DamageFlashRoutine(flashColor, duration));
    }

    private IEnumerator DamageFlashRoutine(Color color, float duration)
    {
        SetSpriteColor(color);

        yield return new WaitForSeconds(duration);

        // إذا أثناء الفلاش صار اللاعب مسموم، لا نرجعه للأصلي.
        // خلي PlayerStatusEffects يرجعه للأخضر.
        if (statusEffects != null && statusEffects.IsPoisonActive)
            statusEffects.PlayPoisonDamageFlash();
        else
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