using UnityEngine;
using UnityEngine.SceneManagement;

public class EndTrigger : MonoBehaviour
{
    private bool gameEnded = false;

    private void OnTriggerEnter(Collider other)
    {
        gameEnded = true;
    }

    private void Update()
    {
        if (gameEnded && !FadeController.IsTransitioning)
        {
            // Mobile touch
            if (Input.touchCount > 0)
            {
                RestartGame();
            }

            // Mouse click (works in editor/PC)
            if (Input.GetMouseButtonDown(0))
            {
                RestartGame();
            }
        }
    }

    void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}