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
            // ensure the thing that interacted with us is the player
            if (!other.CompareTag("Player")) { return; }
        
            // create our notification data
            Types.NotificationData data = new(
                duration: notificationDisplayDuration, 
                messageKey: notificationTextKey,
                messageOverride: notificationTextOverride
            );
            data.Send();
        
            // if we are only triggering once, disable the collider
            if (bTriggerOnce) { _boxCollider.enabled = false; }
        }
    }
}
