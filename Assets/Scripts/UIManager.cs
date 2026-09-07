using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    public Text quest_title;
    public Text quest_content;

    public GameObject quest2_content;
    public Text Q2_1;
    public Text Q2_2;

    public GameObject warning_tab;
    public GameObject Skill_UI;

    AudioSource As;
    player player;
    public static int stageKillGoal = 1;
    bool questRewardSpawned = false;

    GameObject bossOverlayRoot;
    CanvasGroup bossOverlayGroup;
    Image bossOverlayBackdrop;
    Text bossOverlayTitle;
    Text bossOverlayBody;
    Coroutine bossOverlayRoutine;

    public AudioClip click;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        As = GetComponent<AudioSource>();
        player = GameObject.Find("Player").GetComponent<player>();

        if (warning_tab != null)
        {
            warning_tab.SetActive(false);
        }

        if (quest2_content != null)
        {
            quest2_content.SetActive(false);
        }
        EnemyManager.kill_count = 0;
        stageKillGoal = GetKillGoalForScene(SceneManager.GetActiveScene().name);
        questRewardSpawned = false;

        if (IsStage3Scene())
        {
            if (quest_title != null)
            {
                quest_title.enabled = false;
            }

            if (quest_content != null)
            {
                quest_content.enabled = false;
            }
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void Update()
    {
        if (IsStage3Scene())
        {
            if (quest_title != null)
            {
                quest_title.enabled = false;
            }

            if (quest_content != null)
            {
                quest_content.enabled = false;
            }

            if (quest2_content != null)
            {
                quest2_content.SetActive(false);
            }

            return;
        }

        if (quest_title == null || quest_content == null)
        {
            return;
        }

        quest_title.text = "퀘스트";
        quest_content.text = $"열쇠 생성을 위한 적 처치 ({EnemyManager.kill_count}/{stageKillGoal})";

        if (EnemyManager.kill_count >= stageKillGoal)
        {
            Quest_txt.delete_txt();
            SpawnQuestRewards();
        }

        if (!IsStage3Scene() && quest_content.color.a <= 0)
        {
            if (quest2_content != null)
            {
                quest2_content.SetActive(true);
            }
        }
        else if (IsStage3Scene() && quest2_content != null)
        {
            quest2_content.SetActive(false);
        }
    }

    public void Close_tab()
    {
        Skill_UI.SetActive(false);
        warning_tab.SetActive(false);
    }

    public void Cancle()
    {
        warning_tab.SetActive(false);
    }

    public void UI_Sound()
    {
        As.PlayOneShot(click);
    }

    public void Warning_tab()
    {
        warning_tab.SetActive(true);
    }

    public IEnumerator PlayStage3BossIntro(player playerInstance)
    {
        EnsureBossOverlay();

        if (playerInstance != null)
        {
            playerInstance.enabled = false;
        }

        player.isTimer = false;
        ShowBossOverlay("STAGE 3", "BOSS APPROACHING", new Color(0.85f, 0.12f, 0.12f, 0.92f), Color.white, Color.white);
        yield return new WaitForSecondsRealtime(1.2f);

        ShowBossOverlay("WARNING", "GET READY", new Color(0.55f, 0.08f, 0.08f, 0.88f), Color.yellow, Color.white);
        yield return new WaitForSecondsRealtime(0.9f);

        BossManager boss = FindObjectOfType<BossManager>();
        if (boss == null)
        {
            boss = SpawnDarkKnightBoss(playerInstance);
        }

        if (boss != null)
        {
            Transform playerTarget = playerInstance != null ? playerInstance.transform : GameObject.FindWithTag("Player")?.transform;
            yield return StartCoroutine(boss.PlayStage3Intro(playerTarget));
        }

        HideBossOverlay();

        if (playerInstance != null)
        {
            playerInstance.enabled = true;
        }

        player.isTimer = true;
    }

    BossManager SpawnDarkKnightBoss(player playerInstance)
    {
        GameObject prefab = Resources.Load<GameObject>("Dark Knight/Prefabs/Dark Knight");
        if (prefab == null)
        {
            return null;
        }

        Vector3 spawnPosition = Vector3.zero;
        if (playerInstance != null)
        {
            spawnPosition = playerInstance.transform.position + new Vector3(8f, 0f, 0f);
        }

        GameObject bossObject = Instantiate(prefab, spawnPosition, Quaternion.identity);
        BossManager boss = bossObject.GetComponent<BossManager>();
        if (boss == null)
        {
            boss = bossObject.AddComponent<BossManager>();
        }

        return boss;
    }

    public void ShowBossWarning(string title, string body)
    {
        EnsureBossOverlay();

        if (bossOverlayRoutine != null)
        {
            StopCoroutine(bossOverlayRoutine);
            bossOverlayRoutine = null;
        }

        ShowBossOverlay(title, body, new Color(0.95f, 0.15f, 0.15f, 0.78f), Color.white, Color.yellow);
    }

    public void HideBossOverlay()
    {
        if (bossOverlayRoot != null)
        {
            bossOverlayRoot.SetActive(false);
        }
    }

    void EnsureBossOverlay()
    {
        if (bossOverlayRoot != null)
        {
            return;
        }

        GameObject canvasObject = GameObject.Find("Canvas");
        if (canvasObject == null)
        {
            return;
        }

        Transform canvasTransform = canvasObject.transform;

        bossOverlayRoot = new GameObject("BossOverlay", typeof(RectTransform), typeof(CanvasGroup));
        bossOverlayRoot.transform.SetParent(canvasTransform, false);

        RectTransform rootRect = bossOverlayRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        bossOverlayGroup = bossOverlayRoot.GetComponent<CanvasGroup>();
        bossOverlayGroup.alpha = 1f;
        bossOverlayRoot.SetActive(false);

        GameObject backdropObject = new GameObject("Backdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        backdropObject.transform.SetParent(bossOverlayRoot.transform, false);

        RectTransform backdropRect = backdropObject.GetComponent<RectTransform>();
        backdropRect.anchorMin = Vector2.zero;
        backdropRect.anchorMax = Vector2.one;
        backdropRect.offsetMin = Vector2.zero;
        backdropRect.offsetMax = Vector2.zero;

        bossOverlayBackdrop = backdropObject.GetComponent<Image>();
        bossOverlayBackdrop.color = new Color(0f, 0f, 0f, 0.65f);

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        bossOverlayTitle = CreateOverlayText("Title", bossOverlayRoot.transform, font, 64, FontStyle.Bold, new Vector2(0f, 90f), new Vector2(900f, 120f));
        bossOverlayBody = CreateOverlayText("Body", bossOverlayRoot.transform, font, 32, FontStyle.Bold, new Vector2(0f, -10f), new Vector2(900f, 100f));
    }

    void ShowBossOverlay(string title, string body, Color panelColor, Color titleColor, Color bodyColor)
    {
        EnsureBossOverlay();

        if (bossOverlayRoot == null)
        {
            return;
        }

        bossOverlayRoot.SetActive(true);

        if (bossOverlayBackdrop != null)
        {
            bossOverlayBackdrop.color = panelColor;
        }

        if (bossOverlayTitle != null)
        {
            bossOverlayTitle.text = title;
            bossOverlayTitle.color = titleColor;
        }

        if (bossOverlayBody != null)
        {
            bossOverlayBody.text = body;
            bossOverlayBody.color = bodyColor;
        }

        if (bossOverlayGroup != null)
        {
            bossOverlayGroup.alpha = 1f;
        }
    }

    Text CreateOverlayText(string objectName, Transform parent, Font font, int fontSize, FontStyle style, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        Text text = textObject.GetComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;
        text.text = string.Empty;

        return text;
    }

    #region ?ㅽ꺈 媛뺥솕

    public void ATK_Up()
    {
        player.dmg += 5;
    }

    public void Hp_Up()
    {
        player.curHp += 10;
        player.maxHp += 10;
    }

    public void Heal_Now()
    {
        player.curHp += 15;
    }

    #endregion

    public void Retry()
    {
        GameSessionData.ResetSession();
        Time.timeScale = 1f;

        SceneManager.LoadScene("Stage 1");
    }

    public void GoTitle()
    {
        SceneManager.LoadScene("Title");
    }

    public static void NextStage()
    {
        player playerInstance = GameObject.Find("Player").GetComponent<player>();
        if (playerInstance != null)
        {
            GameSessionData.SavePlayerState(playerInstance);
        }

        Time.timeScale = 1f;

        string currentSceneName = SceneManager.GetActiveScene().name;
        int stageNumber = GetStageNumber(currentSceneName);

        if (stageNumber > 0)
        {
            int nextStageNumber = stageNumber + 1;

            if (nextStageNumber <= 3)
            {
                SceneManager.LoadScene($"Stage {nextStageNumber}");
                return;
            }
        }

        GameSessionData.ResetSession();
        SceneManager.LoadScene("Title");
    }

    static int GetKillGoalForScene(string sceneName)
    {
        int stageNumber = GetStageNumber(sceneName);
        if (stageNumber == 1)
        {
            return 3;
        }

        if (stageNumber == 2)
        {
            return 5;
        }

        if (stageNumber == 3)
        {
            return 1;
        }

        return 1;
    }

    static int GetStageNumber(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            return 0;
        }

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

    void SpawnQuestRewards()
    {
        if (questRewardSpawned)
        {
            return;
        }

        questRewardSpawned = true;

        if (stageKillGoal <= 1)
        {
            return;
        }

        ItemManager[] rewards = Object.FindObjectsOfType<ItemManager>(true);
        for (int i = 0; i < rewards.Length; i++)
        {
            if (rewards[i] != null)
            {
                rewards[i].gameObject.SetActive(true);
            }
        }
    }
}
