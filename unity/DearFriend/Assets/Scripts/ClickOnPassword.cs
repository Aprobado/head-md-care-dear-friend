using UnityEngine;
using UnityEngine.EventSystems;
using Yarn.Unity;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class ClickOnPassword : MonoBehaviour, IPointerDownHandler
{
    public InactivityManager inactivityManager;
    public DialogueRunner dialogueRunner;
    public string dialogueNodeName;

    public GameObject objectToActivate;

    public AudioClip inputSound;

    private AudioSource audioSource;

    void Start()
    {
        // Get AudioSource from this GameObject
        audioSource = GetComponent<AudioSource>();

        // Optional: use AudioSource clip automatically
        if (inputSound == null)
        {
            inputSound = audioSource.clip;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (inactivityManager != null)
            inactivityManager.BeginSession();

        StartCoroutine(HandleClick());
    }

    IEnumerator HandleClick()
    {
        // Play sound and wait until finished
        if (inputSound != null)
        {
            audioSource.clip = inputSound;
            audioSource.Play();

            yield return new WaitUntil(() => !audioSource.isPlaying);

            Debug.Log("Played click sound");
        }

        // Start dialogue
        if (dialogueRunner != null && !dialogueRunner.IsDialogueRunning)
        {
            dialogueRunner.StartDialogue(dialogueNodeName);
        }

        // Activate objects
        if (objectToActivate != null)
        {
            objectToActivate.SetActive(true);
        }

        // Destroy parent object
        Destroy(transform.parent.gameObject);
    }
}