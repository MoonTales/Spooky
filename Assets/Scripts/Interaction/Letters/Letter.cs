using UnityEngine;
using System;
using Types = System.Types;

namespace Interaction.Letters
{
    public class Letter : MonoBehaviour, IInteractable
    {
    
        public TextKey PromptKey { get; }
        // Start is called once before the first execution of Update after the MonoBehaviour is created
    
        public bool CanInteract(Interactor interactor)
        {
            // you can always interact with letters
            return true;
        }

        public void Interact(Interactor interactor)
        {
            Types.NotificationData data = new(
                duration: 3.0f, 
                messageKey: new TextKey { place = "Letters", id = "LetterContent1"},
                messageOverride: "This is a fun letter!"
            );
            data.Send();
            Destroy(this.gameObject);
        }
    }
}
