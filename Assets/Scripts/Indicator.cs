using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Indicator : MonoBehaviour
{
    public Transform player;
    public Transform target;

    public GameObject item_target;
    public Image indi_Target;

    void Start()
    {
        indi_Target = GetComponent<Image>();
        if (indi_Target != null)
        {
            indi_Target.enabled = false;
        }
    }

    void Update()
    {
        if (SceneManager.GetActiveScene().name == "Stage 3")
        {
            if (indi_Target != null)
            {
                indi_Target.enabled = false;
            }

            return;
        }

        if (item_target == null || target == null || player == null || indi_Target == null)
        {
            return;
        }

        indi_Target.enabled = item_target.activeSelf;

        Vector2 dir = target.position - player.position;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        transform.rotation = Quaternion.Euler(0, 0, angle - 45);
    }
}
