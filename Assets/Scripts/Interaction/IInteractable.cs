using UnityEngine;

public interface IInteractable
{
    // maybe a short prompt for UI, like "pick up key", "hide", "turn off alarm". Can be removed if not needed.
    string Prompt { get; }

    // return false to block interaction (locked, already used, wrong state, whatever)
    // pass Interactor here just in case enemies interact with objects differently
    bool CanInteract(Interactor interactor);

    // called when the player interacts
    void Interact(Interactor interactor);
}
