public interface IDamageable
{
    bool ReceiveDamage(DamageData damageData);
    bool IsDead { get; }
}