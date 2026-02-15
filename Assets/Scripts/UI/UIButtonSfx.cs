using Managers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
            if (playHover)
            {
                PlaySfx(hoverSfx);
            }
        }

        private void OnButtonClicked()
        {
            if (playClick)
            {
                PlaySfx(clickSfx);
            }
        }

        private static void PlaySfx(AudioManager.SfxId sfxId)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySfx(sfxId);
            }
        }
    }
}
