using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

public class CloseButtons : MonoBehaviour
{
    public DialogueRunner dialogueRunner;
    public string nodeNameAfterClose;
    public bool canClick;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        UpdateButtonState();
    }

    private void OnEnable()
    {
        SetCanClick(true);
    }

    [YarnCommand("setCanClickForCloseButtons")]
    public void SetCanClick(bool value)
    {
        canClick = value;
        UpdateButtonState();
    }

    public async void CloseParentImage()
    {
        if (!canClick || FadeController.IsTransitioning)
            return;

        Transform buttonRoot = transform.parent;
        Transform window = buttonRoot != null ? buttonRoot.parent : null;

        if (window != null)
        {
            DesktopItem.CloseDesktopContent();
            DesktopItem.SetContentOpen(false);
            window.gameObject.SetActive(false);

            if (dialogueRunner != null && !string.IsNullOrEmpty(nodeNameAfterClose))
            {
                await FadeController.WaitForTransitionAsync();
                dialogueRunner.StartDialogue(nodeNameAfterClose);
            }
        }
        else if (buttonRoot != null)
        {
            DesktopItem.CloseDesktopContent();
            buttonRoot.gameObject.SetActive(false);
        }
    }

    private void UpdateButtonState()
    {
        if (button != null)
            button.interactable = canClick;
    }
}