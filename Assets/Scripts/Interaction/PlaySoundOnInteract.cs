using Managers;
using UnityEngine;

public class PlaySoundOnInteract : MonoBehaviour, IInteractable
{
    [Header("SFX")]
    [SerializeField] private AudioClip InteractSfxClip;

    [SerializeField] private float RequiredDelayToPressAgain = 0.1f;
    
    [SerializeField] private bool ApartOfSecretCode = false;

    private float _lastInteractTime = -Mathf.Infinity;

    public TextKey PromptKey { get; }

    public bool CanInteract(Interactor interactor)
    {
        return Time.time >= _lastInteractTime + RequiredDelayToPressAgain;
    }

    public void Interact(Interactor interactor)
    {
        _lastInteractTime = Time.time;
        UAudio.Instance.PlayClip(InteractSfxClip, deviation: 0.1f, fromObject: gameObject);
        
        if (ApartOfSecretCode)
        {
            SecretCodeManager.Instance.ButtonPressed();
        }
    }
}