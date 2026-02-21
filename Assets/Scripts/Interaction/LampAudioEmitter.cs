using FMOD.Studio;
using Managers;
using UnityEngine;

[DisallowMultipleComponent]
public class LampAudioEmitter : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private Light targetLight;

    [Header("Behavior")]
    [SerializeField] private bool playBuzzOnLightOff = false;

    private EventInstance _humLoopInstance;
    private bool _hasInitializedState;
    private bool _previousIsOn;

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
        if (!_humLoopInstance.isValid())
        {
            return;
        }

        Transform emitterTransform = GetEmitterTransform();
        audioManager.UpdateEventInstanceTransform(_humLoopInstance, emitterTransform);

        bool isOn = IsLampOn();
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
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopAndReleaseEventInstance(ref _humLoopInstance, immediate: true);
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
}
