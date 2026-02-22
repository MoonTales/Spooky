using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using FMODUnity;
using FMOD.Studio;
using Types = System.Types;

// TODO: Reorganize inspector organization
namespace Managers
{
    public class AudioManager : Singleton<AudioManager>
    {
        // IDs for unparameterized SFX event mappings.
        public enum SfxId
        {
            // Player
            Jump, Landing, Flashlight, // CrouchIn, CrouchOut, PeekIn, PeekOut, TippytoeIn, TippytoeOut,
            // Interaction
            LetterSlide, LetterScribble,
            AlarmGood, AlarmBad,
            DoorLocked,
            // UI
            UIHover,
        }

        // Inspector entry mapping SfxId -> FMOD event.
        [Serializable]
        public struct SfxEntry
        {
            public SfxId id;
            public EventReference eventRef;
        }

        // Runtime state
        [SerializeField] private SfxEntry[] sfxEvents;      // Inspector-assigned map of SfxId -> FMOD EventReference.
        private Dictionary<SfxId, EventReference> _sfxMap;  // Runtime lookup built from sfxEvents for fast access.
        private Bus _playerMovementBus;
        private float _mentalStateSeverity;
        private float _terrorSeverity;

        private EventInstance _nightmareAmbienceInstance;
        private EventInstance _mainMenuMusicInstance;
        private EventInstance _bedroomAmbienceInstance;
        private EventInstance _heartbeatInstance;
        private EventInstance _terrorLoopInstance;
        private EventInstance _sleepTrackerAlarmInstance;
        private EventInstance _uiHoverInstance;
        private EventInstance _letterScribbleInstance;
        private bool _terrorLoopIsPlaying;
        private bool _sleepTrackerAlarmIsGoodVariant;
        private bool _hasSleepTrackerAlarmVariant;
        private Transform _terrorSourceTransform;
        private EventInstance _pauseSnapshotInstance;
        private bool _pauseSnapshotActive;

        // Per-call parameter payload for FMOD events.
        public readonly struct SfxParam
        {
            public readonly string name;
            public readonly float value;

            public SfxParam(string name, float value)
            {
                this.name = name;
                this.value = value;
            }

            public static SfxParam Bool(string name, bool enabled)
            {
                return new SfxParam(name, enabled ? 1f : 0f);
            }

            public static SfxParam Int(string name, int intValue)
            {
                return new SfxParam(name, intValue);
            }

            public static SfxParam Float(string name, float floatValue)
            {
                return new SfxParam(name, floatValue);
            }
        }

        [Header("Debug")]
        [SerializeField] private bool debugAudioLogs = false;

        // Serialized FMOD event references and parameters.
        [Header("Player Sounds")]
        [SerializeField] private EventReference footstepPlayer;     // Parameterized footstep event with Surface label parameter.
        [SerializeField] private string playerMovementBusPath = "bus:/SFX/Player/Movement"; // Bus containing player movement events for quick stops.

        [Header("Environment Sounds")]
        [SerializeField] private bool autoAttachLampAudioOnSceneLoad = true;
        [SerializeField] private string lampAudioAutoAttachSceneName = "Tutorial";
        [SerializeField] private EventReference lampHumLoopEvent;
        [SerializeField] private string lampOnParameter = "LampOn";
        [SerializeField] private EventReference lampBuzzOffEvent;

        [Header("Mental Audio")]
        [SerializeField] private string terrorDistortionParameter = "Terror";
        [SerializeField] private string mentalHealthDistortionParameter = "MentalHealth";
        [SerializeField] private bool terrorParameterIsGlobal = true;
        [SerializeField] private bool mentalHealthParameterIsGlobal = true;
        [SerializeField] private EventReference terrorLoopEvent;
        [SerializeField] private EventReference nightmareAmbLoopEvent;
        [SerializeField] private EventReference heartbeatLoopEvent;
        [SerializeField] private string heartbeatTerrorParameter = "Terror";
        [SerializeField] private string heartbeatMentalHealthParameter = "MentalHealth";
        [SerializeField] private bool heartbeatTerrorParameterIsGlobal = true;
        [SerializeField] private bool heartbeatMentalHealthParameterIsGlobal = true;
        [SerializeField] private bool logTerrorParameterValue = false;

        [Header("Sleep Tracker Audio")]
        [SerializeField] private string sleepTrackerActiveParameter = "SleepTracker";

        [Header("World Ambience")]
        [SerializeField] private EventReference bedroomAmbLoopEvent;
        [SerializeField] private bool bedroomAmbienceRequiresGameplay = true;

        [Header("Settings Menu Audio")]
        [SerializeField] private EventReference mainMenuMusicEvent;
        [SerializeField] private string masterBusPath = "bus:/";
        [SerializeField] private string sfxBusPath = "bus:/SFX";
        [SerializeField] private string musicBusPath = "bus:/Music";
        [SerializeField] private string ambienceBusPath = "bus:/Ambience";
        [SerializeField] private EventReference pauseSnapshotEvent;

        private Bus _masterBus;
        private Bus _sfxBus;
        private Bus _musicBus;
        private Bus _ambienceBus;
        
        [Header("Mutes")]
        public bool muteSFX = false;
        public bool muteMusic = false;
        
        // Lifecycle
        protected override void RegisterSubscriptions()
        {
            base.RegisterSubscriptions();

            // Mental-state driven audio parameters.
            TrackSubscription(
                () => EventBroadcaster.OnPlayerHealthStateChanged += OnPlayerMentalStateChanged,
                () => EventBroadcaster.OnPlayerHealthStateChanged -= OnPlayerMentalStateChanged);
            TrackSubscription(
                () => EventBroadcaster.OnTerrorIntensityChanged += OnTerrorIntensityChanged,
                () => EventBroadcaster.OnTerrorIntensityChanged -= OnTerrorIntensityChanged);

            // Sleep tracker alarm state changes (active + good/bad variant).
            TrackSubscription(
                () => EventBroadcaster.OnSleepTrackerAudioStateChanged += OnSleepTrackerAudioStateChanged,
                () => EventBroadcaster.OnSleepTrackerAudioStateChanged -= OnSleepTrackerAudioStateChanged);

            // Scene/world transitions that affect persistent loops.
            TrackSubscription(
                () => EventBroadcaster.OnWorldLocationChangedEvent += OnWorldLocationChanged,
                () => EventBroadcaster.OnWorldLocationChangedEvent -= OnWorldLocationChanged);
            TrackSubscription(
                () => SceneManager.sceneLoaded += OnSceneLoaded,
                () => SceneManager.sceneLoaded -= OnSceneLoaded);
        }

        protected override void Awake()
        {
            base.Awake();

            BuildSfxMap();
            CachePlayerMovementBus();
            CacheSettingsBuses();
            AutoAttachLampAudioEmittersInScene(SceneManager.GetActiveScene());
            ApplyBedroomAmbience();
        }

        protected override void OnDestroy()
        {
            StopAndReleaseUiHover();
            StopAndReleaseHeartbeat();
            StopAndReleaseTerrorLoop();
            StopAndReleaseNightmareAmbience();
            StopAndReleaseSleepTrackerAlarm(true);
            StopAndReleaseBedroomAmbience();
            StopMainMenuMusic(true);
            SetPauseSnapshotEnabled(false);
            base.OnDestroy();
        }
        
        // Event handlers
        private void OnPlayerMentalStateChanged(Types.PlayerMentalState newMentalState)
        {
            _mentalStateSeverity = GetMentalStateSeverity(newMentalState);
            LogAudioState($"Mental state changed -> {newMentalState} ({_mentalStateSeverity:0.00}). Expected: nightmare ambience/heartbeat parameters update.");
            RefreshMentalAudio();
        }

        private void OnTerrorIntensityChanged(float normalizedIntensity, Transform terrorSourceTransform)
        {
            if (!IsNightmareWorldLocation())
            {
                _terrorSeverity = 0f;
                _terrorSourceTransform = null;
                RefreshMentalAudio();
                return;
            }

            _terrorSeverity = Mathf.Clamp01(normalizedIntensity);
            _terrorSourceTransform = terrorSourceTransform;
            if (debugAudioLogs && logTerrorParameterValue)
            {
                Debug.Log($"AudioManager: Terror param value = {_terrorSeverity:0.000}");
            }
            LogAudioState($"Terror changed -> {_terrorSeverity:0.00} (source={(terrorSourceTransform != null ? terrorSourceTransform.name : "null")}). Expected: terror loop follows source in Nightmare.");
            RefreshMentalAudio();
        }

        private void OnSleepTrackerAudioStateChanged(bool isActive, bool isGoodWakeup, Transform sourceTransform)
        {
            Debug.Log($"AudioManager: SleepTracker broadcast received | active={isActive}, goodWakeup={isGoodWakeup}, source={(sourceTransform != null ? sourceTransform.name : "null")}");
            ApplySleepTrackerAlarmState(isActive, isGoodWakeup, sourceTransform);
        }

        private void OnWorldLocationChanged(Types.WorldLocation newLocation)
        {
            LogAudioState($"World location changed -> {newLocation}. Expected: nightmare stack in Nightmare, bedroom ambience in Bedroom.");
            if (newLocation != Types.WorldLocation.Nightmare)
            {
                _terrorSeverity = 0f;
                _terrorSourceTransform = null;
            }

            if (newLocation != Types.WorldLocation.Bedroom)
            {
                StopAndReleaseSleepTrackerAlarm(true);
            }
            RefreshMentalAudio();
            ApplyBedroomAmbience();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            AutoAttachLampAudioEmittersInScene(scene);
            ApplyBedroomAmbience();
        }

        protected override void OnGameStateChanged(Types.GameState newState)
        {
            base.OnGameStateChanged(newState);
            LogAudioState($"Game state changed -> {newState}. Expected: pause snapshot {(newState == Types.GameState.Paused ? "enabled" : "disabled")}.");
            SetPauseSnapshotEnabled(newState == Types.GameState.Paused);

            if (newState == Types.GameState.MainMenu)
            {
                PlayMainMenuMusicIfNeeded();
                StopAndReleaseSleepTrackerAlarm(true);
            }
            else
            {
                StopMainMenuMusic(true);
            }

            ApplyBedroomAmbience();
        }

        // Public API: gameplay-triggered SFX
        public void PlayFootstep(string surfaceLabel, Transform fromTransform = null)
        {
            if (muteSFX) return;
            if (footstepPlayer.IsNull) return;

            // Use a labeled parameter to select the correct surface variation.
            EventInstance instance = CreateEventInstance(footstepPlayer, fromTransform);
            instance.setParameterByNameWithLabel("Surface", surfaceLabel);
            instance.start();
            instance.release();
        }

        public bool TryStartLampHumLoop(Transform fromTransform, bool isOn, out EventInstance instance)
        {
            instance = default;

            if (muteSFX || lampHumLoopEvent.IsNull)
            {
                return false;
            }

            instance = CreateEventInstance(lampHumLoopEvent, fromTransform);
            SetLampHumLoopEnabled(instance, isOn);
            instance.start();
            return true;
        }

        public void SetLampHumLoopEnabled(EventInstance instance, bool isOn)
        {
            if (!instance.isValid() || string.IsNullOrWhiteSpace(lampOnParameter))
            {
                return;
            }

            instance.setParameterByName(lampOnParameter, isOn ? 1f : 0f);
        }

        public void UpdateEventInstanceTransform(EventInstance instance, Transform fromTransform)
        {
            if (!instance.isValid() || fromTransform == null)
            {
                return;
            }

            instance.set3DAttributes(RuntimeUtils.To3DAttributes(fromTransform.position));
        }

        public void UpdateEventInstancePosition(EventInstance instance, Vector3 worldPosition)
        {
            if (!instance.isValid())
            {
                return;
            }

            instance.set3DAttributes(RuntimeUtils.To3DAttributes(worldPosition));
        }

        public bool TryStartSfxEventInstance(EventReference eventReference, Vector3 worldPosition, out EventInstance instance)
        {
            instance = default;

            if (muteSFX || eventReference.IsNull)
            {
                return false;
            }

            instance = RuntimeManager.CreateInstance(eventReference);
            if (EventInstanceIs3D(instance))
            {
                instance.set3DAttributes(RuntimeUtils.To3DAttributes(worldPosition));
            }

            instance.start();
            return true;
        }

        public void StopAndReleaseEventInstance(ref EventInstance instance, bool immediate = false)
        {
            if (!instance.isValid())
            {
                return;
            }

            instance.stop(immediate ? FMOD.Studio.STOP_MODE.IMMEDIATE : FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            instance.release();
            instance = default;
        }

        public void PlayLampBuzzOff(Transform fromTransform = null)
        {
            if (muteSFX || lampBuzzOffEvent.IsNull)
            {
                return;
            }

            PlayEvent(lampBuzzOffEvent, fromTransform);
        }

        public void StopFootstepsImmediate()
        {
            if (_playerMovementBus.isValid())
            {
                _playerMovementBus.stopAllEvents(FMOD.Studio.STOP_MODE.IMMEDIATE);
            }
        }

        public void SetSfxVolume(float normalizedVolume)
        {
            float clamped = Mathf.Clamp01(normalizedVolume);
            if (_sfxBus.isValid())
            {
                _sfxBus.setVolume(clamped);
            }
            muteSFX = clamped <= 0.0001f;
        }

        public void SetMasterVolume(float normalizedVolume)
        {
            float clamped = Mathf.Clamp01(normalizedVolume);
            if (_masterBus.isValid())
            {
                _masterBus.setVolume(clamped);
            }
        }

        public void SetMusicVolume(float normalizedVolume)
        {
            float clamped = Mathf.Clamp01(normalizedVolume);
            if (_musicBus.isValid())
            {
                _musicBus.setVolume(clamped);
            }
            muteMusic = clamped <= 0.0001f;
        }

        public void SetAmbienceVolume(float normalizedVolume)
        {
            float clamped = Mathf.Clamp01(normalizedVolume);
            if (_ambienceBus.isValid())
            {
                _ambienceBus.setVolume(clamped);
            }
        }

        public void SetSfxMuted(bool isMuted)
        {
            muteSFX = isMuted;
            if (_sfxBus.isValid())
            {
                _sfxBus.setMute(isMuted);
            }
        }

        public void SetMusicMuted(bool isMuted)
        {
            muteMusic = isMuted;
            if (_musicBus.isValid())
            {
                _musicBus.setMute(isMuted);
            }
        }

        public void SetPauseSnapshotEnabled(bool enabled)
        {
            if (pauseSnapshotEvent.IsNull)
            {
                LogAudioState("Pause snapshot not started: pause snapshot event is null.");
                return;
            }

            if (enabled)
            {
                if (_pauseSnapshotActive)
                {
                    LogAudioState("Pause snapshot already active.");
                    return;
                }

                if (!_pauseSnapshotInstance.isValid())
                {
                    _pauseSnapshotInstance = RuntimeManager.CreateInstance(pauseSnapshotEvent);
                }
                _pauseSnapshotInstance.start();
                _pauseSnapshotActive = true;
                LogAudioState("Pause snapshot started. Expected: paused mix behavior now active.");
                return;
            }

            if (_pauseSnapshotInstance.isValid())
            {
                _pauseSnapshotInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                _pauseSnapshotInstance.release();
                LogAudioState("Pause snapshot stopped.");
            }
            _pauseSnapshotActive = false;
        }

        public void PlayParamSfx(SfxId sfxId, Transform fromTransform = null, params SfxParam[] parameters)
        {
            if (muteSFX) return;

            // Parameterized play path for events that need per-call data.
            EventReference eventReference = GetSfxEvent(sfxId);
            if (eventReference.IsNull)
            {
                return;
            }

            EventInstance instance = CreateEventInstance(eventReference, fromTransform);
            if (parameters != null)
            {
                for (int i = 0; i < parameters.Length; i++)
                {
                    instance.setParameterByName(parameters[i].name, parameters[i].value);
                }
            }
            instance.start();
            instance.release();
        }
        
        public void PlaySfx(SfxId sfxId, Transform fromTransform = null)
        {
            if (muteSFX) return;

            // Unparameterized SFX play path using the SfxId mapping.
            EventReference eventReference = GetSfxEvent(sfxId);
            if (!eventReference.IsNull)
            {
                PlayEvent(eventReference, fromTransform);
            }
            else
            {
                Debug.LogWarning($"AudioManager: Missing FMOD EventReference for SfxId '{sfxId}'.");
            }
        }

        public void PlayUiHoverSfx()
        {
            if (muteSFX)
            {
                return;
            }

            EventReference eventReference = GetSfxEvent(SfxId.UIHover);
            if (eventReference.IsNull)
            {
                Debug.LogWarning("AudioManager: Missing FMOD EventReference for SfxId 'UIHover'.");
                return;
            }

            if (!_uiHoverInstance.isValid())
            {
                _uiHoverInstance = CreateEventInstance(eventReference);
            }
            else
            {
                _uiHoverInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            }

            _uiHoverInstance.start();
        }
        
        public void PlayPlayerJumping(float volume = 1, float deviation = 0.2f, Transform fromTransform = null)
        {
            StopFootstepsImmediate();
            PlaySfx(SfxId.Jump, fromTransform);
        }

        private void ApplySleepTrackerAlarmState(bool isActive, bool isGoodWakeup, Transform sourceTransform)
        {
            if (GameStateManager.Instance == null
                || GameStateManager.Instance.GetCurrentWorldLocation() != Types.WorldLocation.Bedroom)
            {
                StopAndReleaseSleepTrackerAlarm(true);
                return;
            }

            EventReference alarmEvent = GetSfxEvent(isGoodWakeup ? SfxId.AlarmGood : SfxId.AlarmBad);
            if (alarmEvent.IsNull)
            {
                Debug.LogWarning($"AudioManager: Missing FMOD EventReference for SfxId '{(isGoodWakeup ? SfxId.AlarmGood : SfxId.AlarmBad)}'.");
                return;
            }

            bool variantChanged = !_hasSleepTrackerAlarmVariant || _sleepTrackerAlarmIsGoodVariant != isGoodWakeup;
            if (variantChanged || !_sleepTrackerAlarmInstance.isValid())
            {
                StopAndReleaseSleepTrackerAlarm(true);
                _sleepTrackerAlarmInstance = CreateEventInstance(alarmEvent, sourceTransform);
                _sleepTrackerAlarmInstance.start();
                _sleepTrackerAlarmIsGoodVariant = isGoodWakeup;
                _hasSleepTrackerAlarmVariant = true;
                LogAudioState($"Sleep tracker alarm started ({(isGoodWakeup ? "good" : "bad")} wakeup).");
            }
            else if (sourceTransform != null && EventInstanceIs3D(_sleepTrackerAlarmInstance))
            {
                _sleepTrackerAlarmInstance.set3DAttributes(RuntimeUtils.To3DAttributes(sourceTransform.position));
            }

            if (_sleepTrackerAlarmInstance.isValid() && !string.IsNullOrWhiteSpace(sleepTrackerActiveParameter))
            {
                _sleepTrackerAlarmInstance.setParameterByName(sleepTrackerActiveParameter, isActive ? 1f : 0f);
            }
        }

        // Mental audio stack
        private void RefreshMentalAudio()
        {
            float terrorSeverityForAudio = IsNightmareWorldLocation() ? _terrorSeverity : 0f;
            ApplyTerrorLoop(terrorSeverityForAudio);
            ApplyNightmareAmbience();
            ApplyHeartbeat();
        }

        private void PlayMainMenuMusicIfNeeded()
        {
            if (mainMenuMusicEvent.IsNull || muteMusic)
            {
                LogAudioState($"Main menu music not started. Reason: {(mainMenuMusicEvent.IsNull ? "event reference missing" : "music is muted")}.");
                return;
            }

            if (_mainMenuMusicInstance.isValid())
            {
                FMOD.RESULT stateResult = _mainMenuMusicInstance.getPlaybackState(out PLAYBACK_STATE playbackState);
                if (stateResult == FMOD.RESULT.OK
                    && (playbackState == PLAYBACK_STATE.PLAYING || playbackState == PLAYBACK_STATE.STARTING))
                {
                    LogAudioState("Main menu music already playing.");
                    return;
                }

                _mainMenuMusicInstance.release();
            }

            _mainMenuMusicInstance = CreateEventInstance(mainMenuMusicEvent);
            _mainMenuMusicInstance.start();
            LogAudioState("Main menu music started.");
        }

        private void ApplyBedroomAmbience()
        {
            if (!ShouldPlayBedroomAmbience())
            {
                StopAndReleaseBedroomAmbience();
                return;
            }

            if (bedroomAmbLoopEvent.IsNull)
            {
                return;
            }

            if (_bedroomAmbienceInstance.isValid())
            {
                return;
            }

            _bedroomAmbienceInstance = CreateEventInstance(bedroomAmbLoopEvent);
            _bedroomAmbienceInstance.start();
            LogAudioState("Bedroom ambience started.");
        }

        private void StopMainMenuMusic(bool allowFadeout)
        {
            if (!_mainMenuMusicInstance.isValid())
            {
                return;
            }

            _mainMenuMusicInstance.stop(allowFadeout ? FMOD.Studio.STOP_MODE.ALLOWFADEOUT : FMOD.Studio.STOP_MODE.IMMEDIATE);
            _mainMenuMusicInstance.release();
        }

        private void ApplyTerrorLoop(float terrorSeverity)
        {
            if (!IsNightmareWorldLocation())
            {
                StopAndReleaseTerrorLoop();
                return;
            }

            if (terrorLoopEvent.IsNull)
            {
                return;
            }

            bool shouldPlay = terrorSeverity > 0.0001f && _terrorSourceTransform != null;
            if (!shouldPlay)
            {
                StopAndReleaseTerrorLoop();
                return;
            }

            if (!_terrorLoopIsPlaying)
            {
                _terrorLoopInstance = CreateEventInstance(terrorLoopEvent, _terrorSourceTransform);
                _terrorLoopInstance.start();
                _terrorLoopIsPlaying = true;
                LogAudioState("Terror loop started. Expected: audible 3D terror source in Nightmare.");
                return;
            }

            if (_terrorLoopInstance.isValid())
            {
                _terrorLoopInstance.set3DAttributes(RuntimeUtils.To3DAttributes(_terrorSourceTransform.position));
            }
        }

        private void ApplyNightmareAmbience()
        {
            if (!IsNightmareWorldLocation())
            {
                StopAndReleaseNightmareAmbience();
                return;
            }

            if (nightmareAmbLoopEvent.IsNull)
            {
                return;
            }

            if (!_nightmareAmbienceInstance.isValid())
            {
                _nightmareAmbienceInstance = CreateEventInstance(nightmareAmbLoopEvent);
                _nightmareAmbienceInstance.start();
                LogAudioState("Nightmare ambience started. Expected: continuous ambience in Nightmare.");
            }

            if (!string.IsNullOrWhiteSpace(terrorDistortionParameter))
            {
                SetFmodParameter(_nightmareAmbienceInstance, terrorDistortionParameter, _terrorSeverity, terrorParameterIsGlobal);
            }

            if (!string.IsNullOrWhiteSpace(mentalHealthDistortionParameter))
            {
                SetFmodParameter(_nightmareAmbienceInstance, mentalHealthDistortionParameter, _mentalStateSeverity, mentalHealthParameterIsGlobal);
            }
        }

        private void ApplyHeartbeat()
        {
            if (!IsNightmareWorldLocation())
            {
                StopAndReleaseHeartbeat();
                return;
            }

            if (heartbeatLoopEvent.IsNull)
            {
                return;
            }

            if (!_heartbeatInstance.isValid())
            {
                _heartbeatInstance = CreateEventInstance(heartbeatLoopEvent);
                _heartbeatInstance.start();
                LogAudioState("Heartbeat loop started. Expected: heartbeat audible in Nightmare.");
            }

            if (!string.IsNullOrWhiteSpace(heartbeatTerrorParameter))
            {
                SetFmodParameter(_heartbeatInstance, heartbeatTerrorParameter, _terrorSeverity, heartbeatTerrorParameterIsGlobal);
            }

            if (!string.IsNullOrWhiteSpace(heartbeatMentalHealthParameter))
            {
                SetFmodParameter(_heartbeatInstance, heartbeatMentalHealthParameter, _mentalStateSeverity, heartbeatMentalHealthParameterIsGlobal);
            }
        }

        // FMOD helpers
        private void SetFmodParameter(EventInstance instance, string parameterName, float value, bool isGlobal)
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

            if (instance.isValid())
            {
                instance.setParameterByName(parameterName, value);
            }
        }

        private float GetMentalStateSeverity(Types.PlayerMentalState mentalState)
        {
            switch (mentalState)
            {
                case Types.PlayerMentalState.Normal:
                    return 0f;
                case Types.PlayerMentalState.MildlyAnxious:
                case Types.PlayerMentalState.MildlySleepDeprived:
                    return 0.25f;
                case Types.PlayerMentalState.ModeratelyAnxious:
                case Types.PlayerMentalState.ModeratelySleepDeprived:
                    return 0.5f;
                case Types.PlayerMentalState.SeverelyAnxious:
                case Types.PlayerMentalState.SeverelySleepDeprived:
                    return 0.75f;
                case Types.PlayerMentalState.Panic:
                case Types.PlayerMentalState.Exhausted:
                case Types.PlayerMentalState.Breakdown:
                    return 1f;
                default:
                    return 0f;
            }
        }

        private static bool IsNightmareWorldLocation()
        {
            return GameStateManager.Instance != null
                && GameStateManager.Instance.GetCurrentWorldLocation() == Types.WorldLocation.Nightmare;
        }

        private bool ShouldPlayBedroomAmbience()
        {
            if (GameStateManager.Instance == null)
            {
                return false;
            }

            if (GameStateManager.Instance.GetCurrentWorldLocation() != Types.WorldLocation.Bedroom)
            {
                return false;
            }

            if (!bedroomAmbienceRequiresGameplay)
            {
                return true;
            }

            return GameStateManager.Instance.GetCurrentGameState() == Types.GameState.Gameplay;
        }

        private void PlayEvent(EventReference eventReference, Transform fromTransform)
        {
            if (muteSFX) return;
            if (eventReference.IsNull) return;

            EventInstance instance = CreateEventInstance(eventReference, fromTransform);
            instance.start();
            instance.release();
        }

        private EventInstance CreateEventInstance(EventReference eventReference, Transform fromTransform = null)
        {
            // Use RuntimeManager to ensure correct FMOD instance tracking and virtualization.
            EventInstance instance = RuntimeManager.CreateInstance(eventReference);

            if (EventInstanceIs3D(instance))
            {
                Vector3 position = fromTransform != null
                    ? fromTransform.position
                    : Camera.main != null
                        ? Camera.main.transform.position
                        : Vector3.zero;

                instance.set3DAttributes(RuntimeUtils.To3DAttributes(position));
            }
            return instance;
        }

        private static bool EventInstanceIs3D(EventInstance instance)
        {
            if (!instance.isValid())
            {
                return false;
            }

            FMOD.RESULT descriptionResult = instance.getDescription(out EventDescription description);
            if (descriptionResult != FMOD.RESULT.OK)
            {
                return false;
            }

            FMOD.RESULT is3DResult = description.is3D(out bool is3D);
            return is3DResult == FMOD.RESULT.OK && is3D;
        }

        // Cleanup and cache
        private void StopAndReleaseHeartbeat()
        {
            if (_heartbeatInstance.isValid())
            {
                _heartbeatInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                _heartbeatInstance.release();
                LogAudioState("Heartbeat loop stopped.");
            }
        }

        private void StopAndReleaseSleepTrackerAlarm(bool immediate)
        {
            if (_sleepTrackerAlarmInstance.isValid())
            {
                _sleepTrackerAlarmInstance.stop(immediate ? FMOD.Studio.STOP_MODE.IMMEDIATE : FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                _sleepTrackerAlarmInstance.release();
                _sleepTrackerAlarmInstance = default;
                LogAudioState("Sleep tracker alarm stopped.");
            }

            _hasSleepTrackerAlarmVariant = false;
        }

        private void StopAndReleaseTerrorLoop()
        {
            if (_terrorLoopInstance.isValid())
            {
                _terrorLoopInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                _terrorLoopInstance.release();
                LogAudioState("Terror loop stopped.");
            }
            _terrorLoopIsPlaying = false;
        }

        private void StopAndReleaseNightmareAmbience()
        {
            if (_nightmareAmbienceInstance.isValid())
            {
                _nightmareAmbienceInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                _nightmareAmbienceInstance.release();
                LogAudioState("Nightmare ambience stopped.");
            }
        }

        private void StopAndReleaseBedroomAmbience()
        {
            if (_bedroomAmbienceInstance.isValid())
            {
                _bedroomAmbienceInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                _bedroomAmbienceInstance.release();
                _bedroomAmbienceInstance = default;
                LogAudioState("Bedroom ambience stopped.");
            }
        }

        private void StopAndReleaseUiHover()
        {
            if (_uiHoverInstance.isValid())
            {
                _uiHoverInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                _uiHoverInstance.release();
                _uiHoverInstance = default;
            }
        }

        private void LogAudioState(string message)
        {
            if (!debugAudioLogs)
            {
                return;
            }

            Debug.Log($"AudioManager: {message}");
        }

        private EventReference GetSfxEvent(SfxId sfxId)
        {
            // Lazy rebuild in case the inspector list changes at runtime.
            if (_sfxMap == null || _sfxMap.Count == 0)
            {
                BuildSfxMap();
            }

            return _sfxMap != null && _sfxMap.TryGetValue(sfxId, out EventReference evt) ? evt : default;
        }

        private void BuildSfxMap()
        {
            // Build the lookup table once from serialized entries.
            _sfxMap = new Dictionary<SfxId, EventReference>();
            if (sfxEvents == null)
            {
                return;
            }

            foreach (var entry in sfxEvents)
            {
                _sfxMap[entry.id] = entry.eventRef;
            }
        }

        private void CachePlayerMovementBus()
        {
            if (string.IsNullOrWhiteSpace(playerMovementBusPath))
            {
                return;
            }

            if (!_playerMovementBus.isValid())
            {
                _playerMovementBus = RuntimeManager.GetBus(playerMovementBusPath);
            }
        }

        private void CacheSettingsBuses()
        {
            if (!_masterBus.isValid() && !string.IsNullOrWhiteSpace(masterBusPath))
            {
                _masterBus = RuntimeManager.GetBus(masterBusPath);
            }

            if (!_sfxBus.isValid() && !string.IsNullOrWhiteSpace(sfxBusPath))
            {
                _sfxBus = RuntimeManager.GetBus(sfxBusPath);
            }

            if (!_musicBus.isValid() && !string.IsNullOrWhiteSpace(musicBusPath))
            {
                _musicBus = RuntimeManager.GetBus(musicBusPath);
            }

            if (!_ambienceBus.isValid() && !string.IsNullOrWhiteSpace(ambienceBusPath))
            {
                _ambienceBus = RuntimeManager.GetBus(ambienceBusPath);
            }
        }

        private void AutoAttachLampAudioEmittersInScene(Scene scene)
        {
            if (!autoAttachLampAudioOnSceneLoad || !scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(lampAudioAutoAttachSceneName)
                && !string.Equals(scene.name, lampAudioAutoAttachSceneName, StringComparison.Ordinal))
            {
                return;
            }

            int attachedCount = 0;
            int configuredCount = 0;
            HashSet<Transform> configuredRoots = new HashSet<Transform>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Light[] lights = roots[i].GetComponentsInChildren<Light>(true);
                for (int j = 0; j < lights.Length; j++)
                {
                    Light light = lights[j];
                    if (!IsLampCandidate(light))
                    {
                        continue;
                    }

                    Transform emitterRoot = GetLampEmitterRoot(light.transform);
                    if (!configuredRoots.Add(emitterRoot))
                    {
                        continue;
                    }

                    LampAudioEmitter emitter = emitterRoot.GetComponent<LampAudioEmitter>();
                    if (emitter == null)
                    {
                        emitter = emitterRoot.gameObject.AddComponent<LampAudioEmitter>();
                        attachedCount++;
                    }

                    bool playBuzzOnLightOff = IsLikelyFlickeringLamp(emitterRoot, light);
                    emitter.Configure(light, playBuzzOnLightOff);
                    configuredCount++;
                }
            }

            LogAudioState($"Lamp audio auto-attach in scene '{scene.name}': configured={configuredCount}, newlyAdded={attachedCount}.");
        }

        private static bool IsLampCandidate(Light light)
        {
            if (light == null)
            {
                return false;
            }

            Transform current = light.transform;
            while (current != null)
            {
                if (current.name.IndexOf("lamp", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
                current = current.parent;
            }

            return false;
        }

        private static Transform GetLampEmitterRoot(Transform lightTransform)
        {
            Transform bestMatch = null;
            Transform current = lightTransform;
            while (current != null)
            {
                if (current.name.IndexOf("lamp", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    bestMatch = current;
                }
                current = current.parent;
            }

            return bestMatch != null ? bestMatch : lightTransform;
        }

        private static bool IsLikelyFlickeringLamp(Transform emitterRoot, Light light)
        {
            Animator animator = emitterRoot != null ? emitterRoot.GetComponent<Animator>() : null;
            if (animator == null && light != null)
            {
                animator = light.GetComponentInParent<Animator>();
            }

            return animator != null && animator.enabled && animator.runtimeAnimatorController != null;
        }
    }
}
