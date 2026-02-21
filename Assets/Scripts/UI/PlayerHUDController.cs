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
        // Panel
        private Image _hudOverlay;

        // Textmeshpro Text ui

        private TMP_Text _hudInteractionPromptText;
        private TMP_Text _hudItemNameText;

        // scroll view for the description text
        private ScrollRect _hudItemDescriptionScrollRect;
        private TMP_Text _hudItemDescriptionText;

        // notificationText is handled via the NotificationController
        private TMP_Text _hudNotificationText; public TMP_Text GetNotificationText() { return _hudNotificationText; }

        private IInteractable _hoveredInteractable;
        private bool _isInspecting;


        protected override void RegisterSubscriptions()
        {
            base.RegisterSubscriptions();
            TrackSubscription(() => EventBroadcaster.OnBeganHoverInteractable += OnInteractHoverStarted,
                () => EventBroadcaster.OnBeganHoverInteractable -= OnInteractHoverStarted);
            TrackSubscription(() => EventBroadcaster.OnEndedHoverInteractable += OnInteractHoverEnded,
                () => EventBroadcaster.OnEndedHoverInteractable -= OnInteractHoverEnded);
            TrackSubscription(() => EventBroadcaster.OnWorldClockHourChanged += OnWorldClockHourChanged,
                () => EventBroadcaster.OnWorldClockHourChanged -= OnWorldClockHourChanged);
        }

        private void Start()
        {
            _hudCanvas = GetComponent<Canvas>();
            _hudCrosshair = transform.Find("CrossHair").GetComponent<Image>();
            _hudOverlay = transform.Find("Overlay").GetComponent<Image>();
            _hudInteractionPromptText = transform.Find("InteractionPrompt").GetComponent<TMP_Text>();
            _hudItemNameText = transform.Find("ItemName").GetComponent<TMP_Text>();

            // ItemDescription is now a Scroll View root (with ScrollRect)
            Transform itemDescRoot = transform.Find("ItemDescription");
            _hudItemDescriptionScrollRect = itemDescRoot.GetComponent<ScrollRect>();
            // TMP text lives under Content basically
             _hudItemDescriptionText = itemDescRoot.Find("Viewport/Content/ItemDescription").GetComponent<TMP_Text>();

            _hudNotificationText = transform.Find("NotificationText").GetComponent<TMP_Text>();
            _hudNotificationText.gameObject.SetActive(false);
            SetPrompt("");
            SetInspectionText("", "");
            SetInspectionTextVisible(false);
        }

        private void OnInteractHoverStarted(IInteractable interactable)
        {
            //Debug.Log("[HUD] Hover started");
            //Debug.Log($"[HUD] PromptKey = '{interactable.PromptKey.place}.{interactable.PromptKey.id}'");
            //Debug.Log($"[HUD] Prompt = '{TextDB.GetPrompt(interactable.PromptKey.place, interactable.PromptKey.id)}'");

            _hoveredInteractable = interactable;

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
            _hoveredInteractable = null;
            SetPrompt("");
        }

        private void OnWorldClockHourChanged(int clockHour)
        {
            TextDB.SetCurrentAct(clockHour);

            if (_isInspecting)
            {
                HandleInspection();
                return;
            }

            if (_hoveredInteractable != null)
            {
                OnInteractHoverStarted(_hoveredInteractable);
            }
            else
            {
                SetPrompt("");
            }
        }

        protected override void OnGameStateChanged(Types.GameState newstate)
        {
            _isInspecting = (newstate == Types.GameState.Inspecting);

            switch (newstate)
            {
                case Types.GameState.Gameplay:
                    SetInspectionText("", "");
                    SetInspectionTextVisible(false);
                    if (_hudCrosshair != null) { _hudCrosshair.enabled = true; }
                    ShowHUD(true);
                    break;
                case Types.GameState.Cutscene:
                    if (_hudCrosshair != null) { _hudCrosshair.enabled = false; }
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
            ShowHUD(true);
            InspectableObject obj = InspectionSystem.Instance.GetCurrentInspectedObject();
            if (obj == null)
            {
                SetInspectionText("", "");
                SetInspectionTextVisible(false);
                return;
            }

            // pull name / desc from CSV name / desc fields using the inspectable�s row key
            string name = TextDB.GetName(obj.RowKey.place, obj.RowKey.id);
            string desc = TextDB.GetDesc(obj.RowKey.place, obj.RowKey.id);

            // blank means "not inspectable / show nothing"
            if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(desc))
            {
                SetInspectionText("", "");
                SetInspectionTextVisible(false);
            }
            else
            {
                SetInspectionTextVisible(true);
                SetInspectionText(name, desc);
            }

            if (_hudCrosshair != null) { _hudCrosshair.enabled = false; }
            SetPrompt("");
        }
        
        public void RefreshInspectionText()
        {
            if (_isInspecting)
            {
                HandleInspection();
            }
        }

        private void ShowHUD(bool show)
        {
            if (_hudCanvas != null) _hudCanvas.enabled = show;
        }

        private void SetPrompt(string s)
        {
            if (_hudInteractionPromptText == null) return;

            _hudInteractionPromptText.text = s ?? "";
            _hudInteractionPromptText.gameObject.SetActive(!string.IsNullOrEmpty(_hudInteractionPromptText.text));
        }

        private void SetInspectionTextVisible(bool visible)
        {
            if (_hudItemNameText != null) _hudItemNameText.gameObject.SetActive(visible);
            if (_hudItemDescriptionScrollRect != null) _hudItemDescriptionScrollRect.gameObject.SetActive(visible);
            if (_hudOverlay != null) _hudOverlay.enabled = visible;
        }
        private void SetInspectionText(string name, string desc)
        {
            if (_hudItemNameText != null) _hudItemNameText.text = name ?? "";
            if (_hudItemDescriptionText != null) _hudItemDescriptionText.text = desc ?? "";

            if (_hudItemDescriptionScrollRect != null)
            {
                Canvas.ForceUpdateCanvases(); // layoout update force - wanna put scroll back on top
                _hudItemDescriptionScrollRect.verticalNormalizedPosition = 1f;
            }
        }
    }
}
