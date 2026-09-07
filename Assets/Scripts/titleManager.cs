using UnityEngine;
using UnityEngine.SceneManagement;

public class titleManager : MonoBehaviour
{
    AudioSource audioSource;

    public void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        Time.timeScale = 1;
    }

    public void Go_GameScene()
    {
        GameSessionData.ResetSession(); // 게임 시작 전 세션 초기화
        SceneManager.LoadScene("Stage 1"); // 첫 번째 스테이지 로드
    }

    public void GameOff()
    {
        Application.Quit();
    }
}
