using UnityEngine;
using UnityEngine.UI;

public class GameOverManager : MonoBehaviour
{
    public GameObject death_window;

    void Start()
    {
        death_window.SetActive(false);
    }

    void Update()
    {
        
    }
}
