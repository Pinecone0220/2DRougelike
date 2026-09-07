using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class player : MonoBehaviour
{
    [Header("Player Stats")]
    public float spd;
    public float jmp_spd;
    public float double_jmp_spd;
    public float dashSpd;
    public int dmg = 10;
    public float maxHp = 50;
    public float curHp = 0;
    public float cur_Stamina;
    public float max_Stamina = 100f;
    public float shield;
    public float maxShield;

    [Header("Movement Settings")]
    public float dashTime;
    public bool dashFreezeGravity = true;
    public float cur_dashCooldown;
    public float max_dashCooldown;
    public float dashEndXVelocity = 0f;
    public float staminaRecoveryDelay = 3f;
    public float staminaRecoveryRate = 10f;

    [Header("State Check")]
    public bool have_Key;
    public static bool isDie;
    bool isDash;
    int jumpCount = 0;
    int lookDir = 1;
    bool isClipSetToFall = false;
    bool isDoubleJumpAnimLocked = false;
    float doubleJumpAnimLockUntil = 0f;
    bool canDash = true;
    bool isInClearZone = false;
    bool isInStageClearZone = false;


    [Header("Ground Check")]
    public Transform groundCheck;
    public float checkRadius = 0.25f;
    public float groundCheckDistance = 0.05f;
    public LayerMask groundLayer;
    public bool isGround;

    [Header("UI")]
    public GameObject no_key;
    public GameObject key_Info;
    public GameObject gameClear_panel;
    public Text hp_txt;
    public Image Hp_img;
    public float hpLerpSpeed = 5f;
    public int recover_Hp;
    public Text stamina_txt;
    public Image stamina_img;
    public Image dash_ico;
    float lastDashTime;
    public GameObject skill_UI;
    public GameObject gameOver_UI;
    public GameObject gameOver_btn;
    public GameObject H_Potion;
    public ParticleSystem H_Potion_eft;
    public Text Q2_1;
    public Text Q2_2;

    [Header("Game Time")]
    public float clear_Time;
    public Text clearTime_txt;
    public Text bestTime_txt;
    public static bool isTimer;
    [Header("Components")]
    Rigidbody2D rb;
    Collider2D bodyCollider;
    Animator anim;
    SpriteRenderer sprit;
    AttackManager am;
    AudioSource As;
    AnimatorOverrideController overrideController;
    [Header("Audio Clips")]
    public AudioClip jump;
    public AudioClip get_Key;
    public AudioClip get_Jewel;
    public AudioClip game_clear;
    public AudioClip dash;
    public AudioClip die;
    public AudioClip get_H_Potion;
    public AudioClip hit;
    [Header("Animation Clips")]
    AnimationClip jumpRiseClip;
    AnimationClip jumpFallClip;
    AnimationClip doubleJumpClip;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        anim = GetComponent<Animator>();
        sprit = GetComponent<SpriteRenderer>();
        am = GetComponent<AttackManager>();
        As = GetComponent<AudioSource>();

        if (max_Stamina <= 0f)
        {
            max_Stamina = 100f;
        }

        curHp = maxHp;
        cur_Stamina = max_Stamina;
        Hp_img.fillAmount = (float)curHp / maxHp;

        if (double_jmp_spd == 0f)
        {
            double_jmp_spd = jmp_spd * 0.8f;
        }
        jumpCount = 0;


        overrideController = new AnimatorOverrideController(anim.runtimeAnimatorController);
        anim.runtimeAnimatorController = overrideController;

        jumpRiseClip = Resources.Load<AnimationClip>("DeadRevolver/PixelPrototypePlayerSprites/Art/Animations/SwordJumpRise");
        jumpFallClip = Resources.Load<AnimationClip>("DeadRevolver/PixelPrototypePlayerSprites/Art/Animations/SwordJumpFall");
        doubleJumpClip = Resources.Load<AnimationClip>("DeadRevolver/PixelPrototypePlayerSprites/Art/Animations/FrontFlip");

        if (skill_UI != null)
        {
            skill_UI.SetActive(false);
        }
        have_Key = false;
        isDie = false;
        if (key_Info != null)
        {
            key_Info.SetActive(false);
        }
        if (gameClear_panel != null)
        {
            gameClear_panel.SetActive(false);
        }
        if (no_key != null)
        {
            no_key.SetActive(false);
        }

        clear_Time = 0f;



        GameSessionData.LoadPlayerState(this);


        hp_txt.text = $"{curHp} / {maxHp}";
        Hp_img.fillAmount = (float)curHp / maxHp;
        stamina_txt.text = $"{Mathf.CeilToInt(cur_Stamina)} / {max_Stamina}";
        stamina_img.fillAmount = cur_Stamina / max_Stamina;
    }
    void FixedUpdate()
    {
        isGround = CheckGround();

        float x = 0f;

        if (Keyboard.current.rightArrowKey.isPressed && !am.isAttacking)
        {
            x = spd;
            anim.SetTrigger("isRun");
            transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            lookDir = 1;
        }

        else if (Keyboard.current.leftArrowKey.isPressed && !am.isAttacking)
        {
            x = -spd;
            anim.SetBool("isRun", true);
            transform.localRotation = Quaternion.Euler(0f, -180f, 0f);
            lookDir = -1;
        }
        else
        {
            anim.SetBool("isRun", false);
        }

        if (!isDash)
        {
            rb.linearVelocity = new Vector2(x, rb.linearVelocity.y);
        }

    }

    bool CheckGround()
    {
        if (bodyCollider == null)
        {
            return Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);
        }

        Bounds bounds = bodyCollider.bounds;


        Vector2 boxCenter = new Vector2(bounds.center.x, bounds.min.y - (groundCheckDistance / 2f));
        Vector2 boxSize = new Vector2(bounds.size.x * 0.9f, groundCheckDistance);

        Collider2D hit = Physics2D.OverlapBox(boxCenter, boxSize, 0f, groundLayer);
        return hit != null;
    }
    IEnumerator Dash()
    {
        canDash = false;
        isDash = true;

        cur_dashCooldown = max_dashCooldown;

        float originGravity = rb.gravityScale;

        if (dashFreezeGravity)
        {
            rb.gravityScale = 0f;
        }

        rb.linearVelocity = new Vector2(lookDir * dashSpd, 0f);

        yield return new WaitForSeconds(dashTime);

        if (dashFreezeGravity)
        {
            rb.gravityScale = originGravity;
        }

        isDash = false;
        rb.linearVelocity = new Vector2(lookDir * dashEndXVelocity, rb.linearVelocity.y);

        while (cur_dashCooldown > 0)
        {
            cur_dashCooldown -= Time.deltaTime;
            yield return null;
        }

        cur_dashCooldown = 0;
        canDash = true;
    }

    void Update()
    {
        if (isTimer)
        {
            clear_Time += Time.deltaTime;
        }

        if (Keyboard.current.cKey.wasPressedThisFrame && canDash && !isDash && !am.isAttacking && cur_Stamina >= 10f)
        {
            dash_ico.fillAmount = 0f;
            cur_Stamina -= 10f;
            lastDashTime = Time.time;
            anim.SetTrigger("Dash");
            As.PlayOneShot(dash);
            StartCoroutine(Dash());
        }

        if (Time.time - lastDashTime >= staminaRecoveryDelay)
        {
            cur_Stamina += staminaRecoveryRate * Time.deltaTime;

            if (cur_Stamina > max_Stamina)
            {
                cur_Stamina = max_Stamina;
            }
        }
        if (cur_Stamina < 10)
        {
            dash_ico.fillAmount = 0f;
        }
        else if (canDash)
        {
            dash_ico.fillAmount = 1f;
        }
        else
        {
            dash_ico.fillAmount = 1f - (cur_dashCooldown / max_dashCooldown);
        }


        stamina_txt.text = $"{Mathf.CeilToInt(cur_Stamina)} / {max_Stamina}";
        stamina_img.fillAmount = cur_Stamina / max_Stamina;

        anim.SetBool("isJump", !isGround);

        if (isDoubleJumpAnimLocked)
        {
            if (doubleJumpClip != null)
            {
                overrideController["SwordJumpRise"] = doubleJumpClip;
            }

            if (Time.time >= doubleJumpAnimLockUntil)
            {
                isDoubleJumpAnimLocked = false;
            }
        }
        else if (!isGround && jumpRiseClip != null && jumpFallClip != null)
        {
            if (rb.linearVelocity.y > 0.1f)
            {
                if (isClipSetToFall)
                {
                    overrideController["SwordJumpRise"] = jumpRiseClip;
                    isClipSetToFall = false;
                }
            }
            else if (rb.linearVelocity.y < -0.1f)
            {
                if (!isClipSetToFall)
                {
                    overrideController["SwordJumpRise"] = jumpFallClip;
                    isClipSetToFall = true;
                }
            }
        }
        else if (isGround && jumpRiseClip != null)
        {
            overrideController["SwordJumpRise"] = jumpRiseClip;
            isClipSetToFall = false;
            isDoubleJumpAnimLocked = false;
        }

        if (isGround)
        {
            jumpCount = 0;
        }

        if (Input.GetKeyDown(KeyCode.X) && !am.isAttacking)
        {
            if (isGround)
            {

                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                rb.AddForce(Vector2.up * jmp_spd, ForceMode2D.Impulse);
                As.PlayOneShot(jump);
                jumpCount = 1;
                isGround = false;
            }
            else if (jumpCount < 2)
            {

                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                rb.AddForce(Vector2.up * double_jmp_spd, ForceMode2D.Impulse);
                As.PlayOneShot(jump);
                if (doubleJumpClip != null)
                {
                    overrideController["SwordJumpRise"] = doubleJumpClip;
                    isDoubleJumpAnimLocked = true;
                    doubleJumpAnimLockUntil = Time.time + Mathf.Max(doubleJumpClip.length, 0.01f);
                    anim.Play("SwordRunAltGrip", 0, 0f);
                }
                isClipSetToFall = false;
                jumpCount = 2;
            }
        }
        GameSessionData.playerClearTime = clear_Time;

        if (!IsStage3Scene())
        {
            if (have_Key && isInClearZone && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                if (GetCurrentStageNumber() == 2)
                {
                    isTimer = false;
                    enabled = false;

                    UIManager.NextStage();
                }
                else
                {
                    isTimer = false;
                    enabled = false;

                    if (gameClear_panel != null)
                    {
                        gameClear_panel.SetActive(true);
                    }
                    float bestTime = PlayerPrefs.GetFloat("BestTime", float.MaxValue);

                    if (clear_Time < bestTime)
                    {
                        PlayerPrefs.SetFloat("BestTime", clear_Time);
                        PlayerPrefs.Save();
                    }

                    if (clearTime_txt != null)
                    {
                        clearTime_txt.text = $"Clear Time : {clear_Time:F1}s";
                    }
                    if (bestTime_txt != null)
                    {
                        bestTime_txt.text = $"Best Time : {PlayerPrefs.GetFloat("BestTime"):F1}s";
                    }

                    if (As != null && game_clear != null)
                    {
                        As.PlayOneShot(game_clear);
                    }
                    if (Q2_2 != null)
                    {
                        Q2_2.color = Color.yellow;
                    }

                    if (BGMManager.As != null)
                    {
                        BGMManager.As.volume = 0f;
                    }
                    Time.timeScale = 0f;
                }
            }

            if (have_Key && isInStageClearZone && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                isTimer = false;
                enabled = false;

                UIManager.NextStage();
            }
        }

        if (skill_UI != null && skill_UI.activeSelf == true)
        {
            if (BGMManager.As != null)
            {
                BGMManager.As.volume = 0.3f;
            }
            Time.timeScale = 0f;
        }
        else
        {
            if (BGMManager.As != null)
            {
                BGMManager.As.volume = 0.9f;
            }
            Time.timeScale = 1f;
        }

        

        if (curHp > maxHp)
        {
            curHp = maxHp;
        }

        hp_txt.text = $"{curHp} / {maxHp}";

        float targetHpFill = (float)curHp / maxHp;
        Hp_img.fillAmount = Mathf.Lerp(Hp_img.fillAmount, targetHpFill, Time.deltaTime * hpLerpSpeed);

        
    }

    public void TakeDamage(int damage)
    {


        if (shield > 0)
        {
            if (damage <= shield)
            {
                shield -= damage;
                damage = 0;
            }
            else
            {
                damage -= (int)shield;
                shield = 0;
            }
        }


        if (damage > 0)
        {
            curHp -= damage;

            if (curHp < 0)
                curHp = 0;
        }
        hp_txt.text = $"{curHp} / {maxHp}";
        Hp_img.fillAmount = (float)curHp / maxHp;
        if (curHp <= 0)
        {
            Die();
        }
    }
    
    
    public void Die()
    {
        isDie = true;
        isTimer = false;

        anim.SetBool("Die", true);
        this.enabled = false;
        As.PlayOneShot(die);

        Invoke("GameOver", 1.5f);
        Invoke("GameOver_Btn", 2.5f);
    }

    public void GameOver()
    {
        gameOver_UI.SetActive(true);
    }
    public void GameOver_Btn()
    {
        gameOver_btn.SetActive(true);
    }

    public void ShowStage3ClearPanel()
    {
        isTimer = false;
        enabled = false;

        if (gameClear_panel != null)
        {
            gameClear_panel.SetActive(true);
        }

        float bestTime = PlayerPrefs.GetFloat("BestTime", float.MaxValue);
        if (clear_Time < bestTime)
        {
            PlayerPrefs.SetFloat("BestTime", clear_Time);
            PlayerPrefs.Save();
        }

        if (clearTime_txt != null)
        {
            clearTime_txt.text = $"Clear Time : {clear_Time:F1}s";
        }

        if (bestTime_txt != null)
        {
            bestTime_txt.text = $"Best Time : {PlayerPrefs.GetFloat("BestTime"):F1}s";
        }

        if (As != null && game_clear != null)
        {
            As.PlayOneShot(game_clear);
        }

        if (Q2_2 != null)
        {
            Q2_2.color = Color.yellow;
        }

        if (BGMManager.As != null)
        {
            BGMManager.As.volume = 0f;
        }

        Time.timeScale = 0f;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsStage3Scene() && collision.gameObject.CompareTag("Key"))
        {
            if (As != null && get_Key != null)
            {
                As.PlayOneShot(get_Key);
            }
            have_Key = true;
            if (Q2_1 != null)
            {
                Q2_1.color = Color.yellow;
            }
        }

        if (!IsStage3Scene() && collision.gameObject.CompareTag("Jewel"))
        {
            if (As != null && get_Jewel != null)
            {
                As.PlayOneShot(get_Jewel);
            }
            if (skill_UI != null)
            {
                skill_UI.SetActive(true);
            }
        }

        if (collision.gameObject.CompareTag("H_Potion") && curHp < maxHp)
        {

            curHp += recover_Hp;
            if (H_Potion_eft != null)
            {
                H_Potion_eft.Play();
            }
            if (As != null && get_H_Potion != null)
            {
                As.PlayOneShot(get_H_Potion);
            }

            if (H_Potion != null)
            {
                H_Potion.SetActive(false);
            }
        }

        if (!IsStage3Scene() && collision.gameObject.CompareTag("Clear"))
        {
            isInClearZone = true;
        }

        if (!IsStage3Scene() && collision.gameObject.CompareTag("Stage1_clear"))
        {
            isInStageClearZone = true;
        }
    }
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (IsStage3Scene())
        {
            return;
        }

        if (have_Key && collision.gameObject.CompareTag("Clear"))
        {
            if (key_Info != null)
            {
                key_Info.SetActive(true);
            }
            isInClearZone = true;
        }
        else if (!collision.gameObject.CompareTag("Clear"))
        {
            if (key_Info != null)
            {
                key_Info.SetActive(false);
            }
        }

        if (!have_Key && collision.gameObject.CompareTag("Clear"))
        {
            if (no_key != null)
            {
                no_key.SetActive(true);
            }
            isInClearZone = true;
        }
        else
        {
            if (no_key != null)
            {
                no_key.SetActive(false);
            }
        }

        if (have_Key && collision.gameObject.CompareTag("Stage1_clear"))
        {
            if (key_Info != null)
            {
                key_Info.SetActive(true);
            }
            isInStageClearZone = true;
        }
        if(!have_Key && collision.gameObject.CompareTag("Stage1_clear"))
        {
            if (no_key != null)
            {
                no_key.SetActive(true);
            }
            isInStageClearZone = true;
        }
        else
        {
            if (no_key != null)
            {
                no_key.SetActive(false);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (IsStage3Scene())
        {
            return;
        }

        if (collision.gameObject.CompareTag("Clear"))
        {
            isInClearZone = false;
            if (key_Info != null)
            {
                key_Info.SetActive(false);
            }
            if (no_key != null)
            {
                no_key.SetActive(false);
            }
        }

        if (collision.gameObject.CompareTag("Stage1_clear"))
        {
            isInStageClearZone = false;
            if (key_Info != null)
            {
                key_Info.SetActive(false);
            }
            if (no_key != null)
            {
                no_key.SetActive(false);
            }
        }
    }

    int GetCurrentStageNumber()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        int lastSpace = sceneName.LastIndexOf(' ');
        if (lastSpace < 0 || lastSpace >= sceneName.Length - 1)
        {
            return 0;
        }

        string numberText = sceneName.Substring(lastSpace + 1);
        if (int.TryParse(numberText, out int stageNumber))
        {
            return stageNumber;
        }

        return 0;
    }

    bool IsStage3Scene()
    {
        return SceneManager.GetActiveScene().name == "Stage 3";
    }
}
