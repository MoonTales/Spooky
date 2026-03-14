using FMOD.Studio;
using Managers;
using UnityEngine;

[DisallowMultipleComponent]
public class SpiderAudioEmitter : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private Transform audioAnchor;
    [SerializeField] private Attractor targetAttractor;
    [SerializeField] private AttractorAI targetAttractorAI;

    private EventInstance _activeInstance;
    private AudioManager _audioManager;

    public void Configure(Transform anchor, Attractor attractor, AttractorAI attractorAI)
    {
        audioAnchor = anchor != null ? anchor : transform;
        targetAttractor = attractor;
        targetAttractorAI = attractorAI;

        if (isActiveAndEnabled)
        {
            RestartPlayback();
        }
    }

    private void OnEnable()
    {
        RestartPlayback();
    }

    private void Update()
    {
        if (!_activeInstance.isValid() || !TryGetAudioManager(out AudioManager audioManager))
        {
            return;
        }

        audioManager.UpdateSpiderLoop(
            _activeInstance,
            GetAnchorTransform(),
            GetCurrentIntensity(),
            GetCurrentDangerLevel(),
            GetCurrentStateValue());
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

        if (!TryGetAudioManager(out AudioManager audioManager))
        {
            return;
        }

        if (!audioManager.TryStartSpiderLoop(
                GetAnchorTransform(),
                GetCurrentIntensity(),
                GetCurrentDangerLevel(),
                GetCurrentStateValue(),
                out EventInstance instance))
        {
            return;
        }

        _activeInstance = instance;
    }

    private void StopPlaybackImmediate()
    {
        AudioManager audioManager = Object.FindAnyObjectByType<AudioManager>();
        if (audioManager != null)
        {
            audioManager.StopAndReleaseEventInstance(ref _activeInstance, immediate: true);
            return;
        }

        if (_activeInstance.isValid())
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

        _audioManager = FindAnyObjectByType<AudioManager>();
        audioManager = _audioManager;
        return audioManager != null;
    }

    private Transform GetAnchorTransform()
    {
        return audioAnchor != null ? audioAnchor : transform;
    }

    private float GetCurrentIntensity()
    {
        return targetAttractor != null ? targetAttractor.intensity : 0f;
    }

    private float GetCurrentDangerLevel()
    {
        return targetAttractorAI != null ? targetAttractorAI.currentDangerLevel : 0f;
    }

    private int GetCurrentStateValue()
    {
        return targetAttractorAI != null ? (int)targetAttractorAI.GetCurrentState() : 0;
    }
}
