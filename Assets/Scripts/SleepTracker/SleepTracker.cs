using UnityEngine;

/// <summary>
/// This is entirley a proxy class just to avoid having to put the IInteractable interface on the SleepTrackerManager, which is a singleton that persists across scenes, and is not actually attached to any game object in the scene.
/// </summary>
public class SleepTracker : MonoBehaviour, IInteractable
{
    
    [Header("Text Keys (CSV row pointers)")]
    [SerializeField] private TextKey promptTextKey;
    [SerializeField] private TextKey promptWhenInactiveKey;
    
    // internal Glowly
    private GameObject glowy;
    // Keep inactive prompt wiring in place for future toggle behavior, even though
    // current interaction rules only allow turning the tracker off.
    public TextKey PromptKey =>
        SleepTrackerManager.Instance.GetIsSleepTrackerActive()
            ? promptTextKey
            : (promptWhenInactiveKey.IsValid ? promptWhenInactiveKey : promptTextKey);
    public bool CanInteract(Interactor interactor)
    {
        return SleepTrackerManager.Instance.CanInteract(interactor);
    }

    public void Interact(Interactor interactor)
    {
        SleepTrackerManager.Instance.Interact(interactor);
        glowy.SetActive(SleepTrackerManager.Instance.GetIsSleepTrackerActive());
    }

    public void Start()
    {
        // find the child object called "Glowly" and store a reference to it, so we can enable/disable it when the sleep tracker is active/inactive
        glowy = transform.Find("Glowy").gameObject;
        SleepTrackerManager.Instance.RegisterSleepTrackerSourceTransform(transform);
        glowy.SetActive(SleepTrackerManager.Instance.GetIsSleepTrackerActive());
    }
}
