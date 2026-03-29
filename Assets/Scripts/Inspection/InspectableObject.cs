using System;
using System.Collections;
using Managers;
using Player;
using Unity.Cinemachine;
using UnityEngine;
using Types = System.Types;
using Interaction;

namespace Inspection
{
    /// <summary>
    /// This class can be attached to any GameObject to make it inspectable in the inspection system.
    ///
    /// All of the properties setable are optional, but will allow for more detailed inspection if filled out.
    /// </summary>
    ///
    
    // WIll soon inherit from the InteractableObject class / Interactable system Interface
    public class InspectableObject : EventSubscriberBase, IInteractable
    {

        [Header("Text Keys (CSV row pointers)")]
        [SerializeField, Tooltip("Row key that contains name / description fields for inspection UI")]
        protected TextKey rowKey;
        [SerializeField, Tooltip("Row key that contains the prompt field for hover interaction text (optional as needed)")]
        protected TextKey promptKey;

        [SerializeField] private int requiredHour = -1; // -1 means no time restriction
        
        // internal 
        private MeshRenderer[] _meshRenderers;
        private Collider[] _objColliders;

        // audio stuff
        [Header("Audio for Inspect / Un-inspect")]
        [SerializeField] private AudioClip pickupSfx;
        [SerializeField] private AudioClip putDownSfx;
        [SerializeField] private float sfxVolume = 1f;
        [SerializeField] private float sfxPitchVariation = 0.05f;

        // Getters
        public TextKey RowKey => rowKey;
        public TextKey PromptKey => promptKey;

        // Time Lock
        protected bool timeLock = false;

        // Interface Implementation
        public bool CanInteract(Interactor interactor)
        {
            // Handle the clock being at 6pm without player having read the letters.
            if (GameStateManager.Instance.GetCurrentWorldLocation() == Types.WorldLocation.Bedroom && 
            PlayerStats.Instance.GetPlayerStats().GetCurrentMentalHealth() <= 25)  // This correlates to 6pm exactly
            {
                if(GameStateManager.Instance.GetCurrentWorldClockHour() == 1 && 
                (!LetterManager.Instance.GetHasReadAct1ResearcherLetter() ||
                !LetterManager.Instance.GetHasReadAct1FriendLetter()))
                {
                    timeLock = true;
                    return true;
                }
                if(GameStateManager.Instance.GetCurrentWorldClockHour() == 2 && 
                (!LetterManager.Instance.GetHasReadAct2ResearcherLetter() ||
                !LetterManager.Instance.GetHasReadAct2FriendLetter()))
                {
                    timeLock = true;
                    return true;
                }
                if(GameStateManager.Instance.GetCurrentWorldClockHour() == 3 && 
                (!LetterManager.Instance.GetHasReadAct3ResearcherLetter() ||
                !LetterManager.Instance.GetHasReadAct3FriendLetter()))
                {
                    timeLock = true;
                    return true;
                }
            }
            // Default case in the situation where nothing is wrong
            timeLock = false;
            return true;
        }

        public void Interact(Interactor interactor)
        {

            //if (timeLock & gameObject.GetComponent)
            if (timeLock)
            {
                
                // Play notification if player has not read letters by 6pm
                Types.NotificationData data = new(
                    duration: 1.0f, 
                    messageKey: new TextKey(),
                    messageOverride: "Can’t. Too tired...\n\nShould check for letters...",
                    shouldOnlyShowOnce: false
                );
                data.Send();
                return;
                /*
                Types.NotificationData data = new(
                    duration: 1, 
                    messageKey: new TextKey { place = "prompt", id = "letters_not_read" },
                    messageOverride: "",
                    shouldOnlyShowOnce:false
                );
                data.Send();
                return;
                */
            }
            // Default inspect case if nothing is wrong
            if (pickupSfx != null)
                UAudio.Instance.PlayClip(pickupSfx, gameObject, sfxVolume, sfxPitchVariation);

            InspectionSystem.Instance.StartInspection(gameObject);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            
            _meshRenderers = GetComponentsInChildren<MeshRenderer>();
            _objColliders = GetComponentsInChildren<Collider>();
        }

        protected override void OnWorldClockTicked(int newHour)
        {

            if (newHour >= requiredHour)
            {
                for (int i = 0; i < _meshRenderers.Length; i++)
                {
                    _meshRenderers[i].enabled = true;
                }
                for (int i = 0; i < _objColliders.Length; i++)
                {
                    _objColliders[i].enabled = true;
                }
            } else
            {
                for (int i = 0; i < _meshRenderers.Length; i++)
                {
                    _meshRenderers[i].enabled = false;
                }
                for (int i = 0; i < _objColliders.Length; i++)
                {
                    _objColliders[i].enabled = false;
                }
            }
        }
        public virtual void OnInspectionFinished()
        {
            // Custom logic that can run once the inspection has been completed fully
        }

        public virtual void OnReturnedToOriginalPosition()
        {
            // Custom logic that can run once the inspected object has been returned to its original position
            // this is the VERY end of the inspection{
            if (putDownSfx != null)
                UAudio.Instance.PlayClip(putDownSfx, gameObject, sfxVolume, sfxPitchVariation);
        }

    }
}
