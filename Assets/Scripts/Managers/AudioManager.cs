using System;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using Types = System.Types;


namespace Managers
{
    public class AudioManager : Singleton<AudioManager>
    {
        // IDs for unparameterized SFX event mappings.
        public enum SfxId
        {
            // Player
            Jump, Landing, Flashlight, CrouchIn, CrouchOut, PeekIn, PeekOut, TippytoeIn, TippytoeOut,
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
        private EventInstance _heartbeatInstance;
        private bool _heartbeatIsPlaying;
        private EventInstance _terrorLoopInstance;
        private bool _terrorLoopIsPlaying;
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
        }

        // Serialized FMOD event references and parameters.
        [Header("Player Sounds")]
        [SerializeField] private EventReference footstepPlayer;     // Parameterized footstep event with Surface label parameter.
        [SerializeField] private string playerMovementBusPath = "bus:/SFX/Player/Movement"; // Bus containing player movement events for quick stops.

        [Header("Mental Audio")]
        [SerializeField] private string terrorDistortionParameter = "Terror";
        [SerializeField] private string mentalHealthDistortionParameter = "MentalHealth";
        [SerializeField] private bool terrorParameterIsGlobal = true;
        [SerializeField] private bool mentalHealthParameterIsGlobal = true;
        [SerializeField] private EventReference terrorLoopEvent;
        [SerializeField] private EventReference nightmareAmbLoopEvent;
        [SerializeField] private EventReference heartbeatLoopEvent;
        [SerializeField] private string heartbeatIntensityParameter = "Intensity";
        [SerializeField] private bool heartbeatIntensityParameterIsGlobal = false;
        [SerializeField, Range(0f, 1f)] private float heartbeatStartThreshold = 0.55f;
        [SerializeField, Range(0f, 1f)] private float heartbeatStopThreshold = 0.45f;
        [SerializeField] private bool logTerrorParameterValue = false;

        [Header("Settings Menu Audio")]
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
            TrackSubscription(() => EventBroadcaster.OnPlayerHealthStateChanged += OnPlayerMentalStateChanged,
                () => EventBroadcaster.OnPlayerHealthStateChanged -= OnPlayerMentalStateChanged);
            TrackSubscription(() => EventBroadcaster.OnTerrorIntensityChanged += OnTerrorIntensityChanged,
                () => EventBroadcaster.OnTerrorIntensityChanged -= OnTerrorIntensityChanged);
        }

        protected override void Awake()
        {
            base.Awake();

            BuildSfxMap();
            CachePlayerMovementBus();
            CacheSettingsBuses();
        }

        protected override void OnDestroy()
        {
            StopAndReleaseHeartbeat();
            StopAndReleaseTerrorLoop();
            StopAndReleaseNightmareAmbience();
            SetPauseSnapshotEnabled(false);
            base.OnDestroy();
        }
        
        // Event handlers
        private void OnPlayerMentalStateChanged(Types.PlayerMentalState newMentalState)
        {
            _mentalStateSeverity = GetMentalStateSeverity(newMentalState);
            RefreshMentalAudio();
        }

        private void OnTerrorIntensityChanged(float normalizedIntensity, Transform terrorSourceTransform)
        {
            _terrorSeverity = Mathf.Clamp01(normalizedIntensity);
            _terrorSourceTransform = terrorSourceTransform;
            if (logTerrorParameterValue)
            {
                Debug.Log($"AudioManager: Terror param value = {_terrorSeverity:0.000}");
            }
            RefreshMentalAudio();
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
            if (isMuted)
            {
                StopAndReleaseHeartbeat();
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
                return;
            }

            if (enabled)
            {
                if (_pauseSnapshotActive)
                {
                    return;
                }

                if (!_pauseSnapshotInstance.isValid())
                {
                    _pauseSnapshotInstance = RuntimeManager.CreateInstance(pauseSnapshotEvent);
                }
                _pauseSnapshotInstance.start();
                _pauseSnapshotActive = true;
                return;
            }

            if (_pauseSnapshotInstance.isValid())
            {
                _pauseSnapshotInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                _pauseSnapshotInstance.release();
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
        
        public void PlayPlayerJumping(float volume = 1, float deviation = 0.2f, Transform fromTransform = null)
        {
            StopFootstepsImmediate();
            PlaySfx(SfxId.Jump, fromTransform);
        }

        // Mental audio stack
        private void RefreshMentalAudio()
        {
            float combinedSeverity = Mathf.Clamp01(Mathf.Max(_mentalStateSeverity, _terrorSeverity));
            ApplyTerrorLoop(_terrorSeverity);
            ApplyNightmareAmbience();
            ApplyHeartbeat(combinedSeverity);
        }

        private void ApplyTerrorLoop(float terrorSeverity)
        {
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
                return;
            }

            if (_terrorLoopInstance.isValid())
            {
                _terrorLoopInstance.set3DAttributes(RuntimeUtils.To3DAttributes(_terrorSourceTransform.position));
            }
        }

        private void ApplyNightmareAmbience()
        {
            if (nightmareAmbLoopEvent.IsNull)
            {
                return;
            }

            if (!_nightmareAmbienceInstance.isValid())
            {
                _nightmareAmbienceInstance = CreateEventInstance(nightmareAmbLoopEvent);
                _nightmareAmbienceInstance.start();
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

        private void ApplyHeartbeat(float combinedSeverity)
        {
            if (heartbeatLoopEvent.IsNull)
            {
                return;
            }

            if (_heartbeatIsPlaying)
            {
                if (combinedSeverity <= heartbeatStopThreshold || muteSFX)
                {
                    StopAndReleaseHeartbeat();
                    return;
                }

                if (!string.IsNullOrWhiteSpace(heartbeatIntensityParameter) && _heartbeatInstance.isValid())
                {
                    SetFmodParameter(_heartbeatInstance, heartbeatIntensityParameter, combinedSeverity, heartbeatIntensityParameterIsGlobal);
                }
                return;
            }

            if (combinedSeverity >= heartbeatStartThreshold && !muteSFX)
            {
                _heartbeatInstance = CreateEventInstance(heartbeatLoopEvent);
                if (!string.IsNullOrWhiteSpace(heartbeatIntensityParameter))
                {
                    SetFmodParameter(_heartbeatInstance, heartbeatIntensityParameter, combinedSeverity, heartbeatIntensityParameterIsGlobal);
                }
                _heartbeatInstance.start();
                _heartbeatIsPlaying = true;
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
            }
            _heartbeatIsPlaying = false;
        }

        private void StopAndReleaseTerrorLoop()
        {
            if (_terrorLoopInstance.isValid())
            {
                _terrorLoopInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                _terrorLoopInstance.release();
            }
            _terrorLoopIsPlaying = false;
        }

        private void StopAndReleaseNightmareAmbience()
        {
            if (_nightmareAmbienceInstance.isValid())
            {
                _nightmareAmbienceInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                _nightmareAmbienceInstance.release();
            }
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
    }
}
