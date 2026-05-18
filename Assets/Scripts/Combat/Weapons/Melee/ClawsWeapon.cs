using UnityEngine;

public class ClawsWeapon : WeaponBase
{
    [Header("Claws Specific")]
    [SerializeField] private int maxComboCount = 3;

    private int currentCombo = 0;
    private readonly float comboResetTime = 0.8f;

    protected override void Update()
    {
        base.Update();

        if (isWeaponActive)
        {
            if (Time.time > lastAttackTime + comboResetTime && currentCombo > 0)
            {
                ResetCombo();
            }
        }
    }

    public override void Attack()
    {
        if (weaponData == null)
            return;

        lastAttackTime = Time.time;
        currentCombo++;

        if (currentCombo > maxComboCount)
            currentCombo = 1;

        StartCoroutine(AttackRoutine());
    }

    public override void Unequip()
    {
        base.Unequip();
        ResetCombo();
    }

    private void ResetCombo()
    {
        currentCombo = 0;
    }

    public int GetCurrentCombo()
    {
        return currentCombo;
    }
}