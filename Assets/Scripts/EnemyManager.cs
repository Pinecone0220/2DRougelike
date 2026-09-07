using UnityEngine;
using UnityEngine.UI;

public class EnemyManager : MonoBehaviour
{
    public int maxHp = 100;
    public int curhp = 0;
    public int dmg = 4;

    public Image hp_bar;
    public GameObject hp;
    public GameObject[] item;

    public static int kill_count;

    public float moveSpeed = 2f;
    public float moveDistance = 3f;

    Vector3 startPos;
    bool movingRight = true;

    bool isDie;

    SpriteRenderer sprite;
    Animator anim;
    AudioSource As;

    public AudioClip hit;
    public AudioClip die;
    public AudioClip attack;

    public GameObject bulletPrefab;
    public Transform firePoint;
    public float attackInterval = 2f;

    float timer;
    Transform player_pos;


    void Start()
    {
        curhp = maxHp;

        startPos = transform.position;

        sprite = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        As = GetComponent<AudioSource>();

        isDie = false;
        hp.SetActive(true);

        player_pos = GameObject.FindWithTag("Player").transform;
    }

    void Update()
    {
        if (!isDie)
        {
            if (movingRight)
            {
                transform.Translate(Vector2.right * moveSpeed * Time.deltaTime);

                if (transform.position.x >= startPos.x + moveDistance)
                {
                    movingRight = false;
                    sprite.flipX = true;
                }
            }
            else
            {
                transform.Translate(Vector2.left * moveSpeed * Time.deltaTime);

                if (transform.position.x <= startPos.x - moveDistance)
                {
                    movingRight = true;
                    sprite.flipX = false;
                }
            }
        }

        hp_bar.fillAmount = (float)curhp / maxHp;

        if (curhp <= 0 && !isDie)
        {
            EnemyDie();
        }

        timer += Time.deltaTime;

        if (timer >= attackInterval)
        {
            timer = 0f;
            Shoot();
        }
    }

    public void EnemyDie()
    {
        isDie = true;
        sprite.color -= new Color(0, 0, 0, 0.00001f);
        anim.SetBool("Die", true);
        hp.SetActive(false);
        kill_count += 1;
        As.PlayOneShot(die);

        Destroy(gameObject, 2f);
    }
    public void TakeDamage(int damage)
    {
        curhp -= damage;
        As.PlayOneShot(hit);
    }

    void Shoot()
    {

        if (player.isDie == false && isDie == false)
        {
            As.PlayOneShot(attack);

            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);

            Vector2 dir = player_pos.position - firePoint.position;

            bullet.GetComponent<EnemyBullet>().Init(dir);
        }
    }
}
