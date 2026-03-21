using FMOD.Studio;
using FMODUnity;
using Managers;
using UnityEngine;

[DisallowMultipleComponent]
public class RoofRandomEmitter : MonoBehaviour
{
    [SerializeField] private EventReference roofEvent;

    private EventInstance _activeInstance;
    private AudioManager _audioManager;

    public void Configure(EventReference assignedEvent)
    {
        roofEvent = assignedEvent;

        if (isActiveAndEnabled)
        {
            RestartPlayback();
        }
    }

    private void OnEnable()
    {
        RestartPlayback();
    }

    private void OnDisable()
    {
        StopPlaybackImmediate();
    }

    private void OnDestroy()
    {
        StopPlaybackImmediate();
    }

    private void RestartPlayback()
    {
        StopPlaybackImmediate();

        if (roofEvent.IsNull || !TryGetAudioManager(out AudioManager audioManager))
        {
            return;
        }

        if (audioManager.TryStartSfxEventInstance(
                roofEvent,
                transform.position,
                randomizeTimelinePosition: true,
                out EventInstance instance))
        {
            _activeInstance = instance;
        }
    }

    private void StopPlaybackImmediate()
    {
        AudioManager audioManager = Object.FindAnyObjectByType<AudioManager>();
        if (audioManager != null)
        {
            audioManager.StopAndReleaseEventInstance(ref _activeInstance, immediate: true);
        }
        else if (_activeInstance.isValid())
        {
            _activeInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            _activeInstance.release();
            _activeInstance = default;
        }
    }

    private bool TryGetAudioManager(out AudioManager audioManager)
    {
        if (_audioManager != null)
        {
            audioManager = _audioManager;
            return true;
        }

        _audioManager = Object.FindAnyObjectByType<AudioManager>();
        audioManager = _audioManager;
        return audioManager != null;
    }
}
