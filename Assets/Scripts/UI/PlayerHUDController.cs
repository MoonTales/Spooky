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


        protected override void RegisterSubscriptions()
        {
            base.RegisterSubscriptions();
            TrackSubscription(() => EventBroadcaster.OnBeganHoverInteractable += OnInteractHoverStarted,
                () => EventBroadcaster.OnBeganHoverInteractable -= OnInteractHoverStarted);
            TrackSubscription(() => EventBroadcaster.OnEndedHoverInteractable += OnInteractHoverEnded,
                () => EventBroadcaster.OnEndedHoverInteractable -= OnInteractHoverEnded);
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

            SetPrompt("");
            SetInspectionText("", "");
        }

        private void OnInteractHoverStarted(IInteractable interactable)
        {
            Debug.Log("[HUD] Hover started");
            Debug.Log($"[HUD] PromptKey = '{interactable.PromptKey.place}.{interactable.PromptKey.id}'");
            Debug.Log($"[HUD] Prompt = '{TextDB.GetPrompt(interactable.PromptKey.place, interactable.PromptKey.id)}'");


            if (interactable == null)
            {
                SetPrompt("");
                return;
            }

            // pull prompt string from CSV prompt field
            string prompt = TextDB.GetPrompt(interactable.PromptKey.place, interactable.PromptKey.id);

            if (!string.IsNullOrEmpty(prompt))
                SetPrompt(prompt);
            else
                SetPrompt("");
        }

        private void OnInteractHoverEnded()
        {
            SetPrompt("");
        }

        protected override void OnGameStateChanged(Types.GameState newstate)
        {
            switch (newstate)
            {
                case Types.GameState.Gameplay:
                    SetInspectionText("", "");
                    if (_hudCrosshair != null){ _hudCrosshair.enabled = true;}
                    ShowHUD(true);
                    break;
                case Types.GameState.Cutscene:
                    if (_hudCrosshair != null){ _hudCrosshair.enabled = false;}
                    break;
                case Types.GameState.MainMenu:
                    ShowHUD(false);
                    break;
                case Types.GameState.Inspecting:
                    HandleInspection();
                    break;
                case Types.GameState.Paused:
                    ShowHUD(false);
                    break;
            }
        }

        private void HandleInspection()
        {
            InspectableObject obj = InspectionSystem.Instance.GetCurrentInspectedObject();
            if (obj == null)
            {
                SetInspectionText("", "");
                return;
            }

            // pull name / desc from CSV name / desc fields using the inspectable�s row key
            string name = TextDB.GetName(obj.RowKey.place, obj.RowKey.id);
            string desc = TextDB.GetDesc(obj.RowKey.place, obj.RowKey.id);

            // blank means "not inspectable / show nothing"
            if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(desc))
                SetInspectionText("", "");
            else
                SetInspectionText(name, desc);

            if (_hudCrosshair != null) _hudCrosshair.enabled = false;
            SetPrompt("");
        }

        private void ShowHUD(bool show)
        {
            if (_hudCanvas != null) _hudCanvas.enabled = show;
        }

        private void Update()
        {
            if (_hudSanityStateText)
                _hudSanityStateText.text = PlayerStats.Instance.GetPlayerStats().GetPlayerMentalState().ToString();

            if (_hudSanityValueText)
                _hudSanityValueText.text = Mathf.RoundToInt(PlayerStats.Instance.GetPlayerStats().GetCurrentMentalHealth()).ToString();
        }

        private void SetPrompt(string s)
        {
            if (_hudInteractionPromptText == null) return;

            _hudInteractionPromptText.text = s ?? "";

            // If you want the prompt object to fully hide when blank:
            _hudInteractionPromptText.gameObject.SetActive(!string.IsNullOrEmpty(_hudInteractionPromptText.text));
        }

        private void SetInspectionText(string name, string desc)
        {
            if (_hudItemNameText != null) _hudItemNameText.text = name ?? "";
            if (_hudItemDescriptionText != null) _hudItemDescriptionText.text = desc ?? "";
        }
    }
}

