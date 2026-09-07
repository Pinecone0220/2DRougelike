using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    public float speed = 5f;
    public int damage = 10;
    static bool bulletsStopped = false;

    Vector2 direction;

    public void Init(Vector2 dir)
    {
        bulletsStopped = false;
        direction = dir.normalized;
    }

    void Update()
    {
        if (bulletsStopped)
        {
            return;
        }

        transform.Translate(direction * speed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            //플레이어 데미지
            collision.GetComponent<player>().TakeDamage(damage);

            Destroy(gameObject);
        }

        if (collision.CompareTag("Ground"))
        {
            Destroy(gameObject);
        }
    }

    public static void StopAllProjectiles()
    {
        bulletsStopped = true;

        EnemyBullet[] activeBullets = Object.FindObjectsOfType<EnemyBullet>();
        for (int i = 0; i < activeBullets.Length; i++)
        {
            if (activeBullets[i] != null)
            {
                Destroy(activeBullets[i].gameObject);
            }
        }
    }
}
