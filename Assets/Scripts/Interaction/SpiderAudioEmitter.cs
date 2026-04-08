using System;
using FMOD.Studio;
using FMODUnity;
using Managers;
using UnityEngine;

[DisallowMultipleComponent]
public class SpiderAudioEmitter : MonoBehaviour
{
    private const string DebugSpiderRootName = "CrawlyHeadSpider1";
    private const float DebugLogIntervalSeconds = 0.5f;
    private const float MovementSpeedThreshold = 0.05f;

    [Header("Targets")]
    [SerializeField] private Transform audioAnchor;
    [SerializeField] private Attractor targetAttractor;
    [SerializeField] private AttractorAI targetAttractorAI;

    private EventInstance _activeInstance;
    private AudioManager _audioManager;
    private float _nextDebugLogTime;
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
        LogDebugState("Configure");
    }

    private void OnEnable()
    {
        ResetMovementTracking();
    }

    private void Update()
    {
        if (!TryGetAudioManager(out AudioManager audioManager))
        {
            LogDebugState("Update: no AudioManager");
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
                LogDebugState("Update: stopped while stationary");
            }
            else
            {
                LogDebugState("Update: stationary");
            }

            return;
        }

        if (!_activeInstance.isValid())
        {
            TryStartPlayback(audioManager);
        }

        if (!_activeInstance.isValid())
        {
            LogDebugState("Update: moving but instance invalid");
            return;
        }

        audioManager.UpdateSpiderLoop(
            _activeInstance,
            anchor,
            GetCurrentIntensity(),
            GetCurrentDangerLevel(),
            GetCurrentStateValue());

        LogDebugState("Update: moving");
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
            LogDebugState("TryStartPlayback: TryStartSpiderLoop returned false");
            return;
        }

        _activeInstance = instance;
        LogDebugState("TryStartPlayback: started");
    }

    private void StopPlaybackImmediate()
    {
        AudioManager audioManager = UnityEngine.Object.FindAnyObjectByType<AudioManager>();
        if (audioManager != null)
        {
            audioManager.StopAndReleaseEventInstance(ref _activeInstance, immediate: true);
            LogDebugState("StopPlaybackImmediate: stopped via AudioManager");
            return;
        }

        if (_activeInstance.isValid())
        {
            _activeInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            _activeInstance.release();
            _activeInstance = default;
            LogDebugState("StopPlaybackImmediate: stopped locally");
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

    private void LogDebugState(string phase)
    {
        if (!TryGetDebugSpiderRoot(out Transform spiderRoot))
        {
            return;
        }

        bool isUpdatePhase = phase.StartsWith("Update", StringComparison.Ordinal);
        if (isUpdatePhase && Time.unscaledTime < _nextDebugLogTime)
        {
            return;
        }

        if (isUpdatePhase)
        {
            _nextDebugLogTime = Time.unscaledTime + DebugLogIntervalSeconds;
        }

        Transform anchor = GetAnchorTransform();
        string listenerText;
        if (TryGetListenerTransform(out Transform listenerTransform))
        {
            float listenerDistance = Vector3.Distance(listenerTransform.position, anchor.position);
            listenerText = $"listenerPos={FormatVector(listenerTransform.position)}, listenerDistance={listenerDistance:F2}";
        }
        else
        {
            listenerText = "listenerPos=<none>, listenerDistance=<none>";
        }

        string attractorText = targetAttractor == null
            ? "attractor=<none>"
            : $"attractor={targetAttractor.name}, intensityRaw={targetAttractor.intensity:0.00}";

        string aiText = targetAttractorAI == null
            ? "attractorAI=<none>"
            : $"attractorAI={targetAttractorAI.name}, dangerRaw={targetAttractorAI.currentDangerLevel:0.00}, state={targetAttractorAI.GetCurrentState()}({GetCurrentStateValue()})";

        Debug.Log(
            $"[SpiderAudioDebug] phase={phase}, spiderRootPos={FormatVector(spiderRoot.position)}, instanceValid={_activeInstance.isValid()}, movementSpeed={_currentMovementSpeed:0.000}, movementThreshold={MovementSpeedThreshold:0.000}, {listenerText}, {attractorText}, {aiText}");
    }

    private bool TryGetDebugSpiderRoot(out Transform spiderRoot)
    {
        if (targetAttractorAI != null
            && string.Equals(targetAttractorAI.name, DebugSpiderRootName, StringComparison.Ordinal))
        {
            spiderRoot = targetAttractorAI.transform;
            return true;
        }

        Transform current = transform;
        while (current != null)
        {
            if (string.Equals(current.name, DebugSpiderRootName, StringComparison.Ordinal))
            {
                spiderRoot = current;
                return true;
            }

            current = current.parent;
        }

        spiderRoot = null;
        return false;
    }

    private static bool TryGetListenerTransform(out Transform listenerTransform)
    {
        listenerTransform = null;

        StudioListener studioListener = UnityEngine.Object.FindAnyObjectByType<StudioListener>();
        if (studioListener != null)
        {
            listenerTransform = studioListener.AttenuationObject != null
                ? studioListener.AttenuationObject.transform
                : studioListener.transform;
            return true;
        }

        if (Camera.main != null)
        {
            listenerTransform = Camera.main.transform;
            return true;
        }

        return false;
    }

    private static string FormatVector(Vector3 value)
    {
        return $"({value.x:F2}, {value.y:F2}, {value.z:F2})";
    }
}
