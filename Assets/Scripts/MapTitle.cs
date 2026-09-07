using UnityEngine;

public class MapTitle : MonoBehaviour
{
    public GameObject mapTitle;
    public GameObject Quest;

    void Start()
    {
        mapTitle.SetActive(false);
        Quest.SetActive(false);
    }

    void Update()
    {
        Invoke("Title_fadeIn", 2f);
        Invoke("Quest_FadeIn", 4f);
    }

    public void Title_fadeIn()
    {
        mapTitle.SetActive(true);
    }
    public void Quest_FadeIn()
    {
        Quest.SetActive(true);
    }
}
