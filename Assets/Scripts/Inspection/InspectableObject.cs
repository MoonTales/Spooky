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
        
        [SerializeField] private int requiredHour = -1; // -1 means no time restriction
        
        // internal 
        private MeshRenderer[] _meshRenderers;
        private Collider[] _objColliders;
        
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

        protected override void OnEnable()
        {
            base.OnEnable();
            
            _meshRenderers = GetComponentsInChildren<MeshRenderer>();
            _objColliders = GetComponentsInChildren<Collider>();
        }

        protected override void OnWorldClockTicked(int newHour)
        {
            DebugUtils.Log($"InspectableObject '{objectName}' received World Clock Tick: {newHour}");

            if (newHour >= requiredHour)
            {
                for (int i = 0; i < _meshRenderers.Length; i++)
                {
                    _meshRenderers[i].enabled = true;
                }
                for (int i = 0; i < _objColliders.Length; i++)
                {
                    _objColliders[i].enabled = true;
                }
            } else
            {
                for (int i = 0; i < _meshRenderers.Length; i++)
                {
                    _meshRenderers[i].enabled = false;
                }
                for (int i = 0; i < _objColliders.Length; i++)
                {
                    _objColliders[i].enabled = false;
                }
            }
        }

        public string Prompt { get; }
        public AudioClip HoverSfx { get; }
        public AudioClip InteractSfx { get; }
        public AudioClip DeniedSfx { get; }
    }
}
