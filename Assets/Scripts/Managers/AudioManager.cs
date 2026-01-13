using System;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace Managers
{
    public class AudioManager : Singleton<AudioManager>
    {
        [SerializeField] private int _poolSize = 10;
        private List<AudioSource> _sources;

        //public float sfxvolumeslider = 1;
        
        [Header("Footstep Sounds")]
        public AudioClip[] soundGrass;
        public AudioClip[] soundWater;
        public AudioClip[] soundConcrete;
        public AudioClip[] soundGravel;
        public AudioClip[] soundWood;
        public AudioClip[] soundMetal;
        public AudioClip landingAudioClip;
        public AudioClip jumpingAudioClip;
        
        [Header("Mutes")]
        public bool muteSFX = false;
        public bool muteMusic = false;

        [Header("Player Sounds")]
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

            // Create a pool of AudioSources we can reuse
            _sources = new List<AudioSource>();
            for (int i = 0; i < _poolSize; i++)
            {
                AudioSource src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 0f; // 2D by default
                _sources.Add(src);
            }

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

        private void PlaySFX(AudioClip clip, float volume = 1f, float deviation = 0f, Transform fromTransform = null)
        {
            if (muteSFX) return;
            if (clip == null) return;

            AudioSource src = GetFreeSource();
            if (src == null) return;

            src.transform.position = fromTransform != null ? fromTransform.position : Camera.main != null ? Camera.main.transform.position : Vector3.zero;

            src.spatialBlend = fromTransform != null ? 1f : 0f;
            src.volume = (volume * sfxValue);
            src.clip = clip;
            src.pitch = UnityEngine.Random.Range(1 - deviation, 1 + deviation);
            src.Play();
        }


        
        #region Footstep Sounds
        public void PlayPlayerWalkingGrass(float volume = 1, float deviation = 0.2f, Transform fromTransform = null)
        {
            if (soundGrass.Length == 0) { return; }
            int stepnumber;
            stepnumber = UnityEngine.Random.Range(0, soundGrass.Length);
            AudioClip step = soundGrass[stepnumber];
            PlaySFX(step, volume, deviation, fromTransform);
        }
        public void PlayPlayerWalkingWater(float volume = 1, float deviation = 0.2f, Transform fromTransform = null)
        {
            if (soundWater.Length == 0){ return;}
            int stepnumber;
            stepnumber = UnityEngine.Random.Range(0, soundWater.Length);
            AudioClip step = soundWater[stepnumber];
            PlaySFX(step, volume, deviation, fromTransform);
        }
        public void PlayPlayerWalkingConcrete(float volume = 1, float deviation = 0.2f, Transform fromTransform = null)
        {
            // ensure we have some sounds to play
            if (soundConcrete.Length == 0) {return; }
            int stepnumber;
            stepnumber = UnityEngine.Random.Range(0, soundConcrete.Length);
            AudioClip step = soundConcrete[stepnumber];
            PlaySFX(step, volume, deviation, fromTransform);
        }
        public void PlayPlayerWalkingGravel(float volume = 1, float deviation = 0.2f, Transform fromTransform = null)
        {
            if (soundGravel.Length == 0) { return; }
            int stepnumber;
            stepnumber = UnityEngine.Random.Range(0, soundGravel.Length);
            AudioClip step = soundGravel[stepnumber];
            PlaySFX(step, volume, deviation, fromTransform);
        }
        public void PlayPlayerWalkingWood(float volume = 1, float deviation = 0.2f, Transform fromTransform = null)
        {
            if (soundWood.Length == 0) { return; }
            int stepnumber;
            stepnumber = UnityEngine.Random.Range(0, soundWood.Length);
            AudioClip step = soundWood[stepnumber];
            PlaySFX(step, volume, deviation, fromTransform);
        }
        public void PlayPlayerWalkingMetal(float volume = 1, float deviation = 0.2f, Transform fromTransform = null)
        {
            if (soundMetal.Length == 0) { return; }
            int stepnumber;
            stepnumber = UnityEngine.Random.Range(0, soundMetal.Length);
            AudioClip step = soundMetal[stepnumber];
            PlaySFX(step, volume, deviation, fromTransform);
        }
        #endregion

        
        private AudioSource GetFreeSource()
        {
            foreach (var src in _sources)
            {
                if (!src.isPlaying)
                    return src;
            }
            // If none are free, just reuse the first
            return _sources[0];
        }
    }
}
