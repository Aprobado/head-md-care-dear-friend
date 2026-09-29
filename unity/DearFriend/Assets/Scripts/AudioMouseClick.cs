using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlayInputSound : MonoBehaviour
{
    public AudioClip inputSound;

    private AudioSource audioSource;

    void Start()
    {
        // Get AudioSource from this same GameObject
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (FadeController.IsTransitioning)
        {
            return;
        }

        // Mouse click
        if (Input.GetMouseButtonDown(0))
        {
            PlaySound();
        }

        // Touch tap
        if (Input.touchCount > 0 &&
            Input.GetTouch(0).phase == TouchPhase.Began)
        {
            PlaySound();
        }
    }
    

    void PlaySound()
    {
        audioSource.PlayOneShot(inputSound);
    }
}