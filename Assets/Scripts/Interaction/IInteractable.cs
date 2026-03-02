using UnityEngine;

public interface IInteractable
{
    // key to a short prompt
    TextKey PromptKey { get; }


    // return false to block interaction (locked, already used, wrong state, whatever)
    // pass Interactor here just in case enemies interact with objects differently
    bool CanInteract(Interactor interactor);

    // called when the player interacts
    void Interact(Interactor interactor);


    // optional common audio (can be null)
    //AudioClip HoverSfx { get; }
    //AudioClip InteractSfx { get; }
    //AudioClip DeniedSfx { get; }
}
