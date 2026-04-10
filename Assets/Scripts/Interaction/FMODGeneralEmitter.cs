using System;
using FMOD.Studio;
using FMODUnity;
using Managers;
using UnityEngine;

[DisallowMultipleComponent]
public class FMODGeneralEmitter : MonoBehaviour
{
    [Serializable]
    private struct FloatParameterValue
    {
        public string parameterName;
        public float value;
        public bool isGlobal;
    }

    [Header("FMOD")]
    [SerializeField] private EventReference eventRef;
    [SerializeField] private Transform audioAnchor;

    [Header("Playback")]
    [SerializeField] private bool playOnEnable = true;
    [SerializeField] private bool stopOnDisable = true;
    [SerializeField] private bool followAnchor = true;
    [SerializeField] private bool restartIfAlreadyPlaying;
    [SerializeField] private bool randomizeTimelinePositionOnStart;

    [Header("Initial Parameters")]
    [SerializeField] private FloatParameterValue[] initialParameters;

    private EventInstance _activeInstance;
    private AudioManager _audioManager;

    public bool IsPlaying => _activeInstance.isValid();

    public void Configure(EventReference assignedEvent)
    {
        eventRef = assignedEvent;

        if (isActiveAndEnabled && playOnEnable)
        {
            Restart();
        }
    }

    public void Play()
    {
        if (_activeInstance.isValid())
        {
            if (!restartIfAlreadyPlaying)
            {
                ApplyInitialParameters();
                return;
            }

            Stop(immediate: true);
        }

        if (eventRef.IsNull || !TryGetAudioManager(out AudioManager audioManager))
        {
            return;
        }

        Vector3 startPosition = GetAnchorPosition();
        bool started = randomizeTimelinePositionOnStart
            ? audioManager.TryStartSfxEventInstance(eventRef, startPosition, randomizeTimelinePosition: true, out _activeInstance)
            : audioManager.TryStartSfxEventInstance(eventRef, startPosition, out _activeInstance);

        if (!started)
        {
            _activeInstance = default;
            return;
        }

        ApplyInitialParameters();
    }

    public void Stop(bool immediate = false)
    {
        if (TryGetAudioManager(out AudioManager audioManager))
        {
            audioManager.StopAndReleaseEventInstance(ref _activeInstance, immediate);
            return;
        }

        if (_activeInstance.isValid())
        {
            _activeInstance.stop(immediate ? FMOD.Studio.STOP_MODE.IMMEDIATE : FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            _activeInstance.release();
            _activeInstance = default;
        }
    }

    public void Restart()
    {
        Stop(immediate: true);
        Play();
    }

    public void SetParameter(string parameterName, float value, bool isGlobal = false)
    {
        if (string.IsNullOrWhiteSpace(parameterName))
        {
            return;
        }

        if (isGlobal)
        {
            RuntimeManager.StudioSystem.setParameterByName(parameterName, value);
            return;
        }

        if (_activeInstance.isValid())
        {
            _activeInstance.setParameterByName(parameterName, value);
        }
    }

    public void SetParameterWithLabel(string parameterName, string label)
    {
        if (string.IsNullOrWhiteSpace(parameterName) || string.IsNullOrWhiteSpace(label) || !_activeInstance.isValid())
        {
            return;
        }

        _activeInstance.setParameterByNameWithLabel(parameterName, label);
    }

    private void OnEnable()
    {
        if (playOnEnable)
        {
            Play();
        }
    }

    private void Update()
    {
        if (!followAnchor || !_activeInstance.isValid())
        {
            return;
        }

        if (TryGetAudioManager(out AudioManager audioManager))
        {
            audioManager.UpdateEventInstancePosition(_activeInstance, GetAnchorPosition());
        }
    }

    private void OnDisable()
    {
        if (stopOnDisable)
        {
            Stop(immediate: true);
        }
    }

    private void OnDestroy()
    {
        Stop(immediate: true);
    }

    private void ApplyInitialParameters()
    {
        if (!_activeInstance.isValid() || initialParameters == null)
        {
            return;
        }

        foreach (FloatParameterValue parameter in initialParameters)
        {
            if (string.IsNullOrWhiteSpace(parameter.parameterName))
            {
                continue;
            }

            SetParameter(parameter.parameterName, parameter.value, parameter.isGlobal);
        }
    }

    private Vector3 GetAnchorPosition()
    {
        return audioAnchor != null ? audioAnchor.position : transform.position;
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
}
