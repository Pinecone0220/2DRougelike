using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TealFalconEnemySeries{

    public class DarkKnightController : MonoBehaviour
    {


        //Movement Settings
        public float currentSpeed = 0f;
        public float animationSpeed = 2f;
        public float acceleration = 4.8f;
        public float MaxWalkSpeed = 2f;
        public float MaxRunSpeed = 7f;
        public float BackStepPower = 200f;
        public int AttackDamage = 10;
        public float BeamProjectileSpeed = 6f;
        public float BeamProjectileDuration = 4f;
        public float BeamChargeTime = 2.4f;
        public Vector2 MeleeHitboxSize = new Vector2(1.35f, 3.1f);
        public Vector2 MeleeHitboxOffset = new Vector2(1.7f, 0.0f);
        public bool movingRight = true;
        const string BeamPrefabResourcePath = "Dark Knight/Prefabs/DarkBall";

        //Components
        private Animator _animator = null;
        private Rigidbody2D _rigidBody = null;  
        public Material _matRef = null;
        private Material instanceMaterial = null;
        private Vector3 deathPlace;
        private MaterialPropertyBlock mpb;

        //Config
        public bool block = false;

        public enum MovementState {
            Idle,
            Walking,
            Running,
        }

        public enum FightingState {
            OnGuard,
            Attacking,
            Hurt,
            Death,
            Move,
            Idle
        }


        public MovementState CurrentMovementState = MovementState.Idle;
        public FightingState CurrentFightingState = FightingState.Idle;

        public UnityEvent OnHurt,OnDeath, OnCharged;
        
        //Color Settings
        public Color GlowColor;
        

        //Death Settings
        public Color DissolveColor;
        public GameObject ExplosionEffect;
        public Transform ExplosionRef;
        public float DissolveSpeed = 1f;
        private float DissolveStatus = 1f;
        public bool destroy = false;

        public float power = 10.0f;        // Explosion Power

        private Transform BeamAttackRef = null;
        private Transform SwordRef = null;
        public GameObject DarkBall;
        public Transform BeamTarget;
        private int beamAttackToken = 0;
        private BoxCollider2D SwordHitbox = null;
        private DarkKnightAttackHitbox SwordHitboxLogic = null;


        public List<SpriteRenderer> SRList = null;
        public List<Rigidbody2D>    RBList = null;

        public AudioSource _Channel = null;
        public AudioClip BeamSound, DeathExplosionSound, FootStepSound, PainSound, PowerLoadSound, SwordSound;

        //FootStep 

        private float stepTimer = 0f;
        public float baseStepSpeed = 3f;
        public float minStepSpeed = 0.1f;



        void Awake()
        {
           SetComponents();

        }


        void OnEnable()
        {
           SetComponents();
        }

        void SetComponents(){

            if(_animator == null)
                _animator = transform.Find("Root").GetComponent<Animator>();

            if(_rigidBody == null)
                _rigidBody = GetComponent<Rigidbody2D>();

            if(_matRef == null)
                Debug.LogWarning("NO Material Ref Setted!!!");

            if(SRList == null || SRList.Count == 0)
                SRList = GetAllSpriteRenderersInChildren(transform);

            if(RBList == null || RBList.Count == 0)
                RBList = GetAllRigidbodiesInChildren(transform);
            
            if(BeamAttackRef == null)
                BeamAttackRef = transform.Find("Root/Head_Pivot/BeamAttackRef");

            if(SwordRef == null)
                SwordRef = transform.Find("Root/Sword");

            if(SwordRef != null)
            {
                if(SwordHitbox == null)
                    SwordHitbox = SwordRef.GetComponent<BoxCollider2D>();

                if(SwordHitbox == null)
                    SwordHitbox = SwordRef.gameObject.AddComponent<BoxCollider2D>();

                SwordHitbox.isTrigger = true;
                SwordHitbox.enabled = false;

                if(SwordHitboxLogic == null)
                    SwordHitboxLogic = SwordRef.GetComponent<DarkKnightAttackHitbox>();

                if(SwordHitboxLogic == null)
                    SwordHitboxLogic = SwordRef.gameObject.AddComponent<DarkKnightAttackHitbox>();

                SwordHitboxLogic.Configure(this, AttackDamage);
                ApplySwordHitboxSize();
            }


            if(IsBuiltIn(_matRef))
                return;

            //SET MATERIAL OF PRESET
            mpb = new MaterialPropertyBlock();

            if(_matRef.GetTexture("_MainTex") != null)
                mpb.SetTexture("_MainTex", _matRef.GetTexture("_MainTex"));

            mpb.SetTexture("_MagentaPNG",_matRef.GetTexture("_MagentaPNG"));
            mpb.SetTexture("_NormalMap",_matRef.GetTexture("_NormalMap"));
            mpb.SetTexture("_Emission",_matRef.GetTexture("_Emission"));
            mpb.SetFloat("_DissolveScale",_matRef.GetFloat("_DissolveScale"));
            mpb.SetColor("_Glow",GlowColor);
            mpb.SetColor("_DissolveColor",DissolveColor);

           
            ApplyChanges();

        }

        private void ApplyChanges(){
               if(IsBuiltIn(_matRef))
                return;

                foreach (SpriteRenderer sr in SRList)
                {
                    sr.SetPropertyBlock(mpb);
                }
        }

        void Update(){

            if(CurrentFightingState == FightingState.Death){
                DissolveStatus = Mathf.MoveTowards(DissolveStatus, 0f, DissolveSpeed * Time.deltaTime);

                if(!IsBuiltIn(_matRef))
                    mpb.SetFloat("_Dissolve",DissolveStatus);

                ApplyChanges();

                if(DissolveStatus == 0 && destroy)
                    Destroy(gameObject);
            }

            if(CurrentFightingState != FightingState.Idle)
                return;

                float targetSpeed = 0f;

            //Determine Target Speed
            switch(CurrentMovementState) {
                case MovementState.Idle:
                    targetSpeed = 0f;
                    break;
                case MovementState.Walking:
                    targetSpeed = MaxWalkSpeed;
                    break;
                case MovementState.Running:
                    targetSpeed = MaxRunSpeed;
                    break;
            }

        
            // Adjust the current speed based on acceleration
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed * transform.localScale.x, acceleration * Time.deltaTime);

            _animator.SetFloat("Speed",Mathf.Abs(currentSpeed)/animationSpeed);
            
            // Apply the calculated speed to the Rigidbody2D
            _rigidBody.linearVelocity = new Vector2(currentSpeed  , 0);

            //Footstep Sound

            float stepSpeed = Mathf.Lerp(baseStepSpeed, minStepSpeed, Mathf.Abs(currentSpeed) / MaxRunSpeed);
            
            stepTimer += Time.deltaTime;


            if(stepTimer >= stepSpeed && Mathf.Abs(currentSpeed) > 0.1f){
                
    
                PlaySound(FootStepSound);
                
                stepTimer = 0f;
            }



        }

        //Change MovementState to Run
        public void ActivateRun(){

            if(CurrentFightingState != FightingState.Idle)
                return;

            CurrentMovementState = MovementState.Running;

            if (_animator != null)
            {
                _animator.SetBool("Guard", false);
                _animator.SetBool("Busy", false);
            }

        }

        //Change MovementState to Idle
        public void ActivateIdle(){

            CurrentMovementState = MovementState.Idle;
            CurrentFightingState = FightingState.Idle;

            if (_animator != null)
            {
                _animator.SetBool("Guard", false);
                _animator.SetBool("Busy", false);
            }
            
        }

        //Change MovementState to Walk
        public void ActivateWalk(){

            if(CurrentFightingState != FightingState.Idle)
                return;

            CurrentMovementState = MovementState.Walking;

            if (_animator != null)
            {
                _animator.SetBool("Guard", false);
                _animator.SetBool("Busy", false);
            }

        }

        //Change MovementState to Idle-OnGuard
        public void ActivateGuard(){

            if(CurrentFightingState == FightingState.OnGuard){
                CurrentMovementState = MovementState.Idle;
                CurrentFightingState = FightingState.Idle;
                if (_animator != null)
                {
                    _animator.SetBool("Guard",false);
                    _animator.SetBool("Busy", false);
                }

            }else{
                CurrentMovementState = MovementState.Idle;
                CurrentFightingState = FightingState.OnGuard;
                if (_animator != null)
                {
                    _animator.SetBool("Guard",true);
                    _animator.SetBool("Busy", false);
                }
            }
            
        }


        
        //On Guard, Do a Back Step
        public void ActivateBackStep(){       

            if(CurrentFightingState != FightingState.OnGuard)
                return;

            _rigidBody.AddForce(transform.localScale.x*Vector2.left * BackStepPower, ForceMode2D.Impulse);
            _animator.SetTrigger("BackStep");

        }

        //Exec Attack
        public void ActivateAttack(){

            if(CurrentFightingState == FightingState.Death
                || CurrentFightingState == FightingState.Hurt
                || CurrentFightingState == FightingState.Attacking)
                return;

            CurrentMovementState = MovementState.Idle;
            CurrentFightingState = FightingState.Attacking;
            currentSpeed = 0f;
            _animator.SetBool("Guard", true);
            _animator.SetBool("Busy", false);

            StartCoroutine(AttackRoutine());

        }

        //Exec Beam Attack
        public void ActivateBeamAttack(){

            if(CurrentFightingState == FightingState.Death
                || CurrentFightingState == FightingState.Hurt
                || CurrentFightingState == FightingState.Attacking)
            {
                return;
            }


            CurrentMovementState = MovementState.Idle;
            CurrentFightingState = FightingState.Attacking;
            currentSpeed = 0f;
            if (_rigidBody != null)
            {
                _rigidBody.linearVelocity = Vector2.zero;
            }
            _animator.SetBool("Guard", false);
            _animator.SetBool("Busy",true);
            _animator.Play("BeamAttack", 0, 0f);
            _animator.Update(0f);

            beamAttackToken++;
            StartCoroutine(BeamShootRoutine(beamAttackToken));

        }

        //Exec Hurt
        public void ActivateHurt(){
            CurrentMovementState = MovementState.Idle;
            CurrentFightingState = FightingState.Hurt;
            StartCoroutine(OnHurtRoutine());
            OnHurt.Invoke();

            PlaySound(PainSound);


        }

        //Exec Death and calls Event Destroy.
        public void ActivateDeath(){
            CurrentFightingState = FightingState.Death;
            CurrentMovementState = MovementState.Idle;
            currentSpeed = 0f;
            if (_rigidBody != null)
            {
                _rigidBody.linearVelocity = Vector2.zero;
            }
            if (_animator != null)
            {
                _animator.SetBool("Guard", false);
                _animator.SetBool("Busy", false);
                _animator.ResetTrigger("Death");
                _animator.SetTrigger("Death");
            }
            deathPlace = transform.position;
            Explode();
            OnDeath.Invoke();

            PlaySound(DeathExplosionSound);

        }

        public void Explode()
        {
            // Explosion Effect
            if (ExplosionEffect != null)
            {
                Instantiate(ExplosionEffect, transform.position, Quaternion.identity);
            }
        }

        public void ResetState(){

            if(DissolveStatus > 0)
                return;

            _animator.enabled = true;       
            DissolveStatus = 1f;
            instanceMaterial.SetFloat("_Dissolve",DissolveStatus);
            transform.position = deathPlace;
            _rigidBody.linearVelocity = Vector2.zero;
            _rigidBody.angularVelocity = 0f;    
            
            foreach (Rigidbody2D rb in RBList)
            {
     
                   // rb.gravityScale = 1f;
                    rb.linearVelocity = Vector2.zero;
                    rb.angularVelocity = 0f;    
            
            }

            CurrentMovementState = MovementState.Idle;
            CurrentFightingState = FightingState.Idle;

        }

        public void Flip()
        {
            // Flip the enemy sprite by inverting the x scale
            Vector3 localScale = transform.localScale;
            localScale.x *= -1;
            transform.localScale = localScale;
            currentSpeed = currentSpeed * -1;

        }




        IEnumerator AttackRoutine()
        {
            _animator.SetTrigger("Attack");

            PlaySound(SwordSound);

            float waitTime = 0f;
            const float attackStateTimeout = 0.35f;

            while (!_animator.GetCurrentAnimatorStateInfo(0).IsName("Attack"))
            {
                waitTime += Time.deltaTime;

                if (waitTime >= attackStateTimeout)
                {
                    _animator.Play("Attack", 0, 0f);
                    break;
                }

                yield return null;
            }

            while (!_animator.GetCurrentAnimatorStateInfo(0).IsName("Attack"))
            {
                yield return null;
            }

            yield return new WaitForSeconds(0.14f);
            DealMeleeDamage();
            yield return new WaitForSeconds(0.22f);

            while (_animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 0.3f)
            {
                yield return null;
            }

            CurrentFightingState = FightingState.OnGuard;
        }

        IEnumerator OnHurtRoutine()
        {
            // Activate Hurt Animation
            _animator.SetTrigger("Hurt");

            // Waiting till Hurt animation is finished.
            while (!_animator.GetCurrentAnimatorStateInfo(0).IsName("Hurt"))
            {
                yield return null;
            }

            // Waiting time minimum
            while (_animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f)
            {
                yield return null;
            }

            _animator.SetBool("Busy",false);

            // Back to Guard
            CurrentFightingState = FightingState.OnGuard;
            CurrentMovementState = MovementState.Idle;

        }


        IEnumerator BeamShootRoutine(int attackToken)
        {
            PlaySound(PowerLoadSound);

            yield return new WaitForSeconds(BeamChargeTime);

            if (attackToken != beamAttackToken || CurrentFightingState != FightingState.Attacking)
            {
                if (_animator != null)
                {
                    _animator.SetBool("Busy", false);
                }

                yield break;
            }

            PlaySound(BeamSound);

            GameObject beamPrefab = DarkBall;
            if (beamPrefab == null)
            {
                beamPrefab = Resources.Load<GameObject>(BeamPrefabResourcePath);
            }

            if (beamPrefab != null)
            {
                Vector3 beamOrigin = BeamAttackRef != null
                    ? BeamAttackRef.position
                    : transform.position + new Vector3(transform.localScale.x > 0f ? 1.6f : -1.6f, 0.6f, 0f);
                Vector2 travelDirection = Vector2.right;
                if (BeamTarget != null)
                {
                    travelDirection = (BeamTarget.position - beamOrigin).normalized;
                }
                else if (transform.localScale.x < 0f)
                {
                    travelDirection = Vector2.left;
                }

                Vector3 spawnPosition = beamOrigin + (Vector3)(travelDirection.normalized * 2.8f);

                GameObject Ball = Instantiate(beamPrefab, spawnPosition, Quaternion.identity);
                Ball.SetActive(true);

                BeamShot beamShot = Ball.GetComponent<BeamShot>();
                if (beamShot != null)
                {
                    beamShot.launchSpeed = Mathf.Clamp(BeamProjectileSpeed, 3.5f, 6.5f);
                    beamShot.duration = Mathf.Clamp(BeamProjectileDuration, 2f, 5f);
                    beamShot.damage = AttackDamage;
                    beamShot.ownerRoot = transform.root;
                    beamShot.direction = travelDirection;
                }

                if (beamShot == null)
                {
                    Debug.LogWarning("DarkBall prefab is missing BeamShot component.");
                }
            }
            else
            {
                Debug.LogWarning("DarkBall prefab reference is missing.");
            }

            _animator.SetBool("Busy",false);
            CurrentFightingState = FightingState.Idle;
            CurrentMovementState = MovementState.Idle;
        }

        void EnableSwordHitbox(bool enabled)
        {
            if (SwordHitbox == null)
            {
                return;
            }

            if (SwordHitboxLogic != null)
            {
                SwordHitboxLogic.SetActiveHitbox(enabled);
            }

            SwordHitbox.enabled = enabled;
        }

        void DealMeleeDamage()
        {
            Vector3 center = GetMeleeHitboxCenter();

            Collider2D[] hits = Physics2D.OverlapBoxAll(center, MeleeHitboxSize, 0f);
            if (hits == null || hits.Length == 0)
            {
                return;
            }

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i] == null)
                {
                    continue;
                }

                player playerTarget = hits[i].GetComponentInParent<player>();
                if (playerTarget == null)
                {
                    playerTarget = hits[i].GetComponent<player>();
                }

                if (playerTarget != null && !player.isDie)
                {
                    playerTarget.TakeDamage(AttackDamage);
                    return;
                }
            }
        }

        Vector3 GetMeleeHitboxCenter()
        {
            float facingSign = transform.localScale.x >= 0f ? 1f : -1f;
            return transform.position + new Vector3(MeleeHitboxOffset.x * facingSign, MeleeHitboxOffset.y, 0f);
        }

        void ApplySwordHitboxSize()
        {
            if (SwordRef == null || SwordHitbox == null)
            {
                return;
            }

            SpriteRenderer[] swordSprites = SwordRef.GetComponentsInChildren<SpriteRenderer>(true);
            if (swordSprites == null || swordSprites.Length == 0)
            {
                SwordHitbox.size = new Vector2(2.0f, 1.0f);
                SwordHitbox.offset = new Vector2(0.8f, 0f);
                return;
            }

            Bounds worldBounds = swordSprites[0].bounds;
            for (int i = 1; i < swordSprites.Length; i++)
            {
                worldBounds.Encapsulate(swordSprites[i].bounds);
            }

            SwordHitbox.offset = SwordRef.InverseTransformPoint(worldBounds.center);
            SwordHitbox.size = worldBounds.size;
        }

        public List<Rigidbody2D> GetAllRigidbodiesInChildren(Transform parent)
        {
            List<Rigidbody2D> rigidbodies = new List<Rigidbody2D>(); 

            Rigidbody2D[] rbArray = parent.GetComponentsInChildren<Rigidbody2D>();

            foreach (Rigidbody2D rb in rbArray)
            {
                rigidbodies.Add(rb);
            }

            return rigidbodies; 
        }    
        
      public List<SpriteRenderer> GetAllSpriteRenderersInChildren(Transform parent)
        {
            List<SpriteRenderer> spriteRenderers = new List<SpriteRenderer>(); 

            SpriteRenderer[] srArray = parent.GetComponentsInChildren<SpriteRenderer>();


            foreach (SpriteRenderer sr in srArray)
            {
                spriteRenderers.Add(sr);
            }

            return spriteRenderers; 
        }

        private void PlaySound(AudioClip _clip){
            
            if(_clip == null){
                Debug.LogWarning("Sound not setted.");
                return;
            }
        
            if(_Channel == null){
                Debug.LogWarning("Audio Source not setted.");
                return;
            }

            _Channel.PlayOneShot(_clip);

        }

        // Is Built In?
        public bool IsBuiltIn(Material material)
        {
            return material.shader.name != "DarkKnight/DarkKnightShader";
        }
    }
}


