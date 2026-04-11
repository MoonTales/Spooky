using System;
using Managers;
using Player;
using Player.Camera;
using UI.Main_Menu;
using UnityEngine;
using UnityEngine.UI;
using Types = System.Types;

namespace UI.PauseMenu
{
    public class SettingsController : Singleton<SettingsController> 
    {
        private const float DefaultVolume = 1f;
        private const float DefaultBrightness = 0f;
        private const string MasterVolumeKey = "audio.master";
        private const string MusicVolumeKey = "audio.music";
        private const string SfxVolumeKey = "audio.sfx";

        [SerializeField] private GameObject PauseSettings;
        [SerializeField] private GameObject MainMenuSettings;
        [SerializeField] private GameObject SliderSettings;
        [Header("Audio Sliders")]
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;
        // TODO: If ambience gets its own slider later, split ambience control from music.

        // Brightness and crouch 
        [SerializeField] private Slider brightSlider;
        [SerializeField] private Toggle crouchToggle;

        // both of the Pause Menus are going to have back buttons on them somewhere
        private Button _mainMenuBackButton;
        private Button _pauseMenuBackButton;
        private bool _audioSlidersInitialized;

        // CONTROLS CODE move later probably
        [Header("Controls screen")]
        [SerializeField] private GameObject PauseControls;
        [SerializeField] private GameObject MainMenuControls;
        private Button _mainMenuBackControls;
        private Button _pauseMenuBackControls;
        [SerializeField] private GameObject ControlsCanvas;

        public void Start()
        {
            
            
            InitializeAudioSliders();
            

            // loop through all children of the main menu settings, and find the back button, and add a listener to it
            Button[] mainMenuButtons = MainMenuSettings.GetComponentsInChildren<Button>();
            foreach (Button button in mainMenuButtons)
            {
                UI.UIButtonSfx.Ensure(button, enableHover: true, enableClick: true);

                if (button.name == "Back")
                {
                    _mainMenuBackButton = button;
                    _mainMenuBackButton.onClick.AddListener(OnMainMenuBackButtonClicked);
                }
            }
            // loop through all children of the pause menu settings, and find the back button, and add a listener to it
            Button[] pauseMenuButtons = PauseSettings.GetComponentsInChildren<Button>();
            foreach (Button button in pauseMenuButtons)
            {
                UI.UIButtonSfx.Ensure(button, enableHover: true, enableClick: true);

                if (button.name == "Back")
                {
                    _pauseMenuBackButton = button;
                    _pauseMenuBackButton.onClick.AddListener(OnPauseMenuBackButtonClicked);
                }
            }

            // CONTROLS CODE
            Button[] mainMenuButtonControl = MainMenuControls.GetComponentsInChildren<Button>();
            foreach (Button button in mainMenuButtonControl)
            {
                UI.UIButtonSfx.Ensure(button, enableHover: true, enableClick: true);

                if (button.name == "Back")
                {
                    _mainMenuBackControls = button;
                    _mainMenuBackControls.onClick.AddListener(OnMainMenuControlsBackButtonClicked);
                }
            }
            Button[] pauseMenuButtonControl = PauseControls.GetComponentsInChildren<Button>();
            foreach (Button button in pauseMenuButtonControl)
            {
                UI.UIButtonSfx.Ensure(button, enableHover: true, enableClick: true);

                if (button.name == "Back")
                {
                    _pauseMenuBackControls = button;
                    _pauseMenuBackControls.onClick.AddListener(OnPauseMenuControlsBackButtonClicked);
                }
            }
            
            // we will hook up a listner to the crouchToggle button now
            if (crouchToggle != null)
            {
                crouchToggle.onValueChanged.AddListener(SetToggleCrouchMode);
            }
            
            if (brightSlider != null)
            {
                brightSlider.onValueChanged.AddListener(SetBrightness);
            }
            LoadSavedBrightness("brightness");
            LoadToggleCrouchMode();
        }
        private void SetBrightness(float brightness)
        {
            CameraMentalStateEffects effects = CameraMentalStateEffects.Instance;
            if (effects == null){ return;}
            effects.gameBrightness = Mathf.Clamp(brightness, 0f, 1f);
            SaveBrightness("brightness", brightness);
        }

        private void SetToggleCrouchMode(bool isToggle)
        {
            PlayerController.Instance.SetToggleCrouchMode(isToggle);
            PlayerPrefs.SetInt("crouch.toggle", isToggle ? 1 : 0);
        }
        
        private void LoadToggleCrouchMode()
        {
            bool isToggle = PlayerPrefs.GetInt("crouch.toggle", 0) == 1;
            PlayerController.Instance.SetToggleCrouchMode(isToggle);
            if (crouchToggle != null)
            {
                crouchToggle.SetIsOnWithoutNotify(isToggle);
            }
        }

        private void InitializeAudioSliders()
        {
            if (_audioSlidersInitialized)
            {
                return;
            }

            AutoBindAudioSlidersIfNeeded();

            if (masterSlider == null || musicSlider == null || sfxSlider == null)
            {
                Debug.LogWarning("SettingsController: Could not resolve Master/Music/SFX sliders. Assign them in inspector.");
                return;
            }

            float masterVolume = LoadSavedVolume(MasterVolumeKey);
            float musicVolume = LoadSavedVolume(MusicVolumeKey);
            float sfxVolume = LoadSavedVolume(SfxVolumeKey);
            //LogLoadedAudioVolumes("InitializeAudioSliders", masterVolume, musicVolume, sfxVolume);

            SetAudioSliderValues(masterVolume, musicVolume, sfxVolume);
            BindAudioSliderCallbacks();
            _audioSlidersInitialized = true;
        }

        private void AutoBindAudioSlidersIfNeeded()
        {
            if (masterSlider != null && musicSlider != null && sfxSlider != null)
            {
                return;
            }

            if (SliderSettings == null)
            {
                return;
            }

            Slider[] sliders = SliderSettings.GetComponentsInChildren<Slider>(true);
            foreach (Slider slider in sliders)
            {
                string nameLower = slider.name.ToLowerInvariant();
                if (masterSlider == null && nameLower.Contains("master"))
                {
                    masterSlider = slider;
                    continue;
                }

                if (sfxSlider == null && nameLower.Contains("sfx"))
                {
                    sfxSlider = slider;
                    continue;
                }

                if (musicSlider == null && nameLower.Contains("music"))
                {
                    musicSlider = slider;
                }
            }

            // Fallback: use the first three sliders in deterministic order.
            //           this is fragile! Sliders should be 
            if (sliders.Length >= 3)
            {
                if (masterSlider == null) { masterSlider = sliders[0]; }
                if (musicSlider == null) { musicSlider = sliders[1]; }
                if (sfxSlider == null) { sfxSlider = sliders[2]; }
            }
        }

        private static float LoadSavedVolume(string key)
        {
            return Mathf.Clamp01(PlayerPrefs.GetFloat(key, DefaultVolume));
        }

        private static void LogLoadedAudioVolumes(string source, float masterVolume, float musicVolume, float sfxVolume)
        {
            Debug.Log(
                $"SettingsController: Loaded audio PlayerPrefs ({source}) -> " +
                $"{MasterVolumeKey}={masterVolume:0.###} (hasKey={PlayerPrefs.HasKey(MasterVolumeKey)}), " +
                $"{MusicVolumeKey}={musicVolume:0.###} (hasKey={PlayerPrefs.HasKey(MusicVolumeKey)}), " +
                $"{SfxVolumeKey}={sfxVolume:0.###} (hasKey={PlayerPrefs.HasKey(SfxVolumeKey)})");
        }

        private static void ApplyAudioVolumes(float masterVolume, float musicVolume, float sfxVolume)
        {
            if (AudioManager.Instance == null)
            {
                return;
            }

            AudioManager.Instance.SetMasterVolume(masterVolume);
            AudioManager.Instance.SetMusicVolume(musicVolume);
            AudioManager.Instance.SetSfxVolume(sfxVolume);
            AudioManager.Instance.SetAmbienceVolume(musicVolume);
            // TODO: Split ambience from music when a dedicated ambience slider is added.
        }

        private void RefreshAudioSlidersFromSavedValues()
        {
            InitializeAudioSliders();
            if (!_audioSlidersInitialized)
            {
                return;
            }

            float masterVolume = LoadSavedVolume(MasterVolumeKey);
            float musicVolume = LoadSavedVolume(MusicVolumeKey);
            float sfxVolume = LoadSavedVolume(SfxVolumeKey);
            LogLoadedAudioVolumes("RefreshAudioSlidersFromSavedValues", masterVolume, musicVolume, sfxVolume);
            SetAudioSliderValues(masterVolume, musicVolume, sfxVolume);
        }

        private void SetAudioSliderValues(float masterVolume, float musicVolume, float sfxVolume)
        {
            masterSlider.SetValueWithoutNotify(masterVolume);
            musicSlider.SetValueWithoutNotify(musicVolume);
            sfxSlider.SetValueWithoutNotify(sfxVolume);
            ApplyAudioVolumes(masterVolume, musicVolume, sfxVolume);
        }

        private void BindAudioSliderCallbacks()
        {
            masterSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            sfxSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
        }

        private static void SaveVolume(string key, float value)
        {
            PlayerPrefs.SetFloat(key, Mathf.Clamp01(value));
            PlayerPrefs.Save();
        }
        private static void SaveBrightness(string key, float value)
        {
            PlayerPrefs.SetFloat(key, Mathf.Clamp01(value));
            PlayerPrefs.Save();
        }
        private void LoadSavedBrightness(string key)
        {
            float value = PlayerPrefs.HasKey(key)
                ? Mathf.Clamp01(PlayerPrefs.GetFloat(key, DefaultBrightness))
                : DefaultBrightness;

            CameraMentalStateEffects effects = CameraMentalStateEffects.Instance;
            if (effects != null)
            {
                effects.gameBrightness = value;
            }

            if (brightSlider != null)
            {
                brightSlider.SetValueWithoutNotify(value);
            }
        }

        private void OnMasterVolumeChanged(float value)
        {
            float clamped = Mathf.Clamp01(value);
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMasterVolume(clamped);
            }

            SaveVolume(MasterVolumeKey, clamped);
        }

        private void OnMusicVolumeChanged(float value)
        {
            float clamped = Mathf.Clamp01(value);
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMusicVolume(clamped);
                AudioManager.Instance.SetAmbienceVolume(clamped);
                // TODO: Split ambience from music when a dedicated ambience slider is added.
            }

            SaveVolume(MusicVolumeKey, clamped);
        }

        private void OnSfxVolumeChanged(float value)
        {
            float clamped = Mathf.Clamp01(value);
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetSfxVolume(clamped);
            }

            SaveVolume(SfxVolumeKey, clamped);
        }

        public void OpenMainMenuSettings()
        {
            RefreshAudioSlidersFromSavedValues();
            MainMenuSettings.SetActive(true);
            // we also want to enable all the children of the MainMenuSettings, cause we want the sliders to be visible as well
            foreach (Transform child in MainMenuSettings.transform)
            {
                child.gameObject.SetActive(true);
            }
            SliderSettings.SetActive(true);
            foreach (Transform child in SliderSettings.transform)
            {
                child.gameObject.SetActive(true);
            }
        }
        public void OpenPauseSettings()
        {
            RefreshAudioSlidersFromSavedValues();
            PauseSettings.SetActive(true);
            SliderSettings.SetActive(true);
        }
        public void CloseSettings()
        {
            PauseSettings.SetActive(false);
            MainMenuSettings.SetActive(false);
            SliderSettings.SetActive(false);
        
            // if we are in the main menu, we want to make sure the main menu is visible again
            if (GameStateManager.Instance.GetCurrentGameState() == Types.GameState.MainMenu)
            {
                // Look for the main menu in the scene can make this a broadcast if we want)
                MainMenu mainMenu = FindFirstObjectByType<MainMenu>();
                if (mainMenu != null) { mainMenu.MainMenuVisible(); }
            }
        }

        private void ReturnToPauseMenu()
        {
            CloseSettings();
            PauseMenuController.Instance.ShowMenu(true);
        }

        private void OnMainMenuBackButtonClicked()
        {
            CloseSettings();
        }

        private void OnPauseMenuBackButtonClicked()
        {
            ReturnToPauseMenu();
        }

        // CONTROL MENU
        // probably should move this to another script D: laterrrrrr

        public void CloseControlSettings()
        {
            PauseControls.SetActive(false);
            MainMenuControls.SetActive(false);
            ControlsCanvas.SetActive(false);
        
            // if we are in the main menu, we want to make sure the main menu is visible again
            if (GameStateManager.Instance.GetCurrentGameState() == Types.GameState.MainMenu)
            {
                // Look for the main menu in the scene can make this a broadcast if we want)
                MainMenu mainMenu = FindFirstObjectByType<MainMenu>();
                if (mainMenu != null) { mainMenu.MainMenuVisible(); }
            }
        }

        public void OpenMainMenuControls()
        {
            MainMenuControls.SetActive(true);

            foreach (Transform child in MainMenuControls.transform)
            {
                child.gameObject.SetActive(true);
            }

            ControlsCanvas.SetActive(true);

            foreach (Transform child in ControlsCanvas.transform)
            {
                child.gameObject.SetActive(true);
            }
        }

        public void OpenPauseControls()
        {
            PauseControls.SetActive(true);

            foreach (Transform child in PauseControls.transform)
            {
                child.gameObject.SetActive(true);
            }

            ControlsCanvas.SetActive(true);

            foreach (Transform child in ControlsCanvas.transform)
            {
                child.gameObject.SetActive(true);
            }
        }

        private void ReturnToPauseMenuFromControls()
        {
            CloseControlSettings();
            PauseMenuController.Instance.ShowMenu(true);
        }

        private void OnMainMenuControlsBackButtonClicked()
        {
            CloseControlSettings();
        }

        private void OnPauseMenuControlsBackButtonClicked()
        {
            ReturnToPauseMenuFromControls();
        }

    }
}
