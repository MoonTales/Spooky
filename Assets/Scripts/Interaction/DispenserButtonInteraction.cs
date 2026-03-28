using UnityEngine;
using Managers;

public class DispenserButtonInteraction : MonoBehaviour, IInteractable
{
    [Header("Prompt")]
    [SerializeField] private TextKey promptKey;
    public TextKey PromptKey => promptKey;

    [Header("Button SFX")]
    [SerializeField] private AudioClip buttonPressSfx;
    [SerializeField] private AudioClip first;
    [SerializeField] private AudioClip second;

    [SerializeField] private float buttonPressVolume = 1f;
    [SerializeField] private float buttonPressDeviation = 0.05f;

    [Header("Press Visual Kinda Maybe")]
    [SerializeField] private Transform buttonVisual;
    [SerializeField] private float pressDistance = 20f;

    private AudioSource longSource;
    private Vector3 buttonStartLocalPos;

    // 0 = first long sound not used yet - this is the machine sputtering n stuff
    // 1 = first used, second not used yet - second is kind of just a tiny sound following the first
    // 2 = sequence finished, only press sound remains - machine is dead

    private int sequenceStep = 0;

    private void Awake()
    {
        longSource = gameObject.AddComponent<AudioSource>();
        longSource.playOnAwake = false;
        longSource.loop = false;
        longSource.spatialBlend = 1f;
        longSource.volume = buttonPressVolume;

        if (buttonVisual != null)
            buttonStartLocalPos = buttonVisual.localPosition;
    }

    public bool CanInteract(Interactor interactor)
    {
        return true;
    }

    public void Interact(Interactor interactor)
    {
        // always play the short button press - this happens no matter what because its just the button
        UAudio.Instance.PlayClip(buttonPressSfx, gameObject, buttonPressVolume, buttonPressDeviation );

        // trying out button movement, i have no idea how thsis works
        if (buttonVisual != null)
        {
            buttonVisual.localPosition = buttonStartLocalPos + new Vector3(-pressDistance, 0f, 0f);
            CancelInvoke(nameof(ResetButtonVisual));
            Invoke(nameof(ResetButtonVisual), 0.1f);
        }

        // if the longa ass sound is already playing, do not interrupt / reset it
        if (longSource.isPlaying)
            return;

        // otherwise advance through sequence as described above somewehre 
        if (sequenceStep == 0 && first != null)
        {
            longSource.clip = first;
            longSource.volume = buttonPressVolume;
            longSource.Play();
            sequenceStep = 1;
        }
        else if (sequenceStep == 1 && second != null)
        {
            longSource.clip = second;
            longSource.volume = buttonPressVolume;
            longSource.Play();
            sequenceStep = 2;
        }
        // sequenceStep == 2 means do nothing except the button press sound
    }

    // move it back move it back move aaabaa
    private void ResetButtonVisual()
    {
        if (buttonVisual != null)
            buttonVisual.localPosition = buttonStartLocalPos;
    }
}