using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Types = System.Types;

namespace Cutscenes
{
    public class CutsceneSignalReceiver : MonoBehaviour, INotificationReceiver
    {
        
        private void Awake()
        {
            Debug.Log("[CutsceneSignalReceiver] Awake - component exists on: " + gameObject.name);
        }
        
        public void OnNotify(Playable origin, INotification notification, object context)
        {
            DebugUtils.Log($"[CutsceneSignalReceiver] Received notification: {notification.GetType().Name}");
    
            // Use 'as' instead of direct cast for safety
            DialogueMarker dialogueMarker = notification as DialogueMarker;
            if (dialogueMarker != null)
            {
                PlayDialogue(dialogueMarker);
            }
        }

        private void PlayDialogue(DialogueMarker dialogueMarker)
        {
            
            Types.NotificationData data = new(
                duration: dialogueMarker.displayDuration, 
                messageKey: new TextKey { place = "Notifications", id = "CollectedDrawingSuccess"},
                messageOverride: $"THIS WAS CALLED FROM A CUTSCENE!!!"
            );
            data.Send();
        }
    }
}