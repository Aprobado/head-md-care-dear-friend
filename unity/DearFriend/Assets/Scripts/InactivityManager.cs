using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Yarn.Unity;

// Unity 6 / Yarn Spinner 3.2.4.
// Uses the existing AudioDialoguePresenter without modifying its code.
public class InactivityManager : MonoBehaviour
{
    [Header("Timing (seconds)")]
    [Min(1f)] public float idleLimit = 120f;
    [Min(0.1f)] public float cornerHoldLimit = 2f;
    [Min(1f)] public float restartCountdown = 10f;
    [Range(0.02f, 0.2f)] public float cornerSize = 0.1f;

    [Header("Scene references")]
    public GameObject sessionPanel;
    public DialogueRunner dialogueRunner;
    public TMP_Text countdownText;
    public FadeController fadeController;

    [Header("Live timers - do not set manually")]
    public float idleTime;
    public float cornerHoldTime;

    private enum MenuState { Closed, Manual, IdleWarning }
    private MenuState menuState;
    private float countdownRemaining;
    private int heldCorner = -1;
    private bool restarting;
    private bool sessionStarted;
    private bool restartRequested;

    private void Start()
    {
        if (sessionPanel == null || countdownText == null)
        {
            Debug.LogError("Assign Session Panel and Countdown Text on InactivityManager.", this);
            enabled = false;
            return;
        }

        if (dialogueRunner == null)
            dialogueRunner = FindFirstObjectByType<DialogueRunner>(FindObjectsInactive.Include);

        if (fadeController == null)
            fadeController = FindFirstObjectByType<FadeController>(FindObjectsInactive.Include);

        if (dialogueRunner == null)
        {
            Debug.LogError("Assign the scene's Dialogue Runner on InactivityManager.", this);
            enabled = false;
            return;
        }

        countdownText.raycastTarget = false;
        ResumeGame();
        if (fadeController != null)
            StartCoroutine(fadeController.FadeIn());
    }

    private void Update()
    {
        // Let Yarn finish dispatching its command before stopping dialogue.
        if (restartRequested)
        {
            restartRequested = false;
            RestartGame();
            return;
        }

        if (restarting)
            return;

        float delta = Time.unscaledDeltaTime;
        bool pressed = TryGetPressPosition(out Vector2 position);

        if (menuState != MenuState.Closed)
        {
            ResetCornerHold();

            if (pressed)
            {
                idleTime = 0f;

                // Cancel the warning, but keep the buttons available.
                // Button.onClick fires on release, so do not hide the panel here.
                if (menuState == MenuState.IdleWarning)
                {
                    menuState = MenuState.Manual;
                    countdownText.gameObject.SetActive(false);
                }
                return;
            }

            if (menuState == MenuState.IdleWarning)
            {
                countdownRemaining -= delta;
                UpdateCountdownText();
                if (countdownRemaining <= 0f)
                    RestartGame();
            }
            else if (sessionStarted)
            {
                // Only an active play session can become inactive.
                idleTime += delta;
                if (idleTime >= idleLimit)
                    OpenMenu(true);
            }
            return;
        }

        if (pressed)
        {
            idleTime = 0f;
            CheckCornerHold(position);
            return;
        }
        
        if (AllCornersTouched())
            OpenMenu(false);

        ResetCornerHold();
        if (!sessionStarted)
            return;

        idleTime += delta;
        if (idleTime >= idleLimit)
            OpenMenu(true);
    }

    private bool TryGetPressPosition(out Vector2 position)
    {
        // Prefer touch so an attached mouse cannot override an iPad touch.
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            position = Touchscreen.current.primaryTouch.position.ReadValue();
            return true;
        }

        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            position = Mouse.current.position.ReadValue();
            return true;
        }

        position = Vector2.zero;
        return false;
    }

    private bool AllCornersTouched()
    {
        var touchCount = Touchscreen.current.touches.Count;
        if (touchCount < 4) return false;

        var corners = new List<int>();
        for (var i = 0; i < 4; i++)
        {
            var position = Touchscreen.current.touches[i].position.ReadValue();
            var corner = GetCorner(position);
            if (corner < 0) return false;   // not a corner
            if (corners.Contains(corner)) return false; // twice the same corner
            corners.Add(corner);
        }
        return true;
    }

    private void CheckCornerHold(Vector2 position)
    {
        int corner = GetCorner(position);
        if (corner < 0)
        {
            ResetCornerHold();
            return;
        }

        // Moving to a different corner starts a new hold.
        if (corner != heldCorner)
        {
            heldCorner = corner;
            cornerHoldTime = 0f;
        }

        cornerHoldTime += Time.unscaledDeltaTime;
        if (cornerHoldTime >= cornerHoldLimit)
            OpenMenu(false);
    }

    private int GetCorner(Vector2 position)
    {
        if (Screen.width <= 0 || Screen.height <= 0)
            return -1;

        float x = position.x / Screen.width;
        float y = position.y / Screen.height;
        if (x < 0f || x > 1f || y < 0f || y > 1f)
            return -1;

        int side = x <= cornerSize ? 0 : x >= 1f - cornerSize ? 1 : -1;
        int row = y <= cornerSize ? 0 : y >= 1f - cornerSize ? 1 : -1;
        return side < 0 || row < 0 ? -1 : row * 2 + side;
    }

    private void ResetCornerHold()
    {
        cornerHoldTime = 0f;
        heldCorner = -1;
    }

    private void OpenMenu(bool fromInactivity)
    {
        if (restarting)
            return;

        menuState = fromInactivity ? MenuState.IdleWarning : MenuState.Manual;
        idleTime = 0f;
        ResetCornerHold();
        countdownRemaining = restartCountdown;
        sessionPanel.SetActive(true);
        countdownText.gameObject.SetActive(fromInactivity);
        if (fromInactivity)
            UpdateCountdownText();
    }

    private void UpdateCountdownText()
    {
        int seconds = Mathf.CeilToInt(Mathf.Max(0f, countdownRemaining));
        countdownText.text = $"Redémarrage dans {seconds} s\nTouchez l'écran pour annuler.";
    }

    // Called by the password button when a player starts.
    public void BeginSession()
    {
        if (restarting || sessionStarted)
            return;

        sessionStarted = true;
        ResumeGame();
    }

    public void ResumeGame()
    {
        if (restarting)
            return;

        menuState = MenuState.Closed;
        idleTime = 0f;
        countdownRemaining = 0f;
        ResetCornerHold();
        if (countdownText != null)
            countdownText.gameObject.SetActive(false);
        if (sessionPanel != null)
            sessionPanel.SetActive(false);
    }

    [YarnCommand("RestartingGame")]
    public void RequestRestart()
    {
        if (!restarting)
            restartRequested = true;
    }

    public async void RestartGame()
    {
        // Prevent a second click / countdown from starting another reload.
        if (restarting)
            return;

        int sceneIndex = SceneManager.GetActiveScene().buildIndex;
        if (sceneIndex < 0)
        {
            Debug.LogError("Add and enable this scene in File > Build Profiles > Scene List.", this);
            OpenMenu(false);
            return;
        }

        restarting = true;
        try
        {
            // Read each presenter's exact reference: no ambiguous GetComponent<AudioSource>().
            var presenters = FindObjectsByType<AudioDialoguePresenter>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var presenter in presenters)
            {
                if (presenter != null && presenter.audioSource != null)
                    presenter.audioSource.Stop();
            }

            if (dialogueRunner != null && dialogueRunner.IsDialogueRunning)
                await dialogueRunner.Stop();

            // The provided audio presenter polls isPlaying after YarnTask.Yield().
            // Let its pending continuation observe Stop() before destroying the scene.
            await Awaitable.NextFrameAsync();
            await Awaitable.NextFrameAsync();

            if (fadeController != null)
                await fadeController.FadeOutAsync();

            // Also handle leaving Play Mode or an unrelated scene change during the wait.
            if (this == null)
                return;

            SceneManager.LoadScene(sceneIndex);
        }
        catch (Exception exception)
        {
            if (this == null)
                return;

            Debug.LogException(exception, this);
            restarting = false;
            OpenMenu(false);
        }
    }
}
