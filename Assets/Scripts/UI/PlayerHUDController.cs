using System;
using System.Collections;
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
        public CursorInScrollView scrollViewChecker;
        // Internal References to the HUD
        private Canvas _hudCanvas;
        // Crosshair
        private Image _hudCrosshair; public void SetCrosshairVisibility(bool visible) { Color tempColor = _hudCrosshair.color; tempColor.a = visible ? 50f : 0f;
            _hudCrosshair.color = tempColor; }
        // Panel
        private Image _hudOverlay;

        // Textmeshpro Text ui

        private TMP_Text _hudInteractionPromptText;
        private TMP_Text _hudItemNameText;

        // scroll view for the description text
        private ScrollRect _hudItemDescriptionScrollRect;
        private TMP_Text _hudItemDescriptionText;

        // notificationText is handled via the NotificationController

        private IInteractable _hoveredInteractable;
        private bool _isInspecting;
        
        // Hookup for the display System (inventory)
        //private TMP_Text _InventoryCountText;
        private Image _InventoryIcon_1;
        private Image _InventoryIcon_2;
        private Image _InventoryIcon_3;
        private Color _IconCollectedColor = new Color(1f, 1f, 1f, 1f);
        private Color _IconUncollectedColor = new Color(0.68f, 0.68f, 0.68f, 0.4f);
        
        [SerializeField] private Sprite EmptyIcon; 
        [SerializeField] private Sprite CollectedIcon;
        
        
        // 


        protected override void RegisterSubscriptions()
        {
            base.RegisterSubscriptions();
            TrackSubscription(() => EventBroadcaster.OnBeganHoverInteractable += OnInteractHoverStarted,
                () => EventBroadcaster.OnBeganHoverInteractable -= OnInteractHoverStarted);
            TrackSubscription(() => EventBroadcaster.OnEndedHoverInteractable += OnInteractHoverEnded,
                () => EventBroadcaster.OnEndedHoverInteractable -= OnInteractHoverEnded);
            TrackSubscription(() => EventBroadcaster.OnWorldClockHourChanged += OnWorldClockHourChanged,
                () => EventBroadcaster.OnWorldClockHourChanged -= OnWorldClockHourChanged);
            TrackSubscription(()=> EventBroadcaster.OnDrawingCollected += OnDrawingCollected,
                () => EventBroadcaster.OnDrawingCollected -= OnDrawingCollected);
            TrackSubscription(()=> EventBroadcaster.OnWorldLocationChangedEvent += OnWorldLocationChanged,
                () => EventBroadcaster.OnWorldLocationChangedEvent -= OnWorldLocationChanged);
            TrackSubscription(()=>EventBroadcaster.OnAllAllowedDrawingsForNightCollected += AllDrawingsCollected,
                () => EventBroadcaster.OnAllAllowedDrawingsForNightCollected -= AllDrawingsCollected);
        }

        private void AllDrawingsCollected()
        {
            // this is called when the player collects the 3rd (last) drawings for a night
            Types.NotificationData data = new(
                duration: 2.0f, 
                messageKey: new TextKey { place = "Notifications", id = "AllDrawingsCollected" },
                messageOverride: "Need to go back. Can't lose these.",
                shouldOnlyShowOnce: false
            );
            data.Send();
        }

        private void OnWorldLocationChanged(Types.WorldLocation newLocation)
        {
            if (newLocation == Types.WorldLocation.Bedroom)
            {
                // if we are returning to the bedroom, we want to reset the inventory display
                
                HideInventory();
            }
        }

        private void OnDrawingCollected(int drawingid)
        {
            StartCoroutine(OnDrawingCollectedFade(drawingid));
        }
        
        public void ShowInventory()
        {
            int currentDrawings = PlayerInventory.Instance.GetCurrentDrawingsThisNight();
            StartCoroutine(FadeInInventory(currentDrawings, fadeDuration: 0.5f));
        }
        
        public void HideInventory()
        {
            StartCoroutine(FadeOutInventory(fadeDuration: 0.5f, disableAfter: true));
        }

        private IEnumerator OnDrawingCollectedFade(int drawingId)
        {
            int currentDrawings = PlayerInventory.Instance.GetCurrentDrawingsThisNight();

            yield return StartCoroutine(FadeInInventory(currentDrawings, fadeDuration: 0.5f, drawingId));
            yield return new WaitForSeconds(3f);
            yield return StartCoroutine(FadeOutInventory(fadeDuration: 0.5f, disableAfter: true, drawingId));
        }
        
        private Color GetIconColor(int slot, int currentDrawings)
        {
            return currentDrawings >= slot ? _IconCollectedColor : _IconUncollectedColor;
        }

        private IEnumerator FadeIcons(Color target1, Color target2, Color target3, float duration)
        {
            Color start1 = _InventoryIcon_1.color;
            Color start2 = _InventoryIcon_2.color;
            Color start3 = _InventoryIcon_3.color;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                _InventoryIcon_1.color = Color.Lerp(start1, target1, t);
                _InventoryIcon_2.color = Color.Lerp(start2, target2, t);
                _InventoryIcon_3.color = Color.Lerp(start3, target3, t);
                yield return null;
            }
        }

        private Color WithZeroAlpha(Color c) => new Color(c.r, c.g, c.b, 0f);

        private IEnumerator FadeInInventory(int currentDrawings, float fadeDuration, int drawingId = -1)
        {
            SetIconsEnabled(true);

            // EDGE CASE OVER-RIDE:
            // if we picked up drawing 0 (the tutorial drawing), we will ONLY apply stuff to the first icon,
            // and we will ignore the currentDrawings count.
            if (drawingId == 0)
            {
                _InventoryIcon_2.enabled = false;
                _InventoryIcon_3.enabled = false;
            }
            // Start from fully transparent
            _InventoryIcon_1.color = WithZeroAlpha(GetIconColor(1, currentDrawings));
            _InventoryIcon_2.color = WithZeroAlpha(GetIconColor(2, currentDrawings));
            _InventoryIcon_3.color = WithZeroAlpha(GetIconColor(3, currentDrawings));
            // We need to set all of the icons to be either the collected or not coollected sprites
            _InventoryIcon_1.sprite = currentDrawings >= 1 ? CollectedIcon : EmptyIcon;
            _InventoryIcon_2.sprite = currentDrawings >= 2 ? CollectedIcon : EmptyIcon;
            _InventoryIcon_3.sprite = currentDrawings >= 3 ? CollectedIcon : EmptyIcon;

            yield return StartCoroutine(FadeIcons(
                GetIconColor(1, currentDrawings),
                GetIconColor(2, currentDrawings),
                GetIconColor(3, currentDrawings),
                fadeDuration
            ));
        }

        private IEnumerator FadeOutInventory(float fadeDuration, bool disableAfter = true, int drawingId = -1)
        {
            if (_InventoryIcon_1 == null || _InventoryIcon_2 == null || _InventoryIcon_3 == null)
            {
                yield break; // safety check
            }
            yield return StartCoroutine(FadeIcons(
                WithZeroAlpha(_InventoryIcon_1.color),
                WithZeroAlpha(_InventoryIcon_2.color),
                WithZeroAlpha(_InventoryIcon_3.color),
                fadeDuration
            ));

            if (disableAfter) SetIconsEnabled(false);
        }

        private void SetIconsEnabled(bool enabled)
        {
            _InventoryIcon_1.enabled = enabled;
            _InventoryIcon_2.enabled = enabled;
            _InventoryIcon_3.enabled = enabled;
        }

        private void Start()
        {
            _hudCanvas = GetComponent<Canvas>();
            _hudCrosshair = transform.Find("CrossHair").GetComponent<Image>();
            _hudOverlay = transform.Find("Overlay").GetComponent<Image>();
            _hudInteractionPromptText = transform.Find("InteractionPrompt").GetComponent<TMP_Text>();
            _hudItemNameText = transform.Find("ItemName").GetComponent<TMP_Text>();

            _InventoryIcon_1 = transform.Find("Icon_Inventory_1").GetComponent<Image>();
            _InventoryIcon_2 = transform.Find("Icon_Inventory_2").GetComponent<Image>();
            _InventoryIcon_3 = transform.Find("Icon_Inventory_3").GetComponent<Image>();
            
            // ItemDescription is now a Scroll View root (with ScrollRect)
            Transform itemDescRoot = transform.Find("ItemDescription");
            _hudItemDescriptionScrollRect = itemDescRoot.GetComponent<ScrollRect>();
            // TMP text lives under Content basically
             _hudItemDescriptionText = itemDescRoot.Find("Viewport/Content").GetComponent<TMP_Text>();
             
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
                    SetInspectionBGVisible(false);
                    if (_hudCrosshair != null) { _hudCrosshair.enabled = true; }
                    ShowHUD(true);
                    break;
                case Types.GameState.Cutscene:
                    if (_hudCrosshair != null) { _hudCrosshair.enabled = false; }
                    break;
                case Types.GameState.MainMenu:
                    ShowHUD(false);
                    SetInspectionBGVisible(false);
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
            SetInspectionBGVisible(true);
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
            if (_hudItemNameText != null)
            {
                _hudItemNameText.CrossFadeAlpha(visible ? 1f : 0f, 0.5f, true);
               //_hudItemNameText.gameObject.SetActive(visible);
            }

            if (_hudItemDescriptionText != null)
            {
                _hudItemDescriptionText.CrossFadeAlpha(visible ? 1f : 0f, 0.5f, true);
                //_hudItemDescriptionScrollRect.gameObject.SetActive(visible);
            }
        }
        private void SetInspectionBGVisible(bool visible)
        {
            // Experimenting with making this "fade in" rather than just pop in
            if (_hudOverlay != null)
            {
                //_hudOverlay.enabled = visible;
                _hudOverlay.CrossFadeAlpha(visible ? 1f : 0f, 0.5f, true);
            }
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
