using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Types = System.Types;

namespace Cutscenes
{
    public class CutsceneSignalReceiver : MonoBehaviour, INotificationReceiver
    {
        
        public void OnNotify(Playable origin, INotification notification, object context)
        {
    
            // Use 'as' instead of direct cast for safety
            DialogueMarker dialogueMarker = notification as DialogueMarker;
            if (dialogueMarker != null)
            {
                PlayDialogue(dialogueMarker);
            }
        }
        
        // using this idea, we can connect to any function we want

        private void PlayDialogue(DialogueMarker dialogueMarker)
        {

            Types.NotificationData data = new(
                duration: dialogueMarker.displayDuration,
                messageKey: new TextKey { place = "cutscene", id = "op" }
                //messageOverride: $"THIS WAS CALLED FROM A CUTSCENE!!!"
            );
            data.Send();
        }
    }
}