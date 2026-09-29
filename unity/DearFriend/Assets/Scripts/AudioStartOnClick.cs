using UnityEngine;
using UnityEngine.EventSystems;

public class ClickSound : MonoBehaviour, IPointerDownHandler
{
    public AudioClip clickSound;
    private AudioSource audioSource;

    void Start()
    {
        audioSource = gameObject.GetComponent<AudioSource>();
        clickSound = audioSource.clip;
        // if (audioSource == null)
        //     audioSource = gameObject.AddComponent<AudioSource>();
    }

    // void Update()
    // {
    //     if (Input.GetMouseButtonDown(0)) // 0 = left click
    //     {
    //         audioSource.PlayOneShot(clickSound);
    //     }
    // }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (FadeController.IsTransitioning)
            return;

        audioSource.PlayOneShot(clickSound);
    }
}