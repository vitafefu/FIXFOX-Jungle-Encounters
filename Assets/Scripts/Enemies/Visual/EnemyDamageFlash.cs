using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Enemy))]
public class EnemyDamageFlash : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color damageColor = Color.red;
    [SerializeField] private float flashDuration = 0.15f;

    private Enemy enemy;
    private Color originalColor;
    private Coroutine flashRoutine;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();

        if (spriteRenderer == null)
        {
            Transform graphics = transform.Find("Graphics");
            if (graphics != null)
                spriteRenderer = graphics.GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    private void OnEnable()
    {
        enemy.OnDamaged += OnEnemyDamaged;
        enemy.OnDied += OnEnemyDied;
    }

    private void OnDisable()
    {
        enemy.OnDamaged -= OnEnemyDamaged;
        enemy.OnDied -= OnEnemyDied;
    }

    private void OnEnemyDamaged(DamageData damageData)
    {
        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private void OnEnemyDied()
    {
        if (spriteRenderer != null)
            spriteRenderer.color = damageColor;
    }

    private IEnumerator FlashRoutine()
    {
        if (spriteRenderer != null)
            spriteRenderer.color = damageColor;

        yield return new WaitForSeconds(flashDuration);

        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;

        flashRoutine = null;
    }
}