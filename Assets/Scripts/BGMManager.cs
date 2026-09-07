using UnityEngine;
using UnityEngine.SceneManagement;

public class BGMManager : MonoBehaviour
{
    public static AudioSource As;
    const string Stage3BgmPath = "Fantasy Music Starter Pack/The Adventurer Combat Full";

    void Start()
    {
        As = GetComponent<AudioSource>();

        if (SceneManager.GetActiveScene().name == "Stage 3")
        {
            AudioClip stage3Clip = Resources.Load<AudioClip>(Stage3BgmPath);
            if (stage3Clip != null && As != null)
            {
                As.clip = stage3Clip;
                As.loop = true;
                As.Play();
            }
        }

        As.volume = 0.9f;
    }

    void Update()
    {
        
    }

    public static void StopBgm()
    {
        if (As == null)
        {
            return;
        }

        As.Stop();
        As.volume = 0f;
    }
}
