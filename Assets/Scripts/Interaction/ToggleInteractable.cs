using Managers;
using UnityEngine;

public class ToggleInteractable : MonoBehaviour, IInteractable
{
    public enum ToggleMode
    {
        Light,
        AudioSource,
        GameObjectActive,
        Animator
        // add more as needed that fit the 'toggle' structure
    }

    [Header("Mode")]
    [SerializeField] private ToggleMode mode = ToggleMode.Light;

    [Header("Prompt Keys (CSV rows)")]
    [SerializeField] private TextKey promptWhenOffKey; 
    [SerializeField] private TextKey promptWhenOnKey;  

    public TextKey PromptKey => IsOn() ? promptWhenOnKey : promptWhenOffKey;

    // Targets (use whichever matches your mode)
    [Header("Targets")]
    [SerializeField] private Light targetLight;
    [SerializeField] private AudioSource targetAudio;
    [SerializeField] private GameObject targetObject;
    [SerializeField] private Animator targetAnimator;
    // add more as we need

    [Header("FMOD SFX")]
    [SerializeField] private bool playToggleSfx = false;
    [SerializeField] private AudioManager.SfxId toggleSfxId = AudioManager.SfxId.TutorialButtonClick;
    [SerializeField] private bool playSecondaryToggleSfx = false;
    [SerializeField] private AudioManager.SfxId secondaryToggleSfxId = AudioManager.SfxId.TutorialDoorSlide;


    // From the IInteractable interface - set these as appropriate
    public AudioClip HoverSfx => null;
    public AudioClip InteractSfx => null;
    public AudioClip DeniedSfx => null;


    public bool CanInteract(Interactor interactor)
    {
        // Must have the target needed for the selected mode
        return mode switch
        {
            ToggleMode.Light => targetLight != null,
            ToggleMode.AudioSource => targetAudio != null,
            ToggleMode.GameObjectActive => targetObject != null,
            ToggleMode.Animator => targetAnimator != null,
            // add more as we need
            _ => false
        };
    }


    public void Interact(Interactor interactor)
    {
        if (!CanInteract(interactor)) return;

        bool newState = !IsOn();
        SetOn(newState);
        PlayToggleSfx();
        PlaySecondaryToggleSfx();
    }


    private void Awake()
    {
        // Optional auto-find to reduce setup pain - but you can obviously assign tehse in the Inspector
        if (targetLight == null) 
        { 
            targetLight = GetComponentInChildren<Light>();
        }

        if (targetAudio == null)
        {
            targetAudio = GetComponentInChildren<AudioSource>();
        }

        if (targetObject == null)
        {
            targetObject = gameObject;
        }

        if (targetAnimator == null)
        {
            targetAnimator = GetComponentInChildren<Animator>();
        }

        // add more as we need
    }

    private bool IsOn()
    {
        return mode switch
        {
            ToggleMode.Light => targetLight != null && targetLight.enabled,
            ToggleMode.AudioSource => targetAudio != null && targetAudio.enabled && targetAudio.isPlaying,
            ToggleMode.GameObjectActive => targetObject != null && targetObject.activeSelf,
            ToggleMode.Animator => targetAnimator != null && targetAnimator.enabled,
            // add more as we need
            _ => false
        };
    }

    // Add mode details here so that it is reusable
    private void SetOn(bool on)
    {
        switch (mode)
        {
            // for lights and light sources
            case ToggleMode.Light:
                targetLight.enabled = on;
                break;

            // for audio sources (singular) (e.g. alarm clock)
            case ToggleMode.AudioSource:
                targetAudio.enabled = true;
                if (on)
                {
                    if (!targetAudio.isPlaying) targetAudio.Play();
                }
                else
                {
                    if (targetAudio.isPlaying) targetAudio.Stop();
                }
                break;

            // appearance / disappearance of game objects
            case ToggleMode.GameObjectActive:
                targetObject.SetActive(on);
                break;

            // for animators
            case ToggleMode.Animator:
                targetAnimator.enabled = on;
                break;


                // add more as we need

        }
    }

    private void PlayToggleSfx()
    {
        if (!playToggleSfx)
        {
            return;
        }

        AudioManager audioManager = Object.FindAnyObjectByType<AudioManager>();
        if (audioManager != null)
        {
            audioManager.PlaySfx(toggleSfxId, transform);
        }
    }

    private void PlaySecondaryToggleSfx()
    {
        if (!playSecondaryToggleSfx)
        {
            return;
        }

        AudioManager audioManager = Object.FindAnyObjectByType<AudioManager>();
        if (audioManager != null)
        {
            audioManager.PlaySfx(secondaryToggleSfxId, transform);
        }
    }
}


