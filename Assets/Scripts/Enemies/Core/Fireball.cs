using UnityEngine;

public class Fireball : MonoBehaviour
{
    public float speed = 8f;
    private Rigidbody2D rb;
    private Vector2 direction;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }

        rb.gravityScale = 0f;

        // Находим игрока и летим на него
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            direction = (player.transform.position - transform.position).normalized;
            rb.velocity = direction * speed;
        }
        else
        {
            rb.velocity = transform.right * speed;
        }

        Destroy(gameObject, 3f);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Игнорируем босса
        if (collision.gameObject.GetComponent<Boss>() != null) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("УРОН ИГРОКУ!");
        }

        Destroy(gameObject);
    }
}