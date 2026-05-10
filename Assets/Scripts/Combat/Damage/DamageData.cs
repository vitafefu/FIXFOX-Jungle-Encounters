using UnityEngine;

public struct DamageData
{
    public int AmountUnits;
    public DamageType Type;
    public Vector2 SourcePosition;
    public GameObject SourceObject;
    public float KnockbackForceX;
    public float KnockbackForceY;

    public DamageData(
        int amountUnits,
        DamageType type,
        Vector2 sourcePosition,
        GameObject sourceObject,
        float knockbackForceX,
        float knockbackForceY)
    {
        AmountUnits = amountUnits;
        Type = type;
        SourcePosition = sourcePosition;
        SourceObject = sourceObject;
        KnockbackForceX = knockbackForceX;
        KnockbackForceY = knockbackForceY;
    }
}