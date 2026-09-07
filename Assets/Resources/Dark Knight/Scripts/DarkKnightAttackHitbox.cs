using UnityEngine;

namespace TealFalconEnemySeries
{
    public class DarkKnightAttackHitbox : MonoBehaviour
    {
        DarkKnightController owner;
        Collider2D cachedCollider;
        int damage = 10;
        bool hasHitThisSwing;
        readonly System.Collections.Generic.List<Collider2D> overlapResults = new System.Collections.Generic.List<Collider2D>();

        void Awake()
        {
            cachedCollider = GetComponent<Collider2D>();
            if (cachedCollider != null)
            {
                cachedCollider.isTrigger = true;
                cachedCollider.enabled = false;
            }
        }

        public void Configure(DarkKnightController controller, int attackDamage)
        {
            owner = controller;
            damage = attackDamage;

            if (cachedCollider == null)
            {
                cachedCollider = GetComponent<Collider2D>();
            }

            if (cachedCollider != null)
            {
                cachedCollider.isTrigger = true;
                cachedCollider.enabled = false;
            }
        }

        public void SetActiveHitbox(bool active)
        {
            hasHitThisSwing = false;

            if (cachedCollider != null)
            {
                cachedCollider.enabled = active;
            }

            if (active)
            {
                TryHitOverlappingPlayer();
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            TryHitPlayer(other);
        }

        void OnTriggerStay2D(Collider2D other)
        {
            TryHitPlayer(other);
        }

        void TryHitPlayer(Collider2D other)
        {
            if (owner == null || cachedCollider == null || !cachedCollider.enabled || hasHitThisSwing)
            {
                return;
            }

            player playerTarget = other.GetComponentInParent<player>();
            if (playerTarget == null)
            {
                playerTarget = other.GetComponent<player>();
            }

            if (playerTarget == null || player.isDie)
            {
                return;
            }

            playerTarget.TakeDamage(damage);
            hasHitThisSwing = true;
        }

        void TryHitOverlappingPlayer()
        {
            if (owner == null || cachedCollider == null || !cachedCollider.enabled || hasHitThisSwing)
            {
                return;
            }

            overlapResults.Clear();
            ContactFilter2D filter = new ContactFilter2D();
            filter.NoFilter();
            int count = cachedCollider.Overlap(filter, overlapResults);
            for (int i = 0; i < count; i++)
            {
                if (hasHitThisSwing)
                {
                    return;
                }

                TryHitPlayer(overlapResults[i]);
            }
        }
    }
}
