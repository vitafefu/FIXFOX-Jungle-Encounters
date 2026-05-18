using UnityEngine;

[CreateAssetMenu(fileName = "NewWeaponData", menuName = "Weapons/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Identity")]
    public WeaponType weaponType = WeaponType.Claws;
    public string weaponName = "Claws";
    [Tooltip("Кнопка выбора оружия: 0 = цифра 1, 1 = цифра 2 и т.д.")]
    public int weaponSlotIndex = 0;

    [Header("Damage")]
    [Tooltip("Урон в health units. 2 = полсердца, 4 = целое сердце.")]
    public int damageUnits = 2;
    public DamageType damageType = DamageType.Normal;

    [Header("Knockback")]
    public float knockbackForceX = 5f;
    public float knockbackForceY = 2f;

    [Header("Attack")]
    [Tooltip("Кулдаун между атаками в секундах.")]
    public float attackCooldown = 0.4f;
    [Tooltip("Через сколько секунд после начала атаки включится хитбокс.")]
    public float hitboxActivationTime = 0.1f;
    [Tooltip("На сколько секунд включается хитбокс.")]
    public float hitboxActiveDuration = 0.15f;

    [Header("Animation")]
    [Tooltip("Имя триггера в Animator для атаки.")]
    public string attackTriggerName = "Attack";
}