using System;
using FMOD.Studio;
using FMODUnity;
using Managers;
using UnityEngine;

[DisallowMultipleComponent]
public class SpiderAudioEmitter : MonoBehaviour
{
    private const float MovementSpeedThreshold = 0.05f;

    [Header("Targets")]
    [SerializeField] private Transform audioAnchor;
    [SerializeField] private Attractor targetAttractor;
    [SerializeField] private AttractorAI targetAttractorAI;

    private EventInstance _activeInstance;
    private AudioManager _audioManager;
    private Vector3 _lastAnchorPosition;
    private bool _hasLastAnchorPosition;
    private float _currentMovementSpeed;

    public void Configure(Transform anchor, Attractor attractor, AttractorAI attractorAI)
    {
        audioAnchor = anchor != null ? anchor : transform;
        targetAttractor = attractor;
        targetAttractorAI = attractorAI;
        ResetMovementTracking();
        StopPlaybackImmediate();
    }

    private void OnEnable()
    {
        ResetMovementTracking();
    }

    private void Update()
    {
        if (!TryGetAudioManager(out AudioManager audioManager))
        {
            return;
        }

        Transform anchor = GetAnchorTransform();
        _currentMovementSpeed = SampleMovementSpeed(anchor.position);
        bool shouldPlay = _currentMovementSpeed > MovementSpeedThreshold;

        if (!shouldPlay)
        {
            if (_activeInstance.isValid())
            {
                audioManager.StopAndReleaseEventInstance(ref _activeInstance);
            }

            return;
        }

        if (!_activeInstance.isValid())
        {
            TryStartPlayback(audioManager);
        }

        if (!_activeInstance.isValid())
        {
            return;
        }

        audioManager.UpdateSpiderLoop(
            _activeInstance,
            anchor,
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

    private void TryStartPlayback(AudioManager audioManager)
    {
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
        AudioManager audioManager = UnityEngine.Object.FindAnyObjectByType<AudioManager>();
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

        _audioManager = UnityEngine.Object.FindAnyObjectByType<AudioManager>();
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

    private void ResetMovementTracking()
    {
        _hasLastAnchorPosition = false;
        _currentMovementSpeed = 0f;
    }

    private float SampleMovementSpeed(Vector3 currentAnchorPosition)
    {
        if (!_hasLastAnchorPosition)
        {
            _lastAnchorPosition = currentAnchorPosition;
            _hasLastAnchorPosition = true;
            return 0f;
        }

        float deltaTime = Mathf.Max(0.0001f, Time.deltaTime);
        float speed = Vector3.Distance(currentAnchorPosition, _lastAnchorPosition) / deltaTime;
        _lastAnchorPosition = currentAnchorPosition;
        return speed;
    }
}
