using System;
using FMOD.Studio;
using FMODUnity;
using Managers;
using UnityEngine;

[DisallowMultipleComponent]
public class LampAudioEmitter : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private Light targetLight;

    [Header("Behavior")]
    [SerializeField] private bool playBuzzOnLightOff = false;

    private const string DebugLampSceneName = "Tutorial";
    private const string DebugLampObjectName = "P_Lamp";
    private const float DebugLogIntervalSeconds = 0.5f;

    private EventInstance _humLoopInstance;
    private bool _hasInitializedState;
    private bool _previousIsOn;
    private float _nextDebugLogTime;

    public void Configure(Light lightTarget, bool enableBuzzOnLightOff)
    {
        targetLight = lightTarget;
        playBuzzOnLightOff = enableBuzzOnLightOff;
    }

    private void Awake()
    {
        if (targetLight == null)
        {
            targetLight = GetComponentInChildren<Light>(true);
        }
    }

    private void OnEnable()
    {
        TryStartHumLoop();
    }

    private void Update()
    {
        AudioManager audioManager = AudioManager.Instance;
        if (audioManager == null)
        {
            return;
        }

        TryStartHumLoop();
        Transform emitterTransform = GetEmitterTransform();
        bool isOn = IsLampOn();
        bool hasValidHumLoop = _humLoopInstance.isValid();
        LogDebugState(audioManager, emitterTransform, isOn, hasValidHumLoop);

        if (!hasValidHumLoop)
        {
            return;
        }

        audioManager.UpdateEventInstanceTransform(_humLoopInstance, emitterTransform);

        if (!_hasInitializedState)
        {
            _previousIsOn = isOn;
            _hasInitializedState = true;
            audioManager.SetLampHumLoopEnabled(_humLoopInstance, isOn);
            return;
        }

        if (isOn == _previousIsOn)
        {
            return;
        }

        audioManager.SetLampHumLoopEnabled(_humLoopInstance, isOn);

        if (playBuzzOnLightOff && _previousIsOn && !isOn)
        {
            audioManager.PlayLampBuzzOff(emitterTransform);
        }

        _previousIsOn = isOn;
    }

    private void OnDisable()
    {
        StopHumLoopImmediate();
    }

    private void OnDestroy()
    {
        StopHumLoopImmediate();
    }

    private void TryStartHumLoop()
    {
        if (_humLoopInstance.isValid())
        {
            return;
        }

        AudioManager audioManager = AudioManager.Instance;
        if (audioManager == null)
        {
            return;
        }

        bool isOn = IsLampOn();
        if (!audioManager.TryStartLampHumLoop(GetEmitterTransform(), isOn, out EventInstance instance))
        {
            return;
        }

        _humLoopInstance = instance;
        _previousIsOn = isOn;
        _hasInitializedState = true;
    }

    private void StopHumLoopImmediate()
    {
        // Avoid Singleton.Instance here: during scene teardown AudioManager may already be destroyed.
        AudioManager audioManager = UnityEngine.Object.FindAnyObjectByType<AudioManager>();
        if (audioManager != null)
        {
            audioManager.StopAndReleaseEventInstance(ref _humLoopInstance, immediate: true);
        }
        else if (_humLoopInstance.isValid())
        {
            _humLoopInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            _humLoopInstance.release();
            _humLoopInstance = default;
        }

        _hasInitializedState = false;
    }

    private bool IsLampOn()
    {
        return targetLight != null && targetLight.enabled && targetLight.gameObject.activeInHierarchy;
    }

    private Transform GetEmitterTransform()
    {
        return targetLight != null ? targetLight.transform : transform;
    }

    private void LogDebugState(AudioManager audioManager, Transform emitterTransform, bool isOn, bool hasValidHumLoop)
    {
        if (!ShouldLogDebugState())
        {
            return;
        }

        if (Time.unscaledTime < _nextDebugLogTime)
        {
            return;
        }

        _nextDebugLogTime = Time.unscaledTime + DebugLogIntervalSeconds;

        Animator animator = GetComponent<Animator>();
        if (animator == null && targetLight != null)
        {
            animator = targetLight.GetComponentInParent<Animator>();
        }

        string listenerText;
        if (TryGetListenerTransform(out Transform listenerTransform))
        {
            float listenerDistance = Vector3.Distance(listenerTransform.position, emitterTransform.position);
            listenerText =
                $"listenerPos={FormatVector(listenerTransform.position)}, listenerDistance={listenerDistance:F2}";
        }
        else
        {
            listenerText = "listenerPos=<none>, listenerDistance=<none>";
        }

        string animatorText = animator == null
            ? "animator=<none>"
            : $"animator={animator.name}, animatorEnabled={animator.enabled}, animatorCulling={animator.cullingMode}";

        string lightText = targetLight == null
            ? "light=<none>"
            : $"lightName={targetLight.name}, lightEnabled={targetLight.enabled}, lightActive={targetLight.gameObject.activeInHierarchy}, lightIntensity={targetLight.intensity:F2}";

        Debug.Log(
            $"[LampDebug] scene={gameObject.scene.name}, object={name}, rootPos={FormatVector(transform.position)}, emitterPos={FormatVector(emitterTransform.position)}, isOn={isOn}, humLoopValid={hasValidHumLoop}, muteSfx={audioManager.muteSFX}, {listenerText}, {lightText}, {animatorText}");
    }

    private bool ShouldLogDebugState()
    {
        return string.Equals(gameObject.scene.name, DebugLampSceneName, StringComparison.Ordinal)
               && string.Equals(name, DebugLampObjectName, StringComparison.Ordinal);
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
