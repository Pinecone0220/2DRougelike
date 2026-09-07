using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace TealFalconEnemySeries{

    public class BeamShot : MonoBehaviour
    {

        public Rigidbody2D _rigidBody;
        public float launchSpeed = 12f;
        public int damage = 10;
        public Vector2 direction;
    public float duration;
    public Transform ownerRoot;
    bool hasHit;
    Collider2D cachedCollider;
    float spawnedAt;

    // Start is called before the first frame update
    void Start()
    {
        spawnedAt = Time.time;
        cachedCollider = GetComponent<Collider2D>();
        if (_rigidBody == null)
        {
            _rigidBody = GetComponent<Rigidbody2D>();
        }

        if (cachedCollider != null)
        {
            cachedCollider.enabled = false;
            cachedCollider.isTrigger = true;
        }

        if (_rigidBody != null)
        {
            _rigidBody.gravityScale = 0f;
            _rigidBody.linearDamping = 0f;
            _rigidBody.angularDamping = 0f;
            _rigidBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rigidBody.interpolation = RigidbodyInterpolation2D.Interpolate;
            _rigidBody.constraints = RigidbodyConstraints2D.FreezeRotation;
            Vector2 travelDirection = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
            transform.right = travelDirection;
            _rigidBody.linearVelocity = travelDirection * launchSpeed;
        }

        ParticleSystem particleSystem = GetComponentInChildren<ParticleSystem>(true);
        if (particleSystem != null)
        {
            particleSystem.Play(true);
        }

        SetRenderOrder(1000);

        StartCoroutine(EnableColliderSoon());
        StartCoroutine(Timer());
    }

        // Update is called once per frame
        void Update()
        {
            
        }

        IEnumerator Timer()
            {
                yield return new WaitForSeconds(duration);
                Destroy(gameObject);
            }

        IEnumerator EnableColliderSoon()
        {
            yield return new WaitForSeconds(0.18f);

            if (cachedCollider != null)
            {
                cachedCollider.enabled = true;
            }
        }

        void SetRenderOrder(int sortingOrder)
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            if (renderers == null)
            {
                return;
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].sortingOrder = sortingOrder;
                }
            }
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            TryResolveImpact(collision.collider);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            TryResolveImpact(other);
        }

        void TryResolveImpact(Collider2D other)
        {
            if (hasHit || other == null)
            {
                return;
            }

            if (Time.time - spawnedAt < 0.2f)
            {
                return;
            }

            if (ownerRoot != null && other.transform.root == ownerRoot)
            {
                return;
            }

            if (other.GetComponentInParent<DarkKnightController>() != null)
            {
                return;
            }

            if (other.isTrigger && other.GetComponentInParent<player>() == null)
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
                Destroy(gameObject);
                return;
            }

            hasHit = true;
            playerTarget.TakeDamage(damage);
            Destroy(gameObject);
        }

    }
}
