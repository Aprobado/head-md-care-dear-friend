using UnityEngine;
using UnityEngine.EventSystems;
using Yarn.Unity;

public class PulsatingFile : MonoBehaviour
{
    public float pulseAmount = 0.1f;
    public float pulseSpeed = 3f;

    Vector3 originalScale;
    bool pulsing = false;

    void Start()
    {
        originalScale = transform.localScale;
    }

    void Update()
    {
        if (!pulsing) return;

        float scale = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;

        if (transform.name.StartsWith("Folder_"))
        {
            // Folders pulse on Y and Z while keeping their X scale unchanged.
            transform.localScale = new Vector3(
                originalScale.x,
                originalScale.y * scale,
                originalScale.z * scale
            );
            return;
        }

        // Documents and other objects keep their original uniform pulse.
        transform.localScale = originalScale * scale;
    }

    [YarnCommand("startPulsatingFile")]
    public void pulsatingButton()
    {
        pulsing = true;

        CloseButtons closeButton = GetComponent<CloseButtons>();
        if (closeButton != null)
            closeButton.SetCanClick(true);
    }

    [YarnCommand("stopPulsatingFile")]
    public void stopPulsating()
    {
        pulsing = false;
        transform.localScale = originalScale; // Reset to original scale when stopping
    }

    // public void OnPointerDown(PointerEventData eventData)
    // {
    //     pulsing = false;
    // }
}