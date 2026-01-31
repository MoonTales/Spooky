using System;
using Player;
using TMPro;
using UnityEngine;
using Types = System.Types;
using Inspection;
using UnityEngine.UI;

namespace UI
{
    public class PlayerHUDController : Singleton<PlayerHUDController>
    {
    
        // Internal References to the HUD
        private Canvas _hudCanvas;
        // Crosshair
        private Image _hudCrosshair;
        // Textmeshpro Text ui
        private TMP_Text _hudSanityStateText;
        private TMP_Text _hudSanityValueText;
        private TMP_Text _hudInteractionPromptText;
        private TMP_Text _hudItemNameText;
        private TMP_Text _hudItemDescriptionText;
        // Object
        private InspectableObject obj;


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
            _hudInteractionPromptText.text = "[F] " + interactable.Prompt;
        }
        private void OnInteractHoverEnded()
        {
            // Hide interaction prompt on HUD
            _hudInteractionPromptText.text = "";
        }
        private void Start()
        {
            _hudCanvas = GetComponent<Canvas>();
            _hudCrosshair = transform.Find("CrossHair").GetComponent<Image>();
            _hudSanityStateText = transform.Find("SanityState").GetComponent<TMP_Text>();
            _hudSanityValueText = transform.Find("SanityValue").GetComponent<TMP_Text>();
            _hudInteractionPromptText = transform.Find("InteractionPrompt").GetComponent<TMP_Text>();
            _hudItemNameText = transform.Find("ItemName").GetComponent<TMP_Text>();
            _hudItemDescriptionText = transform.Find("ItemDescription").GetComponent<TMP_Text>();
        }
        

        protected override void OnGameStateChanged(Types.GameState newstate)
        {
            switch (newstate)
            {
                case Types.GameState.Gameplay:
                    _hudItemNameText.text = "";
                    _hudItemDescriptionText.text = "";
                    if (_hudCrosshair != null){_hudCrosshair.enabled = true;}
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
            InspectableObject obj = InspectionSystem.Instance.GetCurrentInspectedObject();
            DebugUtils.Log($"PlayerHUDController: Handling inspection of object '{obj.GetObjectName()}'");

            _hudItemNameText.text = obj.GetObjectName();
            _hudItemDescriptionText.text = obj.GetObjectDescription();

            if (_hudCrosshair != null){_hudCrosshair.enabled = false;}

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


