using Managers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Types = System.Types;

namespace UI
{
    [DisallowMultipleComponent]         // only one UI SFX component per object
    [RequireComponent(typeof(Button))]  // ensure object actually has a button
    public class UIButtonSfx : MonoBehaviour, IPointerEnterHandler
    {
        [SerializeField] private bool playHover = true;
        [SerializeField] private bool playClick = true;
        [SerializeField] private AudioManager.SfxId hoverSfx = AudioManager.SfxId.UIHover;
        [SerializeField] private AudioManager.SfxId clickSfx = AudioManager.SfxId.Flashlight;
        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            if (_button != null)
            {
                _button.onClick.AddListener(OnButtonClicked);
            }
        }

        private void OnDisable()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(OnButtonClicked);
            }
        }

        public void Configure(bool enableHover, bool enableClick)
        {
            playHover = enableHover;
            playClick = enableClick;
        }

        public static UIButtonSfx Ensure(Button button, bool enableHover, bool enableClick)
        {
            if (button == null)
            {
                return null;
            }

            UIButtonSfx buttonSfx = button.GetComponent<UIButtonSfx>();
            if (buttonSfx == null)
            {
                buttonSfx = button.gameObject.AddComponent<UIButtonSfx>();
            }

            buttonSfx.Configure(enableHover, enableClick);
            return buttonSfx;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            //ensure the button is enabled
            if (!_button.IsActive() || !_button.interactable) { return; }
            if (playHover && CanPlayHover())
            {
                PlaySfx(hoverSfx);
            }
        }

        private void OnButtonClicked()
        {
            if (playClick && CanPlayClick())
            {
                PlaySfx(clickSfx);
            }
        }

        private static void PlaySfx(AudioManager.SfxId sfxId)
        {
            if (AudioManager.Instance != null)
            {
                if (sfxId == AudioManager.SfxId.UIHover)
                {
                    AudioManager.Instance.PlayUiHoverSfx();
                }
                else
                {
                    AudioManager.Instance.PlaySfx(sfxId);
                }
            }
        }

        private static bool CanPlayHover()
        {
            return GameStateManager.Instance == null
                   || (GameStateManager.Instance.GetCurrentGameState() != Types.GameState.Cutscene
                       && !IsMainMenuFadeInProgress());
        }

        private static bool CanPlayClick()
        {
            return !IsMainMenuFadeInProgress();
        }

        private static bool IsMainMenuFadeInProgress()
        {
            if (!ScreenFadeManager.IsFadeInProgress)
            {
                return false;
            }

            if (GameStateManager.Instance == null)
            {
                return false;
            }

            return GameStateManager.Instance.GetCurrentGameState() == Types.GameState.MainMenu;
        }
    }
}
