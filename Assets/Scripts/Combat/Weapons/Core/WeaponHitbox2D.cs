using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class WeaponHitbox2D : MonoBehaviour
{
    private Collider2D hitboxCollider;
    private WeaponAttackContext currentContext;
    private HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();

    private void Awake()
    {
        hitboxCollider = GetComponent<Collider2D>();
        hitboxCollider.isTrigger = true;
        hitboxCollider.enabled = false;
    }

    public void Activate(WeaponAttackContext context)
    {
        currentContext = context;
        hitTargets.Clear();
        hitboxCollider.enabled = true;
    }

    public void Deactivate()
    {
        hitboxCollider.enabled = false;
        currentContext = null;
        hitTargets.Clear();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (currentContext == null)
            return;

        if (other.CompareTag("Player"))
            return;

        if (other.transform.root.CompareTag("Player"))
            return;

        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable == null)
            damageable = other.GetComponentInParent<IDamageable>();

        if (damageable == null)
            return;

        if (hitTargets.Contains(damageable))
            return;

        hitTargets.Add(damageable);

        DamageData damageData = currentContext.CreateDamageData();
        damageable.ReceiveDamage(damageData);
    }
}