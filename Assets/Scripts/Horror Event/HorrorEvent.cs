using System;
using System.Collections.Generic;
using Managers;
using UnityEngine;
using Types = System.Types;

namespace Horror_Event
{
    
    
    [Serializable]
    public struct HorrorEventData
    {
        public string EventName;
        public ParticleSystem EventParticles;
        public AudioClip EventSound;
        public GameObject gameobj;
    }
    /*
     * The main object class used to place Horror Events within the world.
     * These are events that only happen once, triggered via a hitbox, and cause some effect to play
     * Such as:
     * a hitbox, linking to a light, that turns off the light, plays a sound, and spawns particals
     */
    public class HorrorEvent : MonoBehaviour
    {
        [Header("Horror Event Settings")]
        [SerializeField] private Color editorGizmoColor = new Color(1f, 0f, 1f, 0.15f);
        [SerializeField] private string eventName = "Horror Event"; // the name of this event, used for debugging and notifications
        [SerializeField] private List<HorrorEventData> eventEffects = new List<HorrorEventData>(); // the effects that will play when this event is triggered, such as sounds, particles, and gameobject activations
        [SerializeField] private bool maintainEffectsAfterTrigger = false; 
        
        //Internal variables
        private BoxCollider _boxCollider; // the box collider component attached to this game object
        private string _horrorEventId; public string GetHorrorEventId() { return _horrorEventId; } // a unique ID for this horror event, used to track whether it has been triggered before or not (for saving/loading purposes)
        void Start()
        {
            // Setup the box collider for this zone marker, so that we can detect when the player enters it
            if(_boxCollider == null){ _boxCollider = GetComponent<BoxCollider>(); _boxCollider.isTrigger = true; }
            if(_boxCollider == null){ _boxCollider = gameObject.AddComponent<BoxCollider>(); _boxCollider.isTrigger = true; }

            _horrorEventId = eventName + editorGizmoColor + eventEffects; // creates a unique ID that SHOULD be different for each
            DebugUtils.Log($"[HorrorEvent] Initialized Horror Event with ID: {_horrorEventId}");
            
            // We need to check if we have already been activated in a previous playthrough
            if (maintainEffectsAfterTrigger && HorrorEventManager.Instance.CheckIfHorrorEventTriggered(this))
            {
                // we prolly need to do something here
            }
        }
        
        private void OnTriggerEnter(Collider other)
        {
            // These should only play during Gameplay
            if (GameStateManager.Instance.GetCurrentGameState() != Types.GameState.Gameplay) { return;}
            // ensure the thing that interacted with us is the player
            if (!other.CompareTag("Player")) { return; }
            
            // check if we are event allowed to fire thus.. (hve we already?)
            if (HorrorEventManager.Instance.CheckIfHorrorEventTriggered(this))
            {
                DebugUtils.Log($"[HorrorEvent] Horror Event with ID: {_horrorEventId} has already been triggered, skipping effects.");
                Destroy(gameObject);
                return;
            }
            
            // Now we still trigger all of the effects
            foreach (HorrorEventData data in eventEffects)
            {
                if (data.EventParticles != null) { data.EventParticles.Play(); }
                if (data.EventSound != null) { UAudio.Instance.PlayClip(data.EventSound, data.gameobj); }
            }
            
            // now that all effects have been trigger, we can disable this object (and notify the Manager that we have been triggered, so that it can save this state for future playthroughs)
            EventBroadcaster.Broadcast_OnHorrorEventTriggered(this);
        }

        private void OnTriggerExit(Collider other)
        {
            // These should only play during Gameplay
            if (GameStateManager.Instance.GetCurrentGameState() != Types.GameState.Gameplay) { return;}
            // ensure the thing that interacted with us is the player
            if (!other.CompareTag("Player")) { return; }
        }
        
        
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            BoxCollider boxCollider = GetComponent<BoxCollider>();
            if (boxCollider != null)
            {
                Gizmos.color = editorGizmoColor;
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(boxCollider.center, boxCollider.size);
            }

            if (boxCollider != null)
            {
                Vector3 labelPosition = transform.position + Vector3.up * (boxCollider.size.y / 2 + 0.5f);
                UnityEditor.Handles.Label(labelPosition, $"Name: {eventName}",
                    new GUIStyle()
                    {
                        alignment = TextAnchor.MiddleCenter,
                        normal = new GUIStyleState() { textColor = Color.white },
                        fontSize = 24,
                        fontStyle = FontStyle.Bold
                    });
            }

            // Reset to world space before drawing connection gizmos
            Gizmos.matrix = Matrix4x4.identity;

            foreach (GameObject connectedObject in eventEffects.ConvertAll(effect => effect.gameobj))
            {
                if (connectedObject != null)
                {
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawLine(transform.position, connectedObject.transform.position);

                    Gizmos.color = Color.cyan;
                    Gizmos.DrawSphere(connectedObject.transform.position, 0.25f);
                }
            }
        }
#endif
    }
}
