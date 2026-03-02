using FMOD.Studio;
using FMODUnity;
using Managers;
using UnityEngine;

[DisallowMultipleComponent]
public class TutorialOrbAudioEmitter : MonoBehaviour
{
    [SerializeField] private EventReference orbEvent;

    private EventInstance _activeInstance;
    private AudioManager _audioManager;

    public void Configure(EventReference assignedEvent)
    {
        orbEvent = assignedEvent;

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

    private void Update()
    {
        if (!_activeInstance.isValid() || !TryGetAudioManager(out AudioManager audioManager))
        {
            return;
        }

        audioManager.UpdateEventInstancePosition(_activeInstance, GetAudioWorldPosition());
    }

    private Vector3 GetVisualCenterWorldPosition()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            return transform.position;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds.center;
    }

    private void RestartPlayback()
    {
        StopPlaybackImmediate();

        if (orbEvent.IsNull)
        {
            return;
        }

        if (!TryGetAudioManager(out AudioManager audioManager))
        {
            return;
        }

        Vector3 audioWorldPosition = GetAudioWorldPosition();
        if (audioManager.TryStartSfxEventInstance(orbEvent, audioWorldPosition, out EventInstance instance))
        {
            _activeInstance = instance;
        }
    }

    private void StopPlaybackImmediate()
    {
        // Avoid Singleton.Instance here: during scene teardown AudioManager may already be destroyed.
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

        // Avoid Singleton.Instance here to prevent unload-time warning spam.
        _audioManager = FindAnyObjectByType<AudioManager>();
        audioManager = _audioManager;
        return audioManager != null;
    }

    private Vector3 GetAudioWorldPosition()
    {
        return GetVisualCenterWorldPosition();
    }
}
