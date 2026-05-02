using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class KnockbackReceiver2D : MonoBehaviour
{
    [Header("Knockback Settings")]
    [SerializeField] private bool enableKnockback = true;
    [SerializeField] private bool resetVelocityBeforeKnockback = true;
    [SerializeField] private float knockbackControlLockTime = 0.18f;

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

        if (knockbackRoutine != null)
            StopCoroutine(knockbackRoutine);

        knockbackRoutine = StartCoroutine(KnockbackRoutine(sourcePosition, forceX, forceY));
    }

    private IEnumerator KnockbackRoutine(Vector2 sourcePosition, float forceX, float forceY)
    {
        SetBehavioursEnabled(false);

        float directionX = transform.position.x >= sourcePosition.x ? 1f : -1f;

        if (resetVelocityBeforeKnockback)
            rb.velocity = Vector2.zero;

        rb.AddForce(
            new Vector2(directionX * forceX, forceY),
            ForceMode2D.Impulse
        );

        yield return new WaitForSeconds(knockbackControlLockTime);

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
}