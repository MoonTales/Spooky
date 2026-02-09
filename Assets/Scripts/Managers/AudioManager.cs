using System;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using Player;
using Types = System.Types;


namespace Managers
{
    public class AudioManager : Singleton<AudioManager>
    {
        #region SFX
        // IDs for unparameterized events
        public enum SfxId
        {
            // Player
            Jump, Landing, Flashlight, CrouchIn, CrouchOut, PeekIn, PeekOut, TippytoeIn, TippytoeOut,
        }

        // Inspector-configured entry mapping a SfxId to an FMOD event.
        [Serializable]
        public struct SfxEntry
        {
            public SfxId id;
            public EventReference eventRef;
        }

        [SerializeField] private SfxEntry[] sfxEvents;      // Inspector-assigned map of SfxId -> FMOD EventReference.
        private Dictionary<SfxId, EventReference> _sfxMap;  // Runtime lookup built from sfxEvents for fast access.
        private Bus _playerMovementBus;
        private float _mentalStateSeverity;
        private float _terrorSeverity;
        private bool _seededMentalStateFromStats;

        private EventInstance _ambienceDistortionSnapshotInstance;
        private EventInstance _heartbeatInstance;
        private bool _heartbeatIsPlaying;

        #region Parameterized Sfx
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

        // Parameterized Events
        [Header("Player Sounds")]
        [SerializeField] private EventReference footstepPlayer;     // Parameterized footstep event with Surface label parameter.
        [SerializeField] private string playerMovementBusPath = "bus:/SFX/Player/Movement"; // Bus containing player movement events for quick stops.

        [Header("Mental Audio")]
        [SerializeField] private EventReference ambienceDistortionSnapshot;
        [SerializeField] private string terrorDistortionParameter = "Terror";
        [SerializeField] private bool terrorParameterIsGlobal = true;
        [SerializeField] private string mentalHealthDistortionParameter = "MentalHealth";
        [SerializeField] private bool mentalHealthParameterIsGlobal = true;
        [SerializeField] private EventReference heartbeatLoopEvent;
        [SerializeField] private string heartbeatIntensityParameter = "Intensity";
        [SerializeField] private bool heartbeatIntensityParameterIsGlobal = false;
        [SerializeField, Range(0f, 1f)] private float heartbeatStartThreshold = 0.55f;
        [SerializeField, Range(0f, 1f)] private float heartbeatStopThreshold = 0.45f;
        
        [Header("Mutes")]
        public bool muteSFX = false;
        public bool muteMusic = false;

        [Header("Enemy Effects")]
        [Header("General Sounds")]
        [Header("UI Audio")]
        [Header("Soundtracks")]
        #endregion
        #endregion
        private AudioClip NullClip = null;


  

        //variables for the soundtrack
        public AudioSource Musicsource;
        public float sfxValue = 1;
        public float musicValue = 1;

        private bool muted = false;

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

            AudioSource mus = gameObject.AddComponent<AudioSource>();
            Musicsource = mus;
            mus.playOnAwake = false;
            mus.spatialBlend = 0f;
            mus.loop = true;
            mus.volume = 0;
        }

        protected override void OnDestroy()
        {
            StopAndReleaseHeartbeat();
            StopAndReleaseAmbienceSnapshot();
            base.OnDestroy();
        }

        /// <summary>
        /// Play a sound effect at a given volume multiplier.  
        /// Volume can be higher than 1.0f to boost the clip.  
        /// If a GameObject is provided, sound plays from its world position (3D).  
        /// </summary>
        /// 

        private void Start()
        {
            sfxValue = 1;
            musicValue = 1;
            TrySeedMentalStateFromPlayerStats();
        }
        
        private void Update()
        {
            Musicsource.volume = musicValue;
            UpdateMentalSeverityFromStats();
            if (!_seededMentalStateFromStats)
            {
                TrySeedMentalStateFromPlayerStats();
            }
        }

        private void OnPlayerMentalStateChanged(Types.PlayerMentalState newMentalState)
        {
            _mentalStateSeverity = GetMentalStateSeverity(newMentalState);
            RefreshMentalAudio();
        }

        private void OnTerrorIntensityChanged(float normalizedIntensity)
        {
            _terrorSeverity = Mathf.Clamp01(normalizedIntensity);
            Debug.Log($"AudioManager: Terror param value = {_terrorSeverity:0.000}");
            RefreshMentalAudio();
        }

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

        private void PlayEvent(EventReference eventReference, Transform fromTransform)
        {
            if (muteSFX) return;
            if (eventReference.IsNull) return;

            EventInstance instance = CreateEventInstance(eventReference, fromTransform);
            instance.start();
            instance.release();
        }

        private EventInstance CreateEventInstance(EventReference eventReference, Transform fromTransform)
        {
            // Use RuntimeManager to ensure correct FMOD instance tracking and virtualization.
            EventInstance instance = RuntimeManager.CreateInstance(eventReference);

            Vector3 position = fromTransform != null
                ? fromTransform.position
                : Camera.main != null
                    ? Camera.main.transform.position
                    : Vector3.zero;

            instance.set3DAttributes(RuntimeUtils.To3DAttributes(position));
            return instance;
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
        
        #region Player Sounds
        #region Jumping and Landing
        public void PlayPlayerJumping(float volume = 1, float deviation = 0.2f, Transform fromTransform = null)
        {
            StopFootstepsImmediate();
            PlaySfx(SfxId.Jump, fromTransform);
        }
        public void PlayPlayerLanding(float volume = 1, float deviation = 0.2f, Transform fromTransform = null)
        {
            PlaySfx(SfxId.Landing, fromTransform);
        }
        #endregion
        #region Crouching Sounds
        public void PlayPlayerCrouchIn(float volume = 1, float deviation = 0.2f, Transform fromTransform = null)
        {
            PlaySfx(SfxId.CrouchIn, fromTransform);
        }
        public void PlayPlayerCrouchOut(float volume = 1, float deviation = 0.2f, Transform fromTransform = null)
        {
            PlaySfx(SfxId.CrouchOut, fromTransform);
        }
        #endregion
        #region Footstep Sounds
        #endregion
        #region Peaking
        public void PlayPlayerPeakIn(float volume = 1, float deviation = 0.2f, Transform fromTransform = null)
        {
            PlaySfx(SfxId.PeekIn, fromTransform);
        }
        public void PlayPlayerPeakOut(float volume = 1, float deviation = 0.2f, Transform fromTransform = null)
        {
            PlaySfx(SfxId.PeekOut, fromTransform);
        }
        #endregion
        #region Tippytoe
        public void PlayPlayerTippytoeIn(float volume = 1, float deviation = 0.2f, Transform fromTransform = null)
        {
            PlaySfx(SfxId.TippytoeIn, fromTransform);
        }
        public void PlayPlayerTippytoeOut(float volume = 1, float deviation = 0.2f, Transform fromTransform = null)
        {
            PlaySfx(SfxId.TippytoeOut, fromTransform);
        }
        #endregion
        #endregion

        private void TrySeedMentalStateFromPlayerStats()
        {
            PlayerStats playerStats = FindAnyObjectByType<PlayerStats>();
            if (playerStats == null)
            {
                return;
            }

            _mentalStateSeverity = GetMentalSeverityFromStats(playerStats);
            _seededMentalStateFromStats = true;
            RefreshMentalAudio();
        }

        private void UpdateMentalSeverityFromStats()
        {
            PlayerStats playerStats = FindAnyObjectByType<PlayerStats>();
            if (playerStats == null)
            {
                return;
            }

            float nextSeverity = GetMentalSeverityFromStats(playerStats);
            if (Mathf.Abs(nextSeverity - _mentalStateSeverity) > 0.001f)
            {
                _mentalStateSeverity = nextSeverity;
                RefreshMentalAudio();
            }
        }

        private float GetMentalSeverityFromStats(PlayerStats playerStats)
        {
            Types.FPlayerStats stats = playerStats.GetPlayerStats();
            float max = stats.GetMaxMentalHealth();
            if (max <= 0f)
            {
                return 1f;
            }

            float normalizedHealth = Mathf.Clamp01(stats.GetCurrentMentalHealth() / max);
            return 1f - normalizedHealth;
        }

        private void RefreshMentalAudio()
        {
            float combinedSeverity = Mathf.Clamp01(Mathf.Max(_mentalStateSeverity, _terrorSeverity));
            ApplyAmbienceDistortion(combinedSeverity);
            ApplyHeartbeat(combinedSeverity);
        }

        private void ApplyAmbienceDistortion(float combinedSeverity)
        {
            if (ambienceDistortionSnapshot.IsNull)
            {
                return;
            }

            if (!_ambienceDistortionSnapshotInstance.isValid())
            {
                _ambienceDistortionSnapshotInstance = RuntimeManager.CreateInstance(ambienceDistortionSnapshot);
                _ambienceDistortionSnapshotInstance.start();
            }

            if (!string.IsNullOrWhiteSpace(terrorDistortionParameter))
            {
                SetFmodParameter(_ambienceDistortionSnapshotInstance, terrorDistortionParameter, _terrorSeverity, terrorParameterIsGlobal);
            }

            if (!string.IsNullOrWhiteSpace(mentalHealthDistortionParameter))
            {
                SetFmodParameter(_ambienceDistortionSnapshotInstance, mentalHealthDistortionParameter, _mentalStateSeverity, mentalHealthParameterIsGlobal);
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
                _heartbeatInstance = RuntimeManager.CreateInstance(heartbeatLoopEvent);
                if (!string.IsNullOrWhiteSpace(heartbeatIntensityParameter))
                {
                    SetFmodParameter(_heartbeatInstance, heartbeatIntensityParameter, combinedSeverity, heartbeatIntensityParameterIsGlobal);
                }
                _heartbeatInstance.start();
                _heartbeatIsPlaying = true;
            }
        }

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

        private void StopAndReleaseHeartbeat()
        {
            if (_heartbeatInstance.isValid())
            {
                _heartbeatInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                _heartbeatInstance.release();
            }
            _heartbeatIsPlaying = false;
        }

        private void StopAndReleaseAmbienceSnapshot()
        {
            if (_ambienceDistortionSnapshotInstance.isValid())
            {
                _ambienceDistortionSnapshotInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                _ambienceDistortionSnapshotInstance.release();
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

        
    }
}
