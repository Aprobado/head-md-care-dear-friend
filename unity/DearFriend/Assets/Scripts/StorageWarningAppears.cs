using UnityEngine;
using UnityEngine.EventSystems;
using Yarn.Unity;


public class StorageWarningAppears : MonoBehaviour, IPointerDownHandler
{
   public DialogueRunner dialogueRunner;
   public string dialogueNodeName;
   //public GameObject objectToDeactivate;


   public void OnPointerDown(PointerEventData eventData)
   {
       if (FadeController.IsTransitioning)
           return;

       //delete object after 3 seconds
       Destroy(gameObject, 3f);
       if (dialogueRunner != null && !dialogueRunner.IsDialogueRunning)
       {           dialogueRunner.StartDialogue(dialogueNodeName);
       }
   }


}