using System;
using Managers;
using Player;
using UnityEngine;
using Types = System.Types;

namespace Interaction
{
    /// <summary>
    /// Class for handling waking up from the nightmare with a "good wakeup"
    /// </summary>
    public class NightmareAwakenInteraction : MonoBehaviour, IInteractable
    {
        // Start is called once before the first execution of Update after the MonoBehaviour is created

        [SerializeField] private bool bShouldResetZoneId = true;
        [SerializeField] private SceneField sceneName;
    
        [Header("Text Keys (CSV row pointers)")]
        [SerializeField] private TextKey promptTextKey;
        public TextKey PromptKey => promptTextKey;
        public bool CanInteract(Interactor interactor)
        {
            return true;
        }

        public void Interact(Interactor interactor)
        {
            
            // check if we have atleast 1 drawing in the inventory
            if (PlayerInventory.Instance.GetCurrentDrawingsThisNight() > 0)
            {
                SceneSwapper.Instance.SwapScene(sceneName);
                if(bShouldResetZoneId){GameStateManager.Instance.SetCurrentZoneId(-1);}

                return;
            }
            
            // otherwise, we cant return yet
            Types.NotificationData data = new(
                duration: 1, 
                messageKey: new TextKey(),
                messageOverride: "You feel like you haven't done enough tonight. Maybe you should explore a bit more?"
            );
            data.Send();
            
            

        }
    
    }
}
