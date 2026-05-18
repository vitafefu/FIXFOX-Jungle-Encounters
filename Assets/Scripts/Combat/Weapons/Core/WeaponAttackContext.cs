using UnityEngine;

/// <summary>
/// Container for all data needed during a weapon attack hit.
/// </summary>
public class WeaponAttackContext
{
    public WeaponData WeaponData;
    public GameObject OwnerObject;
    public Vector2 AttackDirection;

    public WeaponAttackContext(WeaponData weaponData, GameObject ownerObject, Vector2 attackDirection)
    {
        WeaponData = weaponData;
        OwnerObject = ownerObject;
        AttackDirection = attackDirection;
    }

    public DamageData CreateDamageData()
    {
        return new DamageData(
            amountUnits: WeaponData.damageUnits,
            type: WeaponData.damageType,
            sourcePosition: OwnerObject.transform.position,
            sourceObject: OwnerObject,
            knockbackForceX: WeaponData.knockbackForceX,
            knockbackForceY: WeaponData.knockbackForceY
        );
    }
}