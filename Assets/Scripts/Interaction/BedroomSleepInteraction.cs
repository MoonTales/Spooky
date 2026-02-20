using System;
using Managers;
using UnityEngine;
using Types = System.Types;
namespace Interaction
{
    public class BedroomSleepInteraction : MonoBehaviour, IInteractable
    {
        
        [SerializeField] private SceneField sceneName;
        [Header("Text Keys (CSV row pointers)")]
        [SerializeField] private TextKey promptTextKey;
        public TextKey PromptKey => promptTextKey;
        public bool CanInteract(Interactor interactor)
        {
            // we can only interact (currently) if the Sleep Tracker has been turning off
            //bool canInteract = !SleepTrackerManager.Instance.GetIsSleepTrackerActive();
            return true;
        }

        public void Interact(Interactor interactor)
        {
            // we will (for now), fade to black, and then load the next scene (which will be the nightmare scene)
            if (SleepTrackerManager.Instance.GetIsSleepTrackerActive())
            {
                // otherwise, we cant return yet
                Types.NotificationData data = new(
                    duration: 1, 
                    messageKey: new TextKey(),
                    messageOverride: "You feel like there is something you need to do before you can sleep. Maybe you should explore a bit more?"
                );
                data.Send();
                return;
            }
        
            // we are good to sleep!
            GetComponent<Collider>().enabled = false;
            const int timeToFadeOut = 5; 
            Types.ScreenFadeData fadeData = new Types.ScreenFadeData(fadeInDuration:2, 2, fadeOutDuration:timeToFadeOut, null, FadeOutCompleted);
            fadeData.Send();
            EventBroadcaster.Broadcast_GameStateChanged(Types.GameState.Cutscene);
        }

        private void FadeOutCompleted()
        {
            SceneSwapper.Instance.SwapScene(sceneName);
            EventBroadcaster.Broadcast_GameStateChanged(Types.GameState.Gameplay);
            GameStateManager.Instance.SetCurrentZoneId(-1);
        }
    }
}
