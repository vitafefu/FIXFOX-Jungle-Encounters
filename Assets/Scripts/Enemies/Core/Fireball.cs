using UnityEngine;

public class Fireball : MonoBehaviour
{
    public float speed = 8f;
    public int damageUnits = 2;
    public float knockbackForceX = 5f;
    public float knockbackForceY = 2f;

    private Rigidbody2D rb;
    private Vector2 direction;
    private bool hasDirection;

    public void SetDirection(Vector2 dir)
    {
        direction = dir.normalized;
        hasDirection = true;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody2D>();

        rb.gravityScale = 0f;

        if (!hasDirection)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                direction = (player.transform.position - transform.position).normalized;
            else
                direction = transform.right;
        }

        rb.velocity = direction * speed;
        Destroy(gameObject, 3f);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.GetComponent<Boss>() != null)
            return;

        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerDamageReceiver receiver = collision.gameObject.GetComponent<PlayerDamageReceiver>();
            if (receiver != null)
            {
                DamageData damageData = new DamageData(
                    amountUnits: damageUnits,
                    type: DamageType.Fire,
                    sourcePosition: transform.position,
                    sourceObject: gameObject,
                    knockbackForceX: knockbackForceX,
                    knockbackForceY: knockbackForceY
                );

                receiver.ReceiveDamage(damageData);
            }
        }

        Destroy(gameObject);
    }
}