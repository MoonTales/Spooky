using System;
using Player;
using TMPro;
using UnityEngine;
using Types = System.Types;

namespace UI
{
    public class PlayerHUDController : Singleton<PlayerHUDController>
    {
    
        // Internal References to the HUD
        private Canvas _hudCanvas;
        // textmeshpro Text ui
        private TMP_Text _hudSanityStateText;
        private TMP_Text _hudSanityValueText;
        private TMP_Text _hudInteractionPromptText;


        protected override void RegisterSubscriptions()
        {
            base.RegisterSubscriptions();
            TrackSubscription(() => EventBroadcaster.OnBeganHoverInteractable += OnInteractHoverStarted,
                () => EventBroadcaster.OnBeganHoverInteractable -= OnInteractHoverStarted);
            TrackSubscription(() => EventBroadcaster.OnEndedHoverInteractable += OnInteractHoverEnded,
                () => EventBroadcaster.OnEndedHoverInteractable -= OnInteractHoverEnded);
        }
        
        private void OnInteractHoverStarted(IInteractable interactable)
        {
            // Show interaction prompt on HUD
            Debug.Log("Show interaction prompt for: " + interactable.Prompt);
            _hudInteractionPromptText.text = "[F] " + interactable.Prompt;
        }
        private void OnInteractHoverEnded()
        {
            // Hide interaction prompt on HUD
            Debug.Log("Hide interaction prompt");
            _hudInteractionPromptText.text = "";
        }
        private void Start()
        {
            _hudCanvas = GetComponent<Canvas>();
            _hudSanityStateText = transform.Find("SanityState").GetComponent<TMP_Text>();
            _hudSanityValueText = transform.Find("SanityValue").GetComponent<TMP_Text>();
            _hudInteractionPromptText = transform.Find("InteractionPrompt").GetComponent<TMP_Text>();
        }
        

        protected override void OnGameStateChanged(Types.GameState newstate)
        {
            switch (newstate)
            {
                case Types.GameState.Gameplay:
                    ShowHUD(true);
                    break;
                case Types.GameState.Cutscene:
                    ShowHUD(false);
                    break;
                case Types.GameState.MainMenu:
                    ShowHUD(false);
                    break;
                case Types.GameState.Inspecting:
                    HandleInspection();
                    break;
            }
        }

        private void HandleInspection()
        {
            //For momo: custom logic here, since this automatically gets called the second we start inspection
            ShowHUD(false);
        }
    
        private void ShowHUD(bool show)
        {
            if (_hudCanvas != null){_hudCanvas.enabled = show;}
        }

        private void Update()
        {
            if (_hudSanityStateText)
            {
                _hudSanityStateText.text = PlayerStats.Instance.GetPlayerStats().GetPlayerMentalState().ToString();
            }
            if (_hudSanityValueText)
            {
                _hudSanityValueText.text = Mathf.RoundToInt(PlayerStats.Instance.GetPlayerStats().GetCurrentMentalHealth()).ToString();
            }
        }
    }
}


