using Managers;
using UnityEngine;

public class PlaySoundOnInteract : MonoBehaviour, IInteractable
{
    [Header("SFX")]
    [SerializeField] private AudioClip InteractSfxClip;

    public TextKey PromptKey { get; }
    public bool CanInteract(Interactor interactor)
    {
        return true;
    }

    public void Interact(Interactor interactor)
    {
        // get the gameobject this component is attached to
        UAudio.Instance.PlayClip(InteractSfxClip, deviation: 0.1f, fromObject: gameObject);
    }
}
