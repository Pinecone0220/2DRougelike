using System.Collections.Generic;
using UnityEngine;

public class AttackManager : MonoBehaviour
{
    public bool isAttacking = false;
    public bool canCombo;
    public bool comboInput;

    public Transform attackPoint;
    public float attackRadius = 0.6f;
    public LayerMask enemyLayer;
    public int maxComboCount = 4;
    public float comboRestartDelay = 0.15f;

    player pl;
    Animator anim;
    AudioSource As;

    public AudioClip atk;

    Rigidbody2D rb;
    int currentComboCount;
    float nextAttackTime;
    bool isAirAttack;

    void Start()
    {
        isAttacking = false;
        anim = GetComponent<Animator>();
        pl = GetComponent<player>();
        As = GetComponent<AudioSource>();
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z))
        {
            if (isAttacking)
            {
                if (canCombo)
                {
                    if (currentComboCount >= maxComboCount)
                    {
                        return;
                    }

                    comboInput = true;
                    currentComboCount += 1;
                    anim.SetTrigger("Attack");
                    As.PlayOneShot(atk);
                }

                return;
            }

            StartAttack();
        }
    }

    void StartAttack()
    {
        if (Time.time < nextAttackTime)
        {
            return;
        }

        isAttacking = true;
        canCombo = false;
        comboInput = false;
        isAirAttack = !pl.isGround;
        currentComboCount = isAirAttack ? 0 : 1;

        if (rb != null)
        {
            if (isAirAttack)
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }
            else
            {
                rb.linearVelocity = Vector2.zero;
            }
        }

        anim.SetTrigger("Attack");
        As.PlayOneShot(atk);
    }

    public void Hit()
    {
        if (attackPoint == null || pl == null)
        {
            return;
        }

        Collider2D[] hits =
            Physics2D.OverlapCircleAll(
                attackPoint.position,
                attackRadius);

        HashSet<EnemyManager> hitEnemies = new HashSet<EnemyManager>();
        HashSet<BossManager> hitBosses = new HashSet<BossManager>();

        foreach (Collider2D hit in hits)
        {
            EnemyManager enemy = hit.GetComponentInParent<EnemyManager>();
            BossManager boss = hit.GetComponentInParent<BossManager>();

            if (enemy != null)
            {
                hitEnemies.Add(enemy);
            }
            if (boss != null)
            {
                hitBosses.Add(boss);
            }
        }

        foreach (EnemyManager enemy in hitEnemies)
        {
            if (enemy != null)
            {
                enemy.TakeDamage(pl.dmg);
            }
        }

        foreach (BossManager boss in hitBosses)
        {
            if (boss != null)
            {
                boss.TakeDamage(pl.dmg);
            }
        }
    }
    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
    }

    public void EndAttack()
    {
        if (!canCombo)
        {
            FinishAttack();
        }
    }

    public void ComboStart()
    {
        canCombo = true;
        comboInput = false;
    }

    public void ComboEnd()
    {
        canCombo = false;

        if (comboInput)
        {
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
        }
        else
        {
            FinishAttack();
        }
    }

    void FinishAttack()
    {
        if (!isAirAttack && currentComboCount >= maxComboCount)
        {
            nextAttackTime = Time.time + comboRestartDelay;
        }

        isAttacking = false;
        canCombo = false;
        comboInput = false;
        currentComboCount = 0;
        isAirAttack = false;
    }

    /*public void Attacking()
    {
        isAttacking = true;
    }*/
}
