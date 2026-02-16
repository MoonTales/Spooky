using System;
using Managers;
using Unity.VisualScripting;
using UnityEngine;
using Types = System.Types;

namespace Placeables
{
    public class ZoneMarker : MonoBehaviour
    {
        // if its a transition zone, we will need a second zone ID, second box collider, and a second gizmo color to represent the second zone
        // only have these options visible if this is true
        
        [SerializeField, Tooltip("The zone this is to mark")] private int zoneID; // the ID of the zone this marker represents
        [SerializeField] private Color editorGizmoColor = new Color(1f, 0f, 1f, 0.15f);
        
        // Internal variables
        private BoxCollider _boxCollider; // the box collider component attached to this game object


        private void Start()
        {
            // Setup the box collider for this zone marker, so that we can detect when the player enters it
            if(_boxCollider == null){ _boxCollider = GetComponent<BoxCollider>(); _boxCollider.isTrigger = true; }
            if(_boxCollider == null){ _boxCollider = gameObject.AddComponent<BoxCollider>(); _boxCollider.isTrigger = true; }
        }
        
        private void OnTriggerEnter(Collider other)
        {
            
            // These should only play during Gameplay
            if (GameStateManager.Instance.GetCurrentGameState() != Types.GameState.Gameplay) { return;}
            
            // ensure the thing that interacted with us is the player
            if (!other.CompareTag("Player")) { return; }
        
            // create our notification data
            Types.NotificationData data = new(
                duration: 1, 
                messageKey: new TextKey(),
                messageOverride: "You just entered the zone: " + zoneID
            );
            data.Send();
            
            GameStateManager.Instance.SetCurrentZoneId(zoneID);
            
        }

        private void OnDrawGizmos()
        {
            // Draw the box collider in the editor for visualization
            BoxCollider boxCollider = GetComponent<BoxCollider>();
            if (boxCollider != null)
            {
                Gizmos.color = editorGizmoColor;
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(boxCollider.center, boxCollider.size);
            }
    
            // Draw the zone ID label above the box collider
            #if UNITY_EDITOR
            if (boxCollider != null)
            {
                Vector3 labelPosition = transform.position + Vector3.up * (boxCollider.size.y / 2 + 0.5f);
                UnityEditor.Handles.Label(labelPosition, $"Zone ID: {zoneID}", 
                    new GUIStyle()
                    {
                        alignment = TextAnchor.MiddleCenter,
                        normal = new GUIStyleState() { textColor = Color.white },
                        fontSize = 24,
                        fontStyle = FontStyle.Bold
                    });
            }
            #endif
        }
    }
}
