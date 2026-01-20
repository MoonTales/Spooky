using UnityEngine;

public class ToggleInteractable : MonoBehaviour, IInteractable
{
    public enum ToggleMode
    {
        Light,
        AudioSource,
        GameObjectActive,
        // add more as needed that fit the 'toggle' structure
    }

    [Header("Mode")]
    [SerializeField] private ToggleMode mode = ToggleMode.Light;

    [Header("Prompt")]
    [SerializeField] private string promptWhenOff = "Turn on";
    [SerializeField] private string promptWhenOn = "Turn off";

    // Targets (use whichever matches your mode)
    [Header("Targets")]
    [SerializeField] private Light targetLight;
    [SerializeField] private AudioSource targetAudio;
    [SerializeField] private GameObject targetObject;
    // add more as we need

    public string Prompt => IsOn() ? promptWhenOn : promptWhenOff;


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
            // add more as we need
            _ => false
        };
    }


    public void Interact(Interactor interactor)
    {
        if (!CanInteract(interactor)) return;

        bool newState = !IsOn();
        SetOn(newState);
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

        // add more as we need
    }

    private bool IsOn()
    {
        return mode switch
        {
            ToggleMode.Light => targetLight != null && targetLight.enabled,
            ToggleMode.AudioSource => targetAudio != null && targetAudio.enabled && targetAudio.isPlaying,
            ToggleMode.GameObjectActive => targetObject != null && targetObject.activeSelf,
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


            // add more as we need

        }
    }
}


