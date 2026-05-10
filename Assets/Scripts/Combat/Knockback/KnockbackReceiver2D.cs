using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class KnockbackReceiver2D : MonoBehaviour
{
    [Header("Knockback Settings")]
    [SerializeField] private bool enableKnockback = true;
    [SerializeField] private bool resetVelocityBeforeKnockback = true;
    [SerializeField] private float knockbackControlLockTime = 0.18f;

    [Header("Burst Knockback")]
    [SerializeField] private bool useBurstVelocity = true;
    [SerializeField] private bool useDamageDataValuesAsVelocity = true;

    [Header("Optional: disable these scripts during knockback")]
    [SerializeField] private MonoBehaviour[] behavioursToDisableDuringKnockback;

    private Rigidbody2D rb;
    private Coroutine knockbackRoutine;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void ApplyKnockback(DamageData damageData)
    {
        ApplyKnockback(
            damageData.SourcePosition,
            damageData.KnockbackForceX,
            damageData.KnockbackForceY
        );
    }

    public void ApplyKnockback(Vector2 sourcePosition, float forceX, float forceY)
    {
        if (!enableKnockback)
            return;

        if (rb == null)
            return;

        if (forceX <= 0f && forceY <= 0f)
            return;

        if (knockbackRoutine != null)
            StopCoroutine(knockbackRoutine);

        knockbackRoutine = StartCoroutine(KnockbackRoutine(sourcePosition, forceX, forceY));
    }

    public void StopKnockback()
    {
        if (knockbackRoutine != null)
        {
            StopCoroutine(knockbackRoutine);
            knockbackRoutine = null;
        }

        SetBehavioursEnabled(true);

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    private IEnumerator KnockbackRoutine(Vector2 sourcePosition, float forceX, float forceY)
    {
        SetBehavioursEnabled(false);

        float directionX = transform.position.x >= sourcePosition.x ? 1f : -1f;

        if (Mathf.Abs(transform.position.x - sourcePosition.x) < 0.05f)
        {
            if (Mathf.Abs(rb.velocity.x) > 0.05f)
                directionX = -Mathf.Sign(rb.velocity.x);
            else
                directionX = 1f;
        }

        if (resetVelocityBeforeKnockback)
            rb.velocity = Vector2.zero;

        if (!useBurstVelocity)
        {
            rb.AddForce(
                new Vector2(directionX * forceX, forceY),
                ForceMode2D.Impulse
            );

            yield return new WaitForSeconds(knockbackControlLockTime);

            SetBehavioursEnabled(true);
            knockbackRoutine = null;
            yield break;
        }

        float timer = 0f;

        while (timer < knockbackControlLockTime)
        {
            if (rb == null)
                yield break;

            if (useDamageDataValuesAsVelocity)
            {
                rb.velocity = new Vector2(
                    directionX * forceX,
                    forceY
                );
            }
            else
            {
                rb.AddForce(
                    new Vector2(directionX * forceX, forceY),
                    ForceMode2D.Impulse
                );
            }

            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        SetBehavioursEnabled(true);
        knockbackRoutine = null;
    }

    private void SetBehavioursEnabled(bool enabled)
    {
        if (behavioursToDisableDuringKnockback == null)
            return;

        for (int i = 0; i < behavioursToDisableDuringKnockback.Length; i++)
        {
            if (behavioursToDisableDuringKnockback[i] != null)
                behavioursToDisableDuringKnockback[i].enabled = enabled;
        }
    }

    private void OnDisable()
    {
        SetBehavioursEnabled(true);
    }
}