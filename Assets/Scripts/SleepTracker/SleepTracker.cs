using UnityEngine;

/// <summary>
/// This is entirley a proxy class just to avoid having to put the IInteractable interface on the SleepTrackerManager, which is a singleton that persists across scenes, and is not actually attached to any game object in the scene.
/// </summary>
public class SleepTracker : MonoBehaviour, IInteractable
{
    
    [Header("Text Keys (CSV row pointers)")]
    [SerializeField] private TextKey promptTextKey;
    public TextKey PromptKey => SleepTrackerManager.Instance.GetIsSleepTrackerActive() ? promptTextKey: TextKey.Empty; // only show the prompt if the sleep tracker is active, otherwise we will just show nothing when we interact with it
    public bool CanInteract(Interactor interactor)
    {
        return SleepTrackerManager.Instance.CanInteract(interactor);
    }

    public void Interact(Interactor interactor)
    {
        SleepTrackerManager.Instance.Interact(interactor);
    }
}
