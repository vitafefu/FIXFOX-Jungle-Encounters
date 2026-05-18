using System.Collections;
using UnityEngine;

/// <summary>
/// Базовый класс для всего оружия.
/// Управляет выбором оружия по цифрам, видимостью, атакой по leftclick.
/// </summary>
public class WeaponBase : MonoBehaviour
{
    [Header("Weapon Data")]
    [SerializeField] protected WeaponData weaponData;

    [Header("References")]
    [SerializeField] protected SpriteRenderer weaponSpriteRenderer;
    [SerializeField] protected WeaponHitbox2D weaponHitbox;
    [SerializeField] protected Animator weaponAnimator;

    protected bool isWeaponActive = false;
    protected bool canAttack = true;
    protected float lastAttackTime = -Mathf.Infinity;
    protected GameObject ownerObject;

    protected virtual void Awake()
    {
        // Владелец — родительский объект, у которого тег Player
        ownerObject = GetComponentInParent<PlayerHealth>()?.gameObject;

        if (weaponSpriteRenderer != null)
            weaponSpriteRenderer.enabled = false;

        if (weaponHitbox != null)
            weaponHitbox.Deactivate();
    }

    protected virtual void Update()
    {
        HandleWeaponSelection();
        HandleAttackInput();
    }

    /// <summary>
    /// Выбор оружия по цифрам: 1 = когти, 2 = меч и т.д.
    /// </summary>
    protected virtual void HandleWeaponSelection()
    {
        if (weaponData == null)
            return;

        int keyNumber = weaponData.weaponSlotIndex + 1;
        if (keyNumber < 1 || keyNumber > 9)
            return;

        KeyCode key = KeyCode.Alpha0 + keyNumber;

        if (Input.GetKeyDown(key))
        {
            Equip();
        }
    }

    public virtual void Equip()
    {
        isWeaponActive = true;

        if (weaponSpriteRenderer != null)
            weaponSpriteRenderer.enabled = true;
    }

    public virtual void Unequip()
    {
        isWeaponActive = false;

        if (weaponSpriteRenderer != null)
            weaponSpriteRenderer.enabled = false;

        if (weaponHitbox != null)
            weaponHitbox.Deactivate();
    }

    protected virtual void HandleAttackInput()
    {
        if (!isWeaponActive)
            return;

        if (!canAttack)
            return;

        if (Time.time < lastAttackTime + weaponData.attackCooldown)
            return;

        if (Input.GetButtonDown("Fire1")) // Left Click
        {
            Attack();
        }
    }

    public virtual void Attack()
    {
        if (weaponData == null)
            return;

        lastAttackTime = Time.time;
        StartCoroutine(AttackRoutine());
    }

    protected virtual IEnumerator AttackRoutine()
    {
        canAttack = false;

        // Запускаем анимацию
        if (weaponAnimator != null && !string.IsNullOrEmpty(weaponData.attackTriggerName))
        {
            weaponAnimator.SetTrigger(weaponData.attackTriggerName);
        }

        // Ждём время активации хитбокса
        yield return new WaitForSeconds(weaponData.hitboxActivationTime);

        // Определяем направление атаки (куда смотрит лис)
        Vector2 attackDirection = transform.root.localScale.x > 0 ? Vector2.right : Vector2.left;

        // Активируем хитбокс
        if (weaponHitbox != null)
        {
            WeaponAttackContext context = new WeaponAttackContext(weaponData, ownerObject, attackDirection);
            weaponHitbox.Activate(context);
        }

        // Ждём длительность хитбокса
        yield return new WaitForSeconds(weaponData.hitboxActiveDuration);

        // Деактивируем хитбокс
        if (weaponHitbox != null)
        {
            weaponHitbox.Deactivate();
        }

        // Кулдаун уже прошёл, можно атаковать снова
        canAttack = true;
    }
}