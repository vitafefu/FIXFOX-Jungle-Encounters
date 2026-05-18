using System.Collections;
using UnityEngine;

public class ClawsCombat : MonoBehaviour
{
    [Header("Лис")]
    [SerializeField] private GameObject graphicsNormal;
    [SerializeField] private GameObject foxNoArms;
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Когти")]
    [SerializeField] private GameObject clawsIdleObject;
    [SerializeField] private GameObject clawsAttackObject;

    [Header("Атака")]
    [SerializeField] private Animator clawsAnimator;
    [SerializeField] private SpriteRenderer clawsAttackSprite;
    [SerializeField] private Collider2D clawsAttackCollider;
    [SerializeField] private WeaponHitbox2D weaponHitbox;
    [SerializeField] private WeaponData weaponData;

    [Header("Настройки")]
    [SerializeField] private float attackCooldown = 0.4f;
    [SerializeField] private float hitboxActivationTime = 0.1f;
    [SerializeField] private float hitboxActiveDuration = 0.15f;

    private bool clawsModeEnabled = false;
    private bool canAttack = true;
    private float lastAttackTime = -Mathf.Infinity;
    private bool isAttacking = false;

    private SpriteRenderer graphicsSpriteRenderer;
    private PlayerController controller;

    private void Awake()
    {
        if (graphicsNormal != null)
        {
            graphicsSpriteRenderer = graphicsNormal.GetComponent<SpriteRenderer>();
            graphicsSpriteRenderer.enabled = true;
        }

        controller = GetComponent<PlayerController>();

        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.OnDied += OnPlayerDied;
            playerHealth.OnRevived += OnPlayerRevived;
        }

        if (clawsAttackCollider != null)
        {
            Collider2D playerCollider = GetComponent<Collider2D>();
            if (playerCollider != null)
                Physics2D.IgnoreCollision(clawsAttackCollider, playerCollider, true);
        }

        if (foxNoArms != null) foxNoArms.SetActive(false);
        if (clawsIdleObject != null) clawsIdleObject.SetActive(false);
        if (clawsAttackObject != null) clawsAttackObject.SetActive(false);
        if (clawsAttackCollider != null) clawsAttackCollider.enabled = false;
    }

    private void Start()
    {
        if (graphicsSpriteRenderer != null)
            graphicsSpriteRenderer.enabled = true;
        if (foxNoArms != null)
            foxNoArms.SetActive(false);
        if (clawsIdleObject != null)
            clawsIdleObject.SetActive(false);
    }

    private void Update()
    {
        HandleClawsToggle();

        if (clawsModeEnabled && !isAttacking)
        {
            UpdateClawsVisibility();
        }

        HandleAttackInput();
    }

    private void HandleClawsToggle()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            clawsModeEnabled = !clawsModeEnabled;

            if (clawsModeEnabled)
            {
                if (graphicsSpriteRenderer != null) graphicsSpriteRenderer.enabled = false;
                if (foxNoArms != null) foxNoArms.SetActive(true);
                if (clawsIdleObject != null) clawsIdleObject.SetActive(true);
            }
            else
            {
                HideAllClaws();
            }
        }
    }

    private void UpdateClawsVisibility()
    {
        bool isGrounded = false;
        bool isJumping = false;
        bool isClimbing = false;
        bool isCrouching = false;

        if (controller != null)
        {
            isGrounded = controller.IsGrounded;
            isClimbing = controller.IsClimbing;
            isCrouching = controller.IsCrouching;
            isJumping = !isGrounded && !isClimbing;
        }

        bool canShowClaws = (isGrounded || controller.IsRunning) && !isJumping && !isClimbing && !isCrouching;

        if (!canShowClaws)
        {
            if (foxNoArms != null) foxNoArms.SetActive(false);
            if (clawsIdleObject != null) clawsIdleObject.SetActive(false);
            if (graphicsSpriteRenderer != null) graphicsSpriteRenderer.enabled = true;
        }
        else
        {
            if (foxNoArms != null) foxNoArms.SetActive(true);
            if (clawsIdleObject != null) clawsIdleObject.SetActive(true);
            if (graphicsSpriteRenderer != null) graphicsSpriteRenderer.enabled = false;

            bool facingRight = controller != null && controller.FacingRight;
            FlipClaws(clawsIdleObject, facingRight);
        }
    }

    private void FlipClaws(GameObject clawsObject, bool faceRight)
    {
        if (clawsObject == null) return;

        Vector3 scale = clawsObject.transform.localScale;
        scale.x = faceRight ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
        clawsObject.transform.localScale = scale;

        float offsetX = 0.5f;
        Vector3 pos = clawsObject.transform.localPosition;
        pos.x = faceRight ? Mathf.Abs(offsetX) : -Mathf.Abs(offsetX);
        clawsObject.transform.localPosition = pos;
    }

    private void HandleAttackInput()
    {
        if (!clawsModeEnabled) return;
        if (!canAttack) return;
        if (isAttacking) return;
        if (Time.time < lastAttackTime + attackCooldown) return;

        if (Input.GetButtonDown("Fire1"))
        {
            StartCoroutine(AttackRoutine());
        }
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        canAttack = false;
        lastAttackTime = Time.time;

        if (clawsIdleObject != null) clawsIdleObject.SetActive(false);

        if (clawsAttackObject != null)
        {
            clawsAttackObject.SetActive(true);
            bool facingRight = controller != null && controller.FacingRight;
            FlipClaws(clawsAttackObject, facingRight);
        }
        if (clawsAttackSprite != null) clawsAttackSprite.enabled = true;
        if (clawsAnimator != null)
        {
            clawsAnimator.enabled = true;
            clawsAnimator.SetTrigger("Attack");
        }

        yield return new WaitForSeconds(hitboxActivationTime);

        if (clawsAttackCollider != null) clawsAttackCollider.enabled = true;
        if (weaponHitbox != null && weaponData != null)
        {
            Vector2 dir = transform.localScale.x >= 0 ? Vector2.right : Vector2.left;
            WeaponAttackContext context = new WeaponAttackContext(weaponData, gameObject, dir);
            weaponHitbox.Activate(context);
        }

        yield return new WaitForSeconds(hitboxActiveDuration);

        if (clawsAttackCollider != null) clawsAttackCollider.enabled = false;
        if (weaponHitbox != null) weaponHitbox.Deactivate();

        yield return new WaitForSeconds(0.1f);

        if (clawsAttackSprite != null) clawsAttackSprite.enabled = false;
        if (clawsAnimator != null) clawsAnimator.enabled = false;
        if (clawsAttackObject != null) clawsAttackObject.SetActive(false);

        isAttacking = false;
        canAttack = true;
    }

    private void HideAllClaws()
    {
        clawsModeEnabled = false;
        if (graphicsSpriteRenderer != null) graphicsSpriteRenderer.enabled = true;
        if (foxNoArms != null) foxNoArms.SetActive(false);
        if (clawsIdleObject != null) clawsIdleObject.SetActive(false);
        if (clawsAttackObject != null) clawsAttackObject.SetActive(false);
        if (clawsAttackCollider != null) clawsAttackCollider.enabled = false;
    }

    private void OnPlayerDied()
    {
        clawsModeEnabled = false;
        if (graphicsSpriteRenderer != null)
            graphicsSpriteRenderer.enabled = true;
        if (foxNoArms != null) foxNoArms.SetActive(false);
        if (clawsIdleObject != null) clawsIdleObject.SetActive(false);
        if (clawsAttackObject != null) clawsAttackObject.SetActive(false);
        if (clawsAttackCollider != null) clawsAttackCollider.enabled = false;
    }

    private void OnPlayerRevived()
    {
        // Включаем Graphics
        if (graphicsNormal != null)
        {
            graphicsNormal.SetActive(true);

            SpriteRenderer sr = graphicsNormal.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.enabled = true;

            // Включаем Animator
            Animator anim = graphicsNormal.GetComponent<Animator>();
            if (anim != null)
            {
                anim.enabled = true;
                anim.SetBool("isDead", false);
            }
        }

        if (graphicsSpriteRenderer != null)
            graphicsSpriteRenderer.enabled = true;

        if (foxNoArms != null) foxNoArms.SetActive(false);
        if (clawsIdleObject != null) clawsIdleObject.SetActive(false);
        if (clawsAttackObject != null) clawsAttackObject.SetActive(false);
        if (clawsAttackCollider != null) clawsAttackCollider.enabled = false;

        clawsModeEnabled = false;
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnDied -= OnPlayerDied;
            playerHealth.OnRevived -= OnPlayerRevived;
        }
    }
}