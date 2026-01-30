using System;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;


namespace Managers
{
    public class AudioManager : Singleton<AudioManager>
    {
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

        [SerializeField] private SfxEntry[] sfxEvents; // Inspector-assigned map of SfxId -> FMOD EventReference.
        // TODO(FMOD): Populate this list in the AudioManager prefab for all migrated SFX.

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

        private Dictionary<SfxId, EventReference> _sfxMap; // Runtime lookup built from sfxEvents for fast access.

        // Parameterized Events
        [Header("Player Sounds")]
        [SerializeField] private EventReference footstepPlayer; // Parameterized footstep event with Surface label parameter.

        
        [Header("Mutes")]
        public bool muteSFX = false;
        public bool muteMusic = false;

        [Header("Enemy Effects")]
        [Header("General Sounds")]
        [Header("UI Audio")]
        [Header("Soundtracks")]
        private AudioClip NullClip = null;


  

        //variables for the soundtrack
        public AudioSource Musicsource;
        public float sfxValue = 1;
        public float musicValue = 1;

        private bool muted = false;

        protected override void Awake()
        {
            base.Awake();

            BuildSfxMap();

            AudioSource mus = gameObject.AddComponent<AudioSource>();
            Musicsource = mus;
            mus.playOnAwake = false;
            mus.spatialBlend = 0f;
            mus.loop = true;
            mus.volume = 0;
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
        }
        
        private void Update()
        {
            Musicsource.volume = musicValue;
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
        }
        
        #region Player Sounds
        #region Jumping and Landing
        public void PlayPlayerJumping(float volume = 1, float deviation = 0.2f, Transform fromTransform = null)
        {
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

        
    }
}
