using UnityEngine;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelTransitionTrigger4 : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Debug.Log("Trying to load scene: Tutorial_3.5");

            SceneManager.LoadScene("Tutorial_3.5");
        }
    }
}
