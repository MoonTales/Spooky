using System;
using Managers;
using UnityEngine;
using Types = System.Types;
namespace UI
{
    /// <summary>
    /// This class will be used to send off a notification message when the player walks through it
    /// </summary>
    public class NotificationPopupHitbox : MonoBehaviour
    {
        // Settable variables
        [SerializeField] private float notificationDisplayDuration = 5f; // how long the notification should be displayed for
        [SerializeField] private TextKey notificationTextKey; // the text key for the notification message to be displayed
        [SerializeField] private string notificationTextOverride; // optional override for the notification message (if not using text keys)
        [SerializeField] private bool bTriggerOnce = true; // whether the hitbox should only trigger once or can be triggered multiple times
        [SerializeField] private Color editorGizmoColor = new Color(0f, 1f, 0f, 0.15f); // color for the editor gizmo
    
        // Internal variables
        private BoxCollider _boxCollider; // the box collider component attached to this game object

        private void Start()
        {
            _boxCollider = GetComponent<BoxCollider>();
            _boxCollider.isTrigger = true; // ensure the box collider is set to be a trigger
        }
    
        // connect th the overlap function to detect when the player walks through the hitbox
        private void OnTriggerEnter(Collider other)
        {

        }
        
        private void OnTriggerStay(Collider other)
        {
            
            // we do it here, incase we spawn inside of it, since we only run once anyways
            
            // These should only play during Gameplay
            if (GameStateManager.Instance.GetCurrentGameState() != Types.GameState.Gameplay) { return;}
            
            // ensure the thing that interacted with us is the player
            if (!other.CompareTag("Player")) { return; }
        
            // create our notification data
            Types.NotificationData data = new(
                duration: notificationDisplayDuration, 
                messageKey: notificationTextKey,
                messageOverride: notificationTextOverride
            );
            data.Send();
        
            // if we are only triggering once, destroy this object
            if (bTriggerOnce) { Destroy(this);}
        }

        private void OnDrawGizmos()
        {
            // NGL I had no idea you could do this LOL
            // Draw the box collider in the editor for visualization
            BoxCollider boxCollider = GetComponent<BoxCollider>();
            if (boxCollider != null)
            {
                Gizmos.color = editorGizmoColor;
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(boxCollider.center, boxCollider.size);
            }
        }
    }
}
