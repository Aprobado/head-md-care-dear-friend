using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Yarn.Unity;

public class DialogueGlobalClick : MonoBehaviour
{
    public DialogueRunner dialogueRunner;
    public AudioDialoguePresenter audioDialoguePresenter;
    public Camera cam;
    private Button continueButton;
    private GameObject inputBlocker;

    void Awake()
    {
        GameObject continueButtonObject = GameObject.Find("Continue Button");
        if (continueButtonObject != null)
        {
            continueButton = continueButtonObject.GetComponent<Button>();

            if (continueButton != null)
                continueButton.gameObject.SetActive(false);
        }

        Canvas dialogueCanvas = dialogueRunner != null
            ? dialogueRunner.GetComponentInChildren<Canvas>(true)
            : null;

        inputBlocker = new GameObject(
            "Dialogue Input Blocker",
            typeof(RectTransform),
            typeof(Image),
            typeof(DialogueClickBlocker));

        if (dialogueCanvas != null)
        {
            inputBlocker.transform.SetParent(dialogueCanvas.transform, false);
            inputBlocker.transform.SetAsFirstSibling();
        }
        else
        {
            Canvas blockerCanvas = inputBlocker.AddComponent<Canvas>();
            blockerCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            blockerCanvas.sortingOrder = 1000;
            inputBlocker.AddComponent<GraphicRaycaster>();
        }

        Image blockerImage = inputBlocker.GetComponent<Image>();
        blockerImage.color = Color.clear;
        blockerImage.raycastTarget = true;

        RectTransform blockerTransform = inputBlocker.GetComponent<RectTransform>();
        blockerTransform.anchorMin = Vector2.zero;
        blockerTransform.anchorMax = Vector2.one;
        blockerTransform.offsetMin = Vector2.zero;
        blockerTransform.offsetMax = Vector2.zero;

        inputBlocker.GetComponent<DialogueClickBlocker>().owner = this;
        inputBlocker.SetActive(false);
    }

    void LateUpdate()
    {
        bool dialogueIsRunning = dialogueRunner != null && dialogueRunner.IsDialogueRunning;
        if (inputBlocker != null && inputBlocker.activeSelf != dialogueIsRunning)
            inputBlocker.SetActive(dialogueIsRunning);

        if (continueButton == null || audioDialoguePresenter == null ||
            audioDialoguePresenter.audioSource == null)
        {
            return;
        }

        if (audioDialoguePresenter.audioSource.isPlaying)
            continueButton.interactable = false;
    }

    void Update()
    {
    }

    private void AdvanceDialogue()
    {
        if (audioDialoguePresenter != null &&
            audioDialoguePresenter.audioSource != null &&
            audioDialoguePresenter.audioSource.isPlaying)
        {
            return;
        }

        if (dialogueRunner != null && dialogueRunner.IsDialogueRunning)
            dialogueRunner.RequestNextLine();
    }

    private sealed class DialogueClickBlocker : MonoBehaviour, IPointerClickHandler
    {
        public DialogueGlobalClick owner;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (FadeController.IsTransitioning)
                return;

            owner.AdvanceDialogue();
        }
    }
}