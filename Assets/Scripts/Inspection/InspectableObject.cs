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

        [Header("Text Keys (CSV row pointers)")]
        [SerializeField, Tooltip("Row key that contains name / description fields for inspection UI")]
        private TextKey rowKey;
        [SerializeField, Tooltip("Row key that contains the prompt field for hover interaction text (optional as needed)")]
        private TextKey promptKey;

        [SerializeField] private int requiredHour = -1; // -1 means no time restriction
        
        // internal 
        private MeshRenderer[] _meshRenderers;
        private Collider[] _objColliders;

        // Getters
        public TextKey RowKey => rowKey;
        public TextKey PromptKey => promptKey;

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
            DebugUtils.Log($"InspectableObject '{rowKey}' received World Clock Tick: {newHour}");

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

    }
}
