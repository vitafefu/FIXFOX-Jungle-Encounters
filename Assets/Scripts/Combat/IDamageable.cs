/// <summary>
/// Interface for anything that can receive damage.
/// </summary>
public interface IDamageable
{
    bool ReceiveDamage(DamageData damageData);
}