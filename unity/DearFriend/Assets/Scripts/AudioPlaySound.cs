using System.Collections;
using UnityEngine;
using Yarn.Unity;

[RequireComponent(typeof(AudioSource))]
public class AudioPlaySound : MonoBehaviour
{
    public AudioClip ComputerWarning;
    public AudioClip ComputerShutdown;
    public AudioClip ComputerHumming;
    public AudioClip ComputerFansSpeedsUp;
    public AudioClip CameraZoomOut;
    public AudioClip KitchenTimerTicTac;
    public AudioClip KitchenTimerDing;
    public AudioClip TrashedFile;
    public AudioClip SaveddFile;
    public AudioClip PackingBag;
    public AudioClip LastZoomOut;


    [Header("Playback")]
    [SerializeField] private bool loopComputerWarning;
    [SerializeField] private bool loopComputerShutdown;
    [SerializeField] private bool loopComputerHumming;
    [SerializeField] private bool loopComputerFansSpeedsUp;
    [SerializeField] private bool loopCameraZoomOut;
    [SerializeField] private bool loopKitchenTimerTicTac = true;
    [SerializeField] private bool loopKitchenTimerDing;

    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private AudioClip GetClip(string soundName)
    {
        return soundName switch
        {
            "Warning"       => ComputerWarning,
            "ZoomOut"       => CameraZoomOut,
            "ComputerFanSpeedsUp" => ComputerFansSpeedsUp,
            "ComputerShutdown"  => ComputerShutdown,
            "ComputerHumming" => ComputerHumming,
            "KitchenTimerTicTac" => KitchenTimerTicTac,
            "KitchenTimerDing" => KitchenTimerDing,
            "TrashedFile" => TrashedFile,
            "SavedFile" => SaveddFile,
            "PackingBag" => PackingBag,
            "LastZoomOut" => LastZoomOut,
            _               => null
        };
    }

    private bool ShouldLoop(string soundName)
    {
        return soundName switch
        {
            "Warning" => loopComputerWarning,
            "ZoomOut" => loopCameraZoomOut,
            "ComputerFanSpeedsUp" => loopComputerFansSpeedsUp,
            "ComputerShutdown" => loopComputerShutdown,
            "ComputerHumming" => loopComputerHumming,
            "KitchenTimerTicTac" => loopKitchenTimerTicTac,
            "KitchenTimerDing" => loopKitchenTimerDing,
            _ => false
        };
    }

    [YarnCommand("PlaySound")]
    public IEnumerator PlaySound(string soundName)
    {
        AudioClip clip = GetClip(soundName);

        if (clip == null)
        {
            Debug.LogWarning($"Sound not found: {soundName}");
            yield break;
        }

        audioSource.clip = clip;
        audioSource.loop = ShouldLoop(soundName);
        audioSource.Play();

    }

    [YarnCommand("PlaySoundAndWait")]
    public IEnumerator PlaySoundAndWait(string soundName)
    {
        yield return PlaySound(soundName);

        audioSource.loop = false;
        yield return new WaitWhile(() => audioSource.isPlaying);
    }

    [YarnCommand("StopSound")]
    public void StopSound(string soundName)
    {
        audioSource.Stop();
        audioSource.loop = false;
    }
}