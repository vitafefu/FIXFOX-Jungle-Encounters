using System.Collections;
using UnityEngine;

public class PlayerStatusEffects : MonoBehaviour
{
    [Header("Poison Visuals")]
    [SerializeField] private Color poisonTintColor = new Color(0.45f, 1f, 0.35f, 1f);
    [SerializeField] private Color poisonDamageFlashColor = new Color(1f, 0.15f, 0.15f, 1f);
    [SerializeField] private float poisonDamageFlashDuration = 0.12f;

    [Header("Poison Safety")]
    [Tooltip("وقت قصير يمنع إعادة تطبيق السم مباشرة بعد الموت أو الرسبون.")]
    [SerializeField] private float blockPoisonAfterDeathOrRespawn = 0.35f;

    private PlayerHealth playerHealth;

    private SpriteRenderer[] spriteRenderers;
    private Color[] originalColors;

    private Coroutine poisonRoutine;
    private Coroutine poisonFlashRoutine;

    private bool poisonActive;
    private int poisonVersion;
    private float blockPoisonUntilTime;

    public bool IsPoisonActive => poisonActive;

    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();

        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        originalColors = new Color[spriteRenderers.Length];

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
                originalColors[i] = spriteRenderers[i].color;
        }
    }

    private void OnEnable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnDied += HandleDeathOrRespawn;
            playerHealth.OnRevived += HandleDeathOrRespawn;
            playerHealth.OnHealthChanged += HandleHealthChanged;
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnDied -= HandleDeathOrRespawn;
            playerHealth.OnRevived -= HandleDeathOrRespawn;
            playerHealth.OnHealthChanged -= HandleHealthChanged;
        }

        ClearAllEffects();
    }

    private void OnDestroy()
    {
        ClearAllEffects();
    }

    private void Update()
    {
        if (ShouldStopPoison())
            ClearAllEffects();
    }

    private void HandleDeathOrRespawn()
    {
        BlockPoisonFor(blockPoisonAfterDeathOrRespawn);
        ClearAllEffects();
    }

    private void HandleHealthChanged(int currentHealth, int maxHealth)
    {
        if (currentHealth <= 0)
        {
            BlockPoisonFor(blockPoisonAfterDeathOrRespawn);
            ClearAllEffects();
        }
    }

    public void BlockPoisonFor(float seconds)
    {
        if (seconds <= 0f)
            return;

        blockPoisonUntilTime = Mathf.Max(blockPoisonUntilTime, Time.time + seconds);
    }

    public bool CanApplyPoison()
    {
        if (playerHealth == null)
            return false;

        if (Time.time < blockPoisonUntilTime)
            return false;

        if (ShouldStopPoison())
            return false;

        return true;
    }

    public void ApplyPoison(DamageData poisonDamageData, int ticks, float tickInterval)
    {
        if (!CanApplyPoison())
            return;

        if (ticks <= 0)
            return;

        if (tickInterval <= 0f)
            tickInterval = 0.1f;

        StopPoison();

        poisonVersion++;
        poisonActive = true;

        ApplyPoisonTint();

        int versionAtStart = poisonVersion;
        poisonRoutine = StartCoroutine(PoisonRoutine(poisonDamageData, ticks, tickInterval, versionAtStart));
    }

    public void StopPoison()
    {
        poisonVersion++;

        if (poisonRoutine != null)
        {
            StopCoroutine(poisonRoutine);
            poisonRoutine = null;
        }

        if (poisonFlashRoutine != null)
        {
            StopCoroutine(poisonFlashRoutine);
            poisonFlashRoutine = null;
        }

        poisonActive = false;
        RestoreSpriteColors();
    }

    public void ClearAllEffects()
    {
        StopPoison();
    }

    public void PlayPoisonDamageFlash()
    {
        PlayPoisonDamageFlash(poisonVersion);
    }

    private IEnumerator PoisonRoutine(DamageData poisonDamageData, int ticks, float tickInterval, int version)
    {
        for (int i = 0; i < ticks; i++)
        {
            float timer = 0f;

            while (timer < tickInterval)
            {
                if (version != poisonVersion)
                    yield break;

                if (ShouldStopPoison())
                {
                    StopPoison();
                    yield break;
                }

                timer += Time.deltaTime;
                yield return null;
            }

            if (version != poisonVersion)
                yield break;

            if (ShouldStopPoison())
            {
                StopPoison();
                yield break;
            }

            bool damageApplied = playerHealth.TakeStatusDamage(poisonDamageData);

            if (!damageApplied)
            {
                StopPoison();
                yield break;
            }

            if (ShouldStopPoison())
            {
                StopPoison();
                yield break;
            }

            PlayPoisonDamageFlash(version);
        }

        if (version == poisonVersion)
            StopPoison();
    }

    private bool ShouldStopPoison()
    {
        return playerHealth == null ||
               playerHealth.IsDead ||
               playerHealth.CurrentHealthUnits <= 0;
    }

    private void PlayPoisonDamageFlash(int version)
    {
        if (version != poisonVersion)
            return;

        if (ShouldStopPoison())
            return;

        if (poisonFlashRoutine != null)
            StopCoroutine(poisonFlashRoutine);

        poisonFlashRoutine = StartCoroutine(PoisonDamageFlashRoutine(version));
    }

    private IEnumerator PoisonDamageFlashRoutine(int version)
    {
        SetSpriteColor(poisonDamageFlashColor);

        yield return new WaitForSeconds(poisonDamageFlashDuration);

        if (version != poisonVersion)
        {
            poisonFlashRoutine = null;
            yield break;
        }

        if (poisonActive && !ShouldStopPoison())
            ApplyPoisonTint();
        else
            RestoreSpriteColors();

        poisonFlashRoutine = null;
    }

    private void ApplyPoisonTint()
    {
        if (!poisonActive)
            return;

        if (ShouldStopPoison())
        {
            StopPoison();
            return;
        }

        SetSpriteColor(poisonTintColor);
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
}