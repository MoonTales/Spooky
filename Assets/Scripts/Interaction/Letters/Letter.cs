using UnityEngine;
using System;
using Inspection;
using Types = System.Types;

namespace Interaction.Letters
{
    public class Letter : InspectableObject, IInteractable
    {
    
        public TextKey PromptKey { get; }
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        

        public void Interact(Interactor interactor)
        {
            Types.NotificationData data = new(
                duration: 3.0f, 
                messageKey: new TextKey { place = "Letters", id = "LetterContent1"},
                messageOverride: "This is a fun letter!"
            );
            data.Send();
            InspectionSystem.Instance.StartInspection(gameObject);
        }

        public override void OnInspectionFinished()
        {
            // Custom logic that can run once the inspection has been completed fully
            DebugUtils.Log($"Finished inspecting letter: !!");
        }
    }
}
