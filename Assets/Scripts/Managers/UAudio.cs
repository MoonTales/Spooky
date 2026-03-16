using System;
using System.Collections.Generic;
using UnityEngine;

namespace Managers
{
    /// <summary>
    /// This is a singleton used for the Unity Implementations of Audio within the project
    /// </summary>
    public class UAudio : Singleton<UAudio>
    {
        [SerializeField] private int _poolSize = 10;
        private List<AudioSource> _sources;

        //public float sfxvolumeslider = 1;
        
        [Header("Example Sounds")]
        public AudioClip ExampleAudioClip;
        
        [Header("Mutes")]
        public bool muteSFX = false;
        
        
        //variables for the soundtrack
        public float sfxValue = 1;

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
        }
        
        private void Update()
        {
            
            // test, when we press T, we will play a concrete footstep sound
            if (Input.GetKeyDown(KeyCode.T))
            {
                PlayExampleAudio(volume: 1f, deviation: 0.1f, fromObject: null);
            }
            
            // always sync the audio (inefficent, but it works)
            sfxValue = AudioManager.Instance.GetSfxVolume();
        }

        private void PlaySFX(AudioClip clip, float volume = 1f, float deviation = 0f, GameObject fromObject = null)
        {
            if (muteSFX) return;
            if (clip == null) return;

            AudioSource src = GetFreeSource();
            if (src == null) return;

            src.transform.position = fromObject ? fromObject.transform.position : Camera.main ? Camera.main.transform.position : Vector3.zero;

            src.spatialBlend = fromObject ? 1f : 0f;
            src.volume = (volume * sfxValue);
            src.clip = clip;
            src.pitch = UnityEngine.Random.Range(1 - deviation, 1 + deviation);
            src.Play();
        }

        
        //This is called like:
        // UAudio.Instance.PlayExampleAudio(fromObject: someGameObject, volume: 0.5f, deviation: 0.2f);
        // where all the params a
        #region Example Sounds
        public void PlayExampleAudio(GameObject fromObject = null, float volume = 1f, float deviation = 0f)
        {
            PlaySFX(ExampleAudioClip, volume, deviation, fromObject);
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
