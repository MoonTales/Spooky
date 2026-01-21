using System;
using UnityEngine;

namespace Inspection
{
    /// <summary>
    /// This class can be attached to any GameObject to make it inspectable in the inspection system.
    ///
    /// All of the properties setable are optional, but will allow for more detailed inspection if filled out.
    /// </summary>
    ///
    
    // WIll soon inherit from the InteractableObject class / Interactable system Interface
    public class InspectableObject : EventSubscriberBase, IInteractable
    {
        
        [Header("Inspection Object Settings")]
        [SerializeField, Tooltip("The name shown when the player inspects this object")] 
        private string objectName = "Inspectable Object";
        [SerializeField, Tooltip("A short description of the object shown during inspection")]
        private string objectDescription = "This is an inspectable object. You can provide a description here.";
        
        
        
        // Getters
        public string GetObjectName() { return objectName; }
        public string GetObjectDescription() { return objectDescription; }
        
        // Interface Implementation
        public bool CanInteract(Interactor interactor)
        {
            return true;
        }

        public void Interact(Interactor interactor)
        {
            InspectionSystem.Instance.StartInspection(gameObject);
        }

        protected override void OnWorldClockTicked(int newHour)
        {
            base.OnWorldClockTicked(newHour);
            DebugUtils.Log($"InspectableObject '{objectName}' received World Clock Tick: {newHour}");
        }

        public string Prompt { get; }
        public AudioClip HoverSfx { get; }
        public AudioClip InteractSfx { get; }
        public AudioClip DeniedSfx { get; }
    }
}
