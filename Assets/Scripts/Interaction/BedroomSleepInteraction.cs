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
                    messageKey: new TextKey { place = "prompt", id = "tracker_not_off" },
                    messageOverride: "",
                    shouldOnlyShowOnce:false
                );
                data.Send();
                return;
            }
            
            
            // Edge cases regarding allowing sleep
            if (GameStateManager.Instance.GetCurrentWorldClockHour() == 1)
            {
                // Ensure both letters have been read before allowing the player to sleep
                if (LetterManager.Instance.GetHasReadAct1ResearcherLetter() &&
                    LetterManager.Instance.GetHasReadAct1FriendLetter())
                {
                    // we are good to sleep
                }
                else
                {
                    // else we need to show a notification as to why they cant sleep
                    // otherwise, we cant return yet
                    Types.NotificationData data = new(
                        duration: 1, 
                        messageKey: new TextKey { place = "prompt", id = "letters_not_read" },
                        messageOverride: "",
                        shouldOnlyShowOnce:false
                    );
                    data.Send();
                    return;
                }
                
            }
            if (GameStateManager.Instance.GetCurrentWorldClockHour() == 2)
            {
                // Ensure both letters have been read before allowing the player to sleep
                if (LetterManager.Instance.GetHasReadAct2ResearcherLetter() &&
                    LetterManager.Instance.GetHasReadAct2FriendLetter())
                {
                    // we are good to sleep
                }
                else
                {
                    // else we need to show a notification as to why they cant sleep
                    // otherwise, we cant return yet
                    Types.NotificationData data = new(
                        duration: 1, 
                        messageKey: new TextKey { place = "prompt", id = "letters_not_read" },
                        messageOverride: "",
                        shouldOnlyShowOnce:false
                    );
                    data.Send();
                    return;
                }
            }
            if (GameStateManager.Instance.GetCurrentWorldClockHour() == 3)
            {
                // Ensure both letters have been read before allowing the player to sleep
                if (LetterManager.Instance.GetHasReadAct3ResearcherLetter() &&
                    LetterManager.Instance.GetHasReadAct3FriendLetter())
                {
                    // we are good to sleep
                }
                else
                {
                    // else we need to show a notification as to why they cant sleep
                    // else we need to show a notification as to why they cant sleep
                    // otherwise, we cant return yet
                    Types.NotificationData data = new(
                        duration: 1, 
                        messageKey: new TextKey { place = "prompt", id = "letters_not_read" },
                        messageOverride: "",
                        shouldOnlyShowOnce:false
                    );
                    data.Send();
                    return;
                }
            }
            
            // if its act 4 (the finale), we dont wanna allow sleeping anymore
            if (GameStateManager.Instance.GetCurrentWorldClockHour() >= 4)
            {
                // otherwise, we cant return yet
                Types.NotificationData data = new(
                    duration: 1, 
                    messageKey: new TextKey { place = "prompt", id = "cant_sleep" },
                    messageOverride: "",
                    shouldOnlyShowOnce:false
                );
                data.Send();
                return;
            }
            // we are good to sleep!
            GetComponent<Collider>().enabled = false;

            const int timeToFadeOut = 2; 
            const int fadeInDuration = 2;
            //Types.ScreenFadeData fadeData = new Types.ScreenFadeData(fadeInDuration:fadeInDuration, 1.5f, fadeOutDuration:timeToFadeOut, null, FadeOutCompleted, FadeDurationCompleted);
            Types.ScreenFadeSceneTransitionData sceneTransitionData = new Types.ScreenFadeSceneTransitionData(fadeOutDuration:timeToFadeOut, fadeInDuration:1.5f, sceneName, null, FadeOutCompleted, FadeDurationCompleted);
            
            sceneTransitionData.Send();
            EventBroadcaster.Broadcast_GameStateChanged(Types.GameState.Cutscene);
        }
        
        private void FadeOutCompleted()
        {
            // Display the notification here!
            Types.NotificationData data = new(
                duration: 3, 
                messageKey: new TextKey { place = "cutscene", id = "act1" }
            );
            data.Send();
        }

        private void FadeDurationCompleted()
        {

            EventBroadcaster.Broadcast_GameStateChanged(Types.GameState.Gameplay);
        }
    }
}
