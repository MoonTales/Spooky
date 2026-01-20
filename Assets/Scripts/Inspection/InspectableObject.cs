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
    public class InspectableObject : MonoBehaviour
    {
        
        [Header("Inspection Object Settings")]
        [SerializeField, Tooltip("The name shown when the player inspects this object")] 
        private string objectName = "Inspectable Object";
        [SerializeField, Tooltip("A short description of the object shown during inspection")]
        private string objectDescription = "This is an inspectable object. You can provide a description here.";
        
        public void OnInteract()
        {
            // Placeholder for future interaction system integration!!!
            InspectionSystem.Instance.StartInspection(gameObject);
        }
        
        
        // Getters
        public string GetObjectName() { return objectName; }
        public string GetObjectDescription() { return objectDescription; }
    }
}
