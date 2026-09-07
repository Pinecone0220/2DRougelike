using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TealFalconEnemySeries;

public class BossManager : MonoBehaviour
{
    [Header("Intro")]
    public float cameraTravelTime = 0.7f;
    public float introPause = 0.75f;
    public float introBeamHoldTime = 2.2f;
    public float introShakeTime = 2.2f;
    public float introShakeMagnitude = 0.12f;
    public float deathShakeTime = 0.6f;
    public float deathShakeMagnitude = 0.16f;
    public string deathTriggerName = "Death";
    public float phase1MeleeRange = 2.4f;
    public float phase2BeamRange = 6f;
    public float phase2MeleeRange = 2.8f;
    public float phase1PatternCooldown = 2.5f;
    public float phase2PatternCooldown = 3.4f;
    public float phase2BeamPatternCooldown = 10f;
    public float phase2MeleePatternCooldown = 1.35f;
    public float attackWindupTime = 0.12f;
    public float attackRecoverTime = 0.75f;
    public float beamRecoverTime = 2.8f;
    public float backStepRecoverTime = 0.35f;
    public float hurtRecoverTime = 0.5f;

    [Header("Combat")]
    public int maxHp = 300;
    public int curHp;
    public float hitFlashTime = 0.12f;
    public float walkSpeed = 2f;
    public float runSpeed = 7f;
    public GameObject hpRoot;
    public Slider hpBar;

    Text hpValueText;

    Animator rootAnimator;
    DarkKnightController darkKnightController;
    Transform playerTarget;
    Rigidbody2D[] cachedBodies;
    SpriteRenderer[] cachedSprites;
    Color[] spriteBaseColors;
    bool introPlayed;
    bool isDead;
    bool isActionLocked;
    bool isBeamCharging;
    int currentPhase = -1;
    float nextPatternTime;

    void Awake()
    {
        rootAnimator = transform.Find("Root") != null
            ? transform.Find("Root").GetComponent<Animator>()
            : GetComponentInChildren<Animator>(true);
        darkKnightController = GetComponent<DarkKnightController>();
        cachedBodies = GetComponentsInChildren<Rigidbody2D>(true);
        cachedSprites = GetComponentsInChildren<SpriteRenderer>(true);
        curHp = maxHp;

        if (hpRoot != null)
        {
            hpRoot.SetActive(false);
            EnsureHpValueText();
        }

        if (cachedSprites != null)
        {
            spriteBaseColors = new Color[cachedSprites.Length];
            for (int i = 0; i < cachedSprites.Length; i++)
            {
                if (cachedSprites[i] != null)
                {
                    spriteBaseColors[i] = cachedSprites[i].color;
                }
            }
        }
    }

    void Start()
    {
        if (SceneManager.GetActiveScene().name != "Stage 3")
        {
            enabled = false;
            return;
        }

        if (darkKnightController != null)
        {
            darkKnightController.enabled = false;
            darkKnightController.BeamTarget = playerTarget;
            darkKnightController.ActivateIdle();
        }

        SetBodiesSimulated(false);

        if (hpRoot != null)
        {
            hpRoot.SetActive(false);
        }

        UpdateHpBar();
    }

    public IEnumerator PlayStage3Intro(Transform playerTarget)
    {
        if (introPlayed)
        {
            yield break;
        }

        introPlayed = true;
        this.playerTarget = playerTarget;

        if (darkKnightController != null)
        {
            darkKnightController.enabled = false;
            darkKnightController.BeamTarget = playerTarget;
        }

        SetBodiesSimulated(false);
        SetCinemachineBrainEnabled(false);

        yield return StartCoroutine(MoveCameraTo(GetBossCameraPoint(), cameraTravelTime));
        yield return new WaitForSecondsRealtime(introPause);

        if (hpRoot != null)
        {
            hpRoot.SetActive(true);
        }

        if (rootAnimator != null)
        {
            rootAnimator.Play("BeamAttack", 0, 0f);
        }

        StartCoroutine(ShakeCamera(introShakeTime, introShakeMagnitude));
        yield return new WaitForSecondsRealtime(introBeamHoldTime);

        yield return StartCoroutine(MoveCameraTo(GetPlayerCameraPoint(playerTarget), cameraTravelTime));

        SetCinemachineBrainEnabled(true);
        SetBossInteractive(true);
    }

    void Update()
    {
        if (!introPlayed || isDead || darkKnightController == null || playerTarget == null)
        {
            return;
        }

        int phase = GetCurrentPhase();
        if (phase != currentPhase)
        {
            ApplyPhase(phase);
        }

        FacePlayer();
        if (isBeamCharging)
        {
            return;
        }
        UpdateChaseState(phase);
        TryStartPattern(phase);
    }

    IEnumerator MoveCameraTo(Vector3 targetPosition, float duration)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            yield break;
        }

        Vector3 startPosition = mainCamera.transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            t = Mathf.SmoothStep(0f, 1f, t);
            mainCamera.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        mainCamera.transform.position = targetPosition;
    }

    bool SetCinemachineBrainEnabled(bool enabled)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return false;
        }

        Behaviour brain = mainCamera.GetComponent("Unity.Cinemachine.CinemachineBrain") as Behaviour;
        if (brain != null)
        {
            bool previousEnabled = brain.enabled;
            brain.enabled = enabled;
            return previousEnabled;
        }

        return false;
    }

    Vector3 GetBossCameraPoint()
    {
        return transform.position + new Vector3(0f, 1f, -10f);
    }

    Vector3 GetPlayerCameraPoint(Transform target)
    {
        if (target == null)
        {
            Camera mainCamera = Camera.main;
            return mainCamera != null ? mainCamera.transform.position : GetBossCameraPoint();
        }

        return target.position + new Vector3(0f, 1f, -10f);
    }

    public void TakeDamage(int damage)
    {
        if (isDead)
        {
            return;
        }

        curHp -= damage;
        if (curHp < 0)
        {
            curHp = 0;
        }

        UpdateHpBar();
        StartCoroutine(FlashHit());

        if (darkKnightController != null && !isActionLocked && !isBeamCharging)
        {
            StartCoroutine(PlayHurtReaction());
        }

        if (curHp <= 0)
        {
            StartCoroutine(DieSequence());
        }
    }

    void SetBodiesSimulated(bool simulated)
    {
        if (cachedBodies == null)
        {
            return;
        }

        for (int i = 0; i < cachedBodies.Length; i++)
        {
            if (cachedBodies[i] != null)
            {
                cachedBodies[i].simulated = simulated;
            }
        }
    }

    void SetBossInteractive(bool interactive)
    {
        if (darkKnightController != null)
        {
            darkKnightController.enabled = interactive;

            if (interactive)
            {
                darkKnightController.BeamTarget = playerTarget;
                darkKnightController.ActivateIdle();
            }
        }

        SetBodiesSimulated(interactive);
    }

    int GetCurrentPhase()
    {
        if (maxHp <= 0)
        {
            return 1;
        }

        float hpPercent = (float)curHp / maxHp;
        return hpPercent <= 0.5f ? 2 : 1;
    }

    void ApplyPhase(int phase)
    {
        currentPhase = phase;

        if (darkKnightController == null)
        {
            return;
        }

        darkKnightController.MaxWalkSpeed = walkSpeed;
        darkKnightController.MaxRunSpeed = runSpeed;
    }

    void UpdateChaseState(int phase)
    {
        if (darkKnightController == null || isBeamCharging)
        {
            return;
        }

        if (phase == 2)
        {
            if (darkKnightController.CurrentMovementState != DarkKnightController.MovementState.Running)
            {
                darkKnightController.ActivateRun();
            }
        }
        else
        {
            if (darkKnightController.CurrentMovementState != DarkKnightController.MovementState.Walking)
            {
                darkKnightController.ActivateWalk();
            }
        }
    }

    void TryStartPattern(int phase)
    {
        if (isActionLocked || isBeamCharging || darkKnightController == null || playerTarget == null || Time.time < nextPatternTime)
        {
            return;
        }

        float horizontalDistance = Mathf.Abs(transform.position.x - playerTarget.position.x);

        if (phase == 1)
        {
            if (horizontalDistance <= phase1MeleeRange)
            {
                StartCoroutine(Phase1MeleePattern());
            }

            return;
        }

        if (horizontalDistance <= phase2MeleeRange)
        {
            StartCoroutine(Phase2ClosePattern());
        }
        else if (horizontalDistance <= phase2BeamRange)
        {
            StartCoroutine(Phase2BeamPattern());
        }
    }

    IEnumerator Phase1MeleePattern()
    {
        isActionLocked = true;
        nextPatternTime = Time.time + phase1PatternCooldown;

        if (darkKnightController != null)
        {
            darkKnightController.ActivateAttack();
        }

        yield return new WaitForSecondsRealtime(attackRecoverTime);

        if (darkKnightController != null && !isDead)
        {
            darkKnightController.ActivateIdle();
        }

        isActionLocked = false;
    }

    IEnumerator Phase2BeamPattern()
    {
        isActionLocked = true;
        isBeamCharging = true;
        nextPatternTime = Time.time + phase2BeamPatternCooldown;

        darkKnightController.BeamTarget = playerTarget;
        darkKnightController.ActivateBeamAttack();

        yield return new WaitForSecondsRealtime(beamRecoverTime);

        if (darkKnightController != null && !isDead)
        {
            darkKnightController.ActivateIdle();
        }

        isBeamCharging = false;
        isActionLocked = false;
    }

    IEnumerator Phase2ClosePattern()
    {
        isActionLocked = true;
        nextPatternTime = Time.time + phase2MeleePatternCooldown;

        if (darkKnightController != null)
        {
            darkKnightController.ActivateAttack();
        }

        yield return new WaitForSecondsRealtime(attackRecoverTime);

        if (darkKnightController != null && !isDead)
        {
            darkKnightController.ActivateIdle();
        }

        isActionLocked = false;
    }

    void FacePlayer()
    {
        if (playerTarget == null || darkKnightController == null)
        {
            return;
        }

        bool playerOnRight = playerTarget.position.x > transform.position.x;
        bool facingRight = transform.localScale.x > 0f;

        if (playerOnRight != facingRight)
        {
            darkKnightController.Flip();
        }
    }

    void UpdateHpBar()
    {
        if (maxHp <= 0)
        {
            return;
        }

        if (hpBar != null)
        {
            hpBar.minValue = 0f;
            hpBar.maxValue = maxHp;
            hpBar.wholeNumbers = true;
            hpBar.value = curHp;
        }

        if (hpValueText != null)
        {
            hpValueText.text = $"{curHp} / {maxHp}";
        }
    }

    void EnsureHpValueText()
    {
        if (hpRoot == null || hpValueText != null)
        {
            return;
        }

        GameObject textObject = new GameObject("HpValueText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(hpRoot.transform, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 8f);
        rect.sizeDelta = new Vector2(220f, 36f);

        hpValueText = textObject.GetComponent<Text>();
        hpValueText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hpValueText.fontSize = 20;
        hpValueText.fontStyle = FontStyle.Bold;
        hpValueText.alignment = TextAnchor.MiddleCenter;
        hpValueText.color = Color.white;
        hpValueText.raycastTarget = false;
        hpValueText.text = $"{curHp} / {maxHp}";
    }

    IEnumerator FlashHit()
    {
        if (cachedSprites == null || spriteBaseColors == null)
        {
            yield break;
        }

        for (int i = 0; i < cachedSprites.Length; i++)
        {
            if (cachedSprites[i] != null)
            {
                cachedSprites[i].color = Color.white;
            }
        }

        yield return new WaitForSeconds(hitFlashTime);

        for (int i = 0; i < cachedSprites.Length; i++)
        {
            if (cachedSprites[i] != null)
            {
                cachedSprites[i].color = spriteBaseColors[i];
            }
        }
    }

    IEnumerator PlayHurtReaction()
    {
        if (darkKnightController == null || isDead)
        {
            yield break;
        }

        darkKnightController.ActivateHurt();
        yield return new WaitForSecondsRealtime(hurtRecoverTime);

        if (darkKnightController != null && !isDead)
        {
            darkKnightController.ActivateIdle();
        }
    }

    IEnumerator ShakeCamera(float duration, float magnitude)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null || duration <= 0f || magnitude <= 0f)
        {
            yield break;
        }

        Behaviour brain = mainCamera.GetComponent("Unity.Cinemachine.CinemachineBrain") as Behaviour;
        bool restoreBrain = brain != null && brain.enabled;
        if (brain != null)
        {
            brain.enabled = false;
        }

        Vector3 basePosition = mainCamera.transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            Vector2 offset = UnityEngine.Random.insideUnitCircle * magnitude;
            mainCamera.transform.position = basePosition + new Vector3(offset.x, offset.y, 0f);
            yield return null;
        }

        mainCamera.transform.position = basePosition;

        if (brain != null)
        {
            brain.enabled = restoreBrain;
        }
    }

    IEnumerator DieSequence()
    {
        if (isDead)
        {
            yield break;
        }

        isDead = true;
        EnemyManager.kill_count += 1;
        SetBossInteractive(false);

        if (darkKnightController != null)
        {
            darkKnightController.ActivateDeath();
        }
        else if (rootAnimator != null)
        {
            rootAnimator.ResetTrigger("Death");
            rootAnimator.SetTrigger("Death");
            rootAnimator.Update(0f);
        }

        StartCoroutine(ShakeCamera(deathShakeTime, deathShakeMagnitude));
        yield return new WaitForSecondsRealtime(deathShakeTime);

        if (hpRoot != null)
        {
            hpRoot.SetActive(false);
        }

        if (SceneManager.GetActiveScene().name == "Stage 3")
        {
            BGMManager.StopBgm();
            EnemyBullet.StopAllProjectiles();

            player playerInstance = playerTarget != null ? playerTarget.GetComponent<player>() : FindObjectOfType<player>();
            if (playerInstance != null)
            {
                playerInstance.ShowStage3ClearPanel();
            }

            Destroy(gameObject, 1.2f);
            yield break;
        }
    }
}
