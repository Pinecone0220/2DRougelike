using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class OpeningManager : MonoBehaviour
{
    public Image fadePanel;
    public float fadeTime = 2f;

    public player player;

    void Start()
    {
        fadePanel.enabled = true;
        player.enabled = false;

        StartCoroutine(FadeOut());
    }

    IEnumerator FadeOut()
    {
        player.isTimer = false;
        Color color = fadePanel.color;

        float t = 0f;

        while (t < fadeTime)
        {
            t += Time.deltaTime;

            color.a = 1f - (t / fadeTime);
            fadePanel.color = color;

            yield return null;
        }

        color.a = 0f;
        fadePanel.color = color;
        fadePanel.enabled = false;

        if (IsStage3Scene())
        {
            UIManager ui = UIManager.Instance != null ? UIManager.Instance : FindObjectOfType<UIManager>();
            if (ui != null)
            {
                yield return StartCoroutine(ui.PlayStage3BossIntro(player));
                yield break;
            }
        }

        player.enabled = true;
        player.isTimer = true;
    }

    bool IsStage3Scene()
    {
        return SceneManager.GetActiveScene().name == "Stage 3";
    }
}
