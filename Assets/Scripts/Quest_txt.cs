using UnityEngine;

public class Quest_txt : MonoBehaviour
{
    static Animator anim;

    void Start()
    {
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        
    }

    public static void delete_txt()
    {
        anim.SetBool("isClear", true);
    }
}
