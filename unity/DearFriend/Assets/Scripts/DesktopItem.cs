using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Yarn.Unity;

public class DesktopItem : MonoBehaviour,
    IPointerClickHandler,
    IPointerDownHandler,
    IPointerUpHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    public bool canClick = false;
    public bool canDrag = false;
    private bool deleteAfterClick = false;

    [Header("Audio")]
    public AudioClip inputSound;
    public AudioClip dropSound;
    private AudioSource audioSource;

    [Header("Yarn")]
    public DialogueRunner dialogueRunner;

    public string clickNodeName;
    public string trashNodeName;
    public string trashAfterOpenNodeName;
    public string dropElsewhereNodeName;

    public Camera cam;

    [Header("Selection Scale")]
    public float selectedScaleMultiplier = 1.15f;

    [Header("Open File Object")]
    public GameObject fileContentObject;
    public GameObject[] contentWindowsToIgnore;

    [Header("Asset Disabled While Open")]
    public GameObject assetToDisableWhenOpen;

    [Header("Trash Material")]
    public Material trashHoverMaterial;

    [Header("Trash Target")]
    public GameObject trashObject;

    [Header("Folder Target")]
    public GameObject folderObject;
    public Material folderHoverMaterial;

    [Header("Trash Animation")]
    public Transform trashTransform;
    public float trashBigScale = 1.3f;
    public float trashPulseDuration = 1f;

    [Header("Folder Animation")]
    public Transform folderTransform;
    public float folderBigScale = 1.3f;
    public float folderPulseDuration = 1f;

    private Vector3 originalScale;
    private Vector3 dragStartPosition;
    private Vector3 dragOffset;

    private Renderer objectRenderer;
    private Material[] originalMaterials;

    private Vector3 trashOriginalScale;
    private Coroutine trashPulseCoroutine;
    private Vector3 folderOriginalScale;
    private Coroutine folderPulseCoroutine;

    private bool isDragging;
    private bool wasDragged;
    private bool isOverTrash;
    private bool isOverFolder;

    // Track whether the file has been opened at least once
    private bool hasOpenedFile = false;
    private static bool isContentOpen;
    private static GameObject openContentObject;

    public static void SetContentOpen(bool value)
    {
        isContentOpen = value;
        if (!value)
        {
            openContentObject = null;
        }
    }

    [YarnCommand("closeDesktopContent")]
    public static void CloseDesktopContent()
    {
        if (openContentObject != null)
        {
            openContentObject.SetActive(false);
        }

        SetContentOpen(false);
    }

    private static void SetContentOpen(GameObject contentObject)
    {
        isContentOpen = true;
        openContentObject = contentObject;
    }

    private bool IsBlockedByOpenContent()
    {
        if (!isContentOpen)
        {
            return false;
        }

        if (openContentObject == null || !openContentObject.activeInHierarchy)
        {
            SetContentOpen(false);
            return false;
        }

        if (openContentObject != null &&
            transform.IsChildOf(openContentObject.transform))
        {
            return false;
        }

        if (openContentObject != null &&
            openContentObject.name == "Folder_Eugenie_Window" &&
            name.StartsWith("File", System.StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (contentWindowsToIgnore != null)
        {
            foreach (var ignoredContent in contentWindowsToIgnore)
            {
                if (ignoredContent == openContentObject)
                {
                    return false;
                }
            }
        }

        return true;
    }

    void Start()
    {
        if (cam == null)
        {
            cam = Camera.main;
        }

        EnsurePointerCollider();
        AlignPointerColliderToVisuals();

        originalScale = transform.localScale;

        objectRenderer = GetComponent<Renderer>();

        if (objectRenderer != null)
        {
            originalMaterials = objectRenderer.materials;
        }

        if (trashTransform == null && trashObject != null)
        {
            trashTransform = trashObject.transform;
        }

        if (trashTransform != null)
        {
            trashOriginalScale = trashTransform.localScale;
        }

        if (folderTransform == null && folderObject != null)
        {
            folderTransform = folderObject.transform;
        }

        if (folderTransform != null)
        {
            folderOriginalScale = folderTransform.localScale;
        }

        // Get AudioSource from this GameObject
        audioSource = GetComponent<AudioSource>();

        // Optional: use AudioSource clip automatically
        if (inputSound == null)
        {
            inputSound = audioSource.clip;
        }
    }

    void EnsurePointerCollider()
    {
        if (GetComponent<Collider>() != null)
        {
            return;
        }

        var meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            return;
        }

        var meshCollider = gameObject.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = meshFilter.sharedMesh;
    }

    void AlignPointerColliderToVisuals()
    {
        var boxCollider = GetComponent<BoxCollider>();
        var renderers = GetComponentsInChildren<Renderer>(true);

        if (boxCollider == null || renderers.Length == 0)
        {
            return;
        }

        Bounds worldBounds = renderers[0].bounds;

        for (var i = 1; i < renderers.Length; i++)
        {
            worldBounds.Encapsulate(renderers[i].bounds);
        }

        var localBounds = new Bounds(
            transform.InverseTransformPoint(worldBounds.center),
            Vector3.zero
        );

        var worldCorners = new Vector3[8];
        var min = worldBounds.min;
        var max = worldBounds.max;
        worldCorners[0] = new Vector3(min.x, min.y, min.z);
        worldCorners[1] = new Vector3(min.x, min.y, max.z);
        worldCorners[2] = new Vector3(min.x, max.y, min.z);
        worldCorners[3] = new Vector3(min.x, max.y, max.z);
        worldCorners[4] = new Vector3(max.x, min.y, min.z);
        worldCorners[5] = new Vector3(max.x, min.y, max.z);
        worldCorners[6] = new Vector3(max.x, max.y, min.z);
        worldCorners[7] = new Vector3(max.x, max.y, max.z);

        foreach (var corner in worldCorners)
        {
            localBounds.Encapsulate(transform.InverseTransformPoint(corner));
        }

        boxCollider.center = localBounds.center;
        boxCollider.size = localBounds.size;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (FadeController.IsTransitioning || IsBlockedByOpenContent() || !canDrag)
        {
            return;
        }
        // Scale up the selected file/folder
        transform.localScale = originalScale * selectedScaleMultiplier;

        // Start trash animation while player is holding this item
        StartTrashPulse();
        StartFolderPulse();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (FadeController.IsTransitioning || !canDrag)
        {
            return;
        }
        // Stop trash animation when player releases
        StopTrashPulse();
        StopFolderPulse();

        // If it was only a click, scale back down
        if (!isDragging)
        {
            transform.localScale = originalScale;
        }
    }

    [YarnCommand("setCanClick")]
    public static void SetCanClick(string objectName, bool value)
    {
        var item = FindDesktopItem(objectName);

        if (item == null)
        {
            Debug.LogWarning($"DesktopItem: unable to find '{objectName}' for setCanClick.");
            return;
        }

        item.canClick = value;

        foreach (var collider in item.GetComponents<Collider>())
        {
            collider.enabled = value;
        }

        var button = item.GetComponent<Button>();
        if (button != null)
        {
            button.interactable = value;
        }
    }
    
    [YarnCommand("setCanDrag")]
    public static void SetCanDrag(string objectName, bool value)
    {
        var item = FindDesktopItem(objectName);

        if (item == null)
        {
            Debug.LogWarning($"DesktopItem: unable to find '{objectName}' for setCanDrag.");
            return;
        }

        item.canDrag = value;

        foreach (var collider in item.GetComponents<Collider>())
        {
            collider.enabled = value || item.canClick;
        }
    }

    [YarnCommand("setObjectColor")]
    public static void SetObjectColor(string objectName, string htmlColor)
    {
        var item = FindDesktopItem(objectName);

        if (item == null)
        {
            Debug.LogWarning($"DesktopItem: unable to find '{objectName}' for setObjectColor.");
            return;
        }

        if (!ColorUtility.TryParseHtmlString(htmlColor, out var color))
        {
            Debug.LogWarning($"DesktopItem: invalid color '{htmlColor}' for setObjectColor.");
            return;
        }

        var renderers = item.GetComponentsInChildren<Renderer>(true);

        foreach (var renderer in renderers)
        {
            renderer.material.color = color;
        }
    }

    static DesktopItem FindDesktopItem(string objectName)
    {
        var items = Object.FindObjectsByType<DesktopItem>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (var item in items)
        {
            if (item.name == objectName)
            {
                return item;
            }
        }

        return null;
    }

    [YarnCommand("debugOpened")]
    public static void debugOpened()
    {
        var dialogueRunner = Object.FindFirstObjectByType<DialogueRunner>();

        if (dialogueRunner == null || dialogueRunner.VariableStorage == null)
        {
            Debug.LogWarning("Desktop: unable to read $opened because DialogueRunner or VariableStorage is missing.");
            return;
        }

        if (dialogueRunner.VariableStorage.TryGetValue("$opened", out float opened))
        {
            Debug.Log($"Desktop: opened = {opened}");
        }
        else
        {
            Debug.LogWarning("Desktop: variable $opened was not found.");
        }
    }

    [YarnCommand("deleteItem")]
    public void deleteItem()
    {
        deleteAfterClick = true;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (FadeController.IsTransitioning)
        {
            return;
        }

        if (IsBlockedByOpenContent())
        {
            Debug.LogWarning($"DesktopItem: click blocked on '{name}' because another content is open.");
            return;
        }

        if (hasOpenedFile)
        {
            Debug.LogWarning($"DesktopItem: click blocked on '{name}' because it has already been opened.");
            return;
        }

        if (!canClick)
        {
            Debug.LogWarning($"DesktopItem: click blocked on '{name}' because canClick is false.");
            return;
        }
        // Mark the file as opened so trashing is no longer treated as the first interaction
        hasOpenedFile = true;

        if (inputSound != null)
        {
            audioSource.clip = inputSound;
            audioSource.Play();

            Debug.Log("Played click sound");
        }

        // Prevent click from firing after drag
        if (wasDragged)
        {
            wasDragged = false;
            return;
        }

        // Activate the image/content inside the file
        if (fileContentObject != null)
        {
            SetContentOpen(fileContentObject);

            for (var parent = fileContentObject.transform.parent; parent != null; parent = parent.parent)
            {
                parent.gameObject.SetActive(true);
            }

            fileContentObject.SetActive(true);

            if (assetToDisableWhenOpen != null)
            {
                assetToDisableWhenOpen.SetActive(false);
            }

            foreach (var closeWindow in fileContentObject.GetComponentsInChildren<CloseWindow>(true))
            {
                closeWindow.SetAssetToEnableOnClose(assetToDisableWhenOpen);
            }
        }

        // Start Yarn dialogue for clicking/opening this file
        var nodeToPlay = clickNodeName;
        if (string.IsNullOrEmpty(nodeToPlay))
        {
            var match = Regex.Match(name, @"^DocumentIcon_(\d+)$");
            if (match.Success)
            {
                nodeToPlay = $"Fichier{match.Groups[1].Value}";
            }
        }

        if (dialogueRunner != null && !string.IsNullOrEmpty(nodeToPlay))
        {
            Debug.Log($"DesktopItem: OnPointerClick received, starting dialogue node '{nodeToPlay}'.");
            dialogueRunner.StartDialogue(nodeToPlay);
        }
        else
        {
            Debug.LogError($"DesktopItem: cannot open '{name}'. Assign a DialogueRunner and clickNodeName.");
        }

        if(deleteAfterClick)
        {
            Destroy(gameObject);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (IsBlockedByOpenContent() || !canDrag)
        {
            return;
        }
        isDragging = true;
        wasDragged = true;

        dragStartPosition = transform.position;

        // Create ray from mouse/touch position
        Ray ray = cam.ScreenPointToRay(eventData.position);

        // Drag on XY plane, keeping Z fixed
        Plane dragPlane = new Plane(Vector3.forward, dragStartPosition);

        if (dragPlane.Raycast(ray, out float distance))
        {
            Vector3 worldPoint = ray.GetPoint(distance);

            // Prevent object from snapping to pointer center
            dragOffset = transform.position - worldPoint;
        }

        transform.localScale = originalScale * selectedScaleMultiplier;

        // StartTrashPulse();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if(!canDrag)
        {
            return;
        }
        Ray ray = cam.ScreenPointToRay(eventData.position);

        // Drag on XY plane, keeping Z fixed
        Plane dragPlane = new Plane(Vector3.forward, dragStartPosition);

        if (dragPlane.Raycast(ray, out float distance))
        {
            Vector3 worldPosition = ray.GetPoint(distance) + dragOffset;

            transform.position = new Vector3(
                worldPosition.x,
                worldPosition.y,
                dragStartPosition.z
            );
        }

        CheckDropTargetHover(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!canDrag)
        {
            return;
        }
        isDragging = false;

        transform.localScale = originalScale;

        StopTrashPulse();
        StopFolderPulse();

        bool droppedOnTrash = folderObject == null &&
            (IsPointerOverTrash(eventData) || IsObjectOverTrash());

        if (folderObject != null && IsObjectOverTrash())
        {
            transform.position = dragStartPosition;
            ResetMaterial();

            if (dialogueRunner != null && !string.IsNullOrEmpty(trashNodeName))
            {
                dialogueRunner.StartDialogue(trashNodeName);
            }

            return;
        }

        if (droppedOnTrash)
        {
            // Choose the correct trash node based on whether the file was opened.
            string nodeToPlay = trashNodeName;

            if (hasOpenedFile && !string.IsNullOrEmpty(trashAfterOpenNodeName))
            {
                nodeToPlay = trashAfterOpenNodeName;
            }

            if (dialogueRunner != null && !string.IsNullOrEmpty(nodeToPlay))
            {
                dialogueRunner.StartDialogue(nodeToPlay);
            }

            PlayDropSound();
            Destroy(gameObject);
            return;
        }

        if (IsPointerOverFolder(eventData))
        {
            if (dialogueRunner != null && !string.IsNullOrEmpty(trashNodeName))
            {
                dialogueRunner.StartDialogue(trashNodeName);
            }

            PlayDropSound();
            Destroy(gameObject);
            return;
        }

        // Invalid drop: snap the item back to its position before dragging.
        transform.position = dragStartPosition;
        ResetMaterial();

        if (dialogueRunner != null && !string.IsNullOrEmpty(dropElsewhereNodeName))
        {
            dialogueRunner.StartDialogue(dropElsewhereNodeName);
        }
    }

    void PlayDropSound()
    {
        if (dropSound != null)
        {
            AudioSource.PlayClipAtPoint(dropSound, transform.position);
        }
    }

    void CheckDropTargetHover(PointerEventData eventData)
    {
        bool currentlyOverTrash = IsPointerOverTrash(eventData);
        bool currentlyOverFolder = IsPointerOverFolder(eventData);

        if (currentlyOverFolder && !isOverFolder)
        {
            isOverFolder = true;
            isOverTrash = false;
            SetAllMaterials(folderHoverMaterial);
        }
        else if (currentlyOverTrash && !isOverTrash)
        {
            isOverTrash = true;
            isOverFolder = false;
            SetAllMaterials(trashHoverMaterial);
        }
        else if (!currentlyOverTrash && !currentlyOverFolder && (isOverTrash || isOverFolder))
        {
            ResetMaterial();
        }
    }

    bool IsPointerOverTrash(PointerEventData eventData)
    {
        if (folderObject != null)
        {
            return false;
        }

        if (IsPointerOverTarget(eventData, trashObject, "Trash"))
        {
            return true;
        }

        return false;
    }

    bool IsObjectOverTrash()
    {
        if (trashObject == null)
        {
            return false;
        }

        Collider[] fileColliders = GetComponentsInChildren<Collider>();
        Collider[] trashColliders = trashObject.GetComponentsInChildren<Collider>();

        foreach (Collider fileCollider in fileColliders)
        {
            foreach (Collider trashCollider in trashColliders)
            {
                if (fileCollider.bounds.Intersects(trashCollider.bounds))
                {
                    return true;
                }
            }
        }

        return false;
    }

    bool IsPointerOverFolder(PointerEventData eventData)
    {
        if (folderObject == null)
        {
            return false;
        }

        return IsPointerOverTarget(eventData, folderObject, null);
    }

    bool IsPointerOverTarget(PointerEventData eventData, GameObject targetObject, string targetTag)
    {
        Ray ray = cam.ScreenPointToRay(eventData.position);

        foreach (RaycastHit hit in Physics.RaycastAll(ray))
        {
            if (targetObject != null &&
                (hit.collider.transform == targetObject.transform ||
                 hit.collider.transform.IsChildOf(targetObject.transform)))
            {
                return true;
            }

            if (targetObject == null && !string.IsNullOrEmpty(targetTag) &&
                hit.collider.CompareTag(targetTag))
            {
                return true;
            }
        }

        return false;
    }

    void SetAllMaterials(Material newMaterial)
    {
        if (objectRenderer == null || newMaterial == null)
        {
            return;
        }

        Material[] newMaterials = new Material[objectRenderer.materials.Length];

        for (int i = 0; i < newMaterials.Length; i++)
        {
            newMaterials[i] = newMaterial;
        }

        objectRenderer.materials = newMaterials;
    }

    void ResetMaterial()
    {
        if (objectRenderer != null && originalMaterials != null)
        {
            objectRenderer.materials = originalMaterials;
        }

        isOverTrash = false;
        isOverFolder = false;
    }

    void StartTrashPulse()
    {
        if (trashTransform == null)
        {
            return;
        }

        if (trashPulseCoroutine != null)
        {
            StopCoroutine(trashPulseCoroutine);
        }

        trashPulseCoroutine = StartCoroutine(TrashPulseLoop());
    }

    void StopTrashPulse()
    {
        if (trashPulseCoroutine != null)
        {
            StopCoroutine(trashPulseCoroutine);
            trashPulseCoroutine = null;
        }

        if (trashTransform != null)
        {
            trashTransform.localScale = trashOriginalScale;
        }
    }

    void StartFolderPulse()
    {
        if (folderTransform == null)
        {
            return;
        }

        if (folderPulseCoroutine != null)
        {
            StopCoroutine(folderPulseCoroutine);
        }

        folderPulseCoroutine = StartCoroutine(FolderPulseLoop());
    }

    void StopFolderPulse()
    {
        if (folderPulseCoroutine != null)
        {
            StopCoroutine(folderPulseCoroutine);
            folderPulseCoroutine = null;
        }

        if (folderTransform != null)
        {
            folderTransform.localScale = folderOriginalScale;
        }
    }

    IEnumerator TrashPulseLoop()
    {
        Vector3 bigScale = trashOriginalScale * trashBigScale;

        while (true)
        {
            yield return ScaleTrash(trashOriginalScale, bigScale, trashPulseDuration);
            yield return ScaleTrash(bigScale, trashOriginalScale, trashPulseDuration);
        }
    }

    IEnumerator FolderPulseLoop()
    {
        Vector3 bigScale = folderOriginalScale * folderBigScale;

        while (true)
        {
            yield return ScaleFolder(folderOriginalScale, bigScale, folderPulseDuration);
            yield return ScaleFolder(bigScale, folderOriginalScale, folderPulseDuration);
        }
    }

    IEnumerator ScaleTrash(Vector3 fromScale, Vector3 toScale, float duration)
    {
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float t = timer / duration;

            trashTransform.localScale = Vector3.Lerp(fromScale, toScale, t);

            yield return null;
        }

        trashTransform.localScale = toScale;
    }

    IEnumerator ScaleFolder(Vector3 fromScale, Vector3 toScale, float duration)
    {
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float t = timer / duration;

            folderTransform.localScale = Vector3.Lerp(fromScale, toScale, t);

            yield return null;
        }

        folderTransform.localScale = toScale;
    }
}