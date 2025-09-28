using UnityEngine;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelTransitionTrigger67 : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Debug.Log("Trying to load scene: End");

            SceneManager.LoadScene("End");
        }
    }
}
