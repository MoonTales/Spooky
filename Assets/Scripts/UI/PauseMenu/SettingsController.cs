using System;
using Managers;
using UI.Main_Menu;
using UnityEngine;
using UnityEngine.UI;
using Types = System.Types;

namespace UI.PauseMenu
{
    public class SettingsController : Singleton<SettingsController> 
    {
        [SerializeField] private GameObject PauseSettings;
        [SerializeField] private GameObject MainMenuSettings;
        [SerializeField] private GameObject SliderSettings;
    
        // both of the Pause Menus are going to have back buttons on them somewhere
        private Button _mainMenuBackButton;
        private Button _pauseMenuBackButton;

        public void Start()
        {
            // loop through all children of the main menu settings, and find the back button, and add a listener to it
            Button[] mainMenuButtons = MainMenuSettings.GetComponentsInChildren<Button>();
            foreach (Button button in mainMenuButtons)
            {
                if (button.name == "Back")
                {
                    _mainMenuBackButton = button;
                    _mainMenuBackButton.onClick.AddListener(CloseSettings);
                }
            }
            // loop through all children of the pause menu settings, and find the back button, and add a listener to it
            Button[] pauseMenuButtons = PauseSettings.GetComponentsInChildren<Button>();
            foreach (Button button in pauseMenuButtons)
            {
                if (button.name == "Back")
                {
                    _pauseMenuBackButton = button;
                    _pauseMenuBackButton.onClick.AddListener(ReturnToPauseMenu);
                }
            }
        
        }

        public void OpenMainMenuSettings()
        {
            MainMenuSettings.SetActive(true);
            // we also want to enable all the children of the MainMenuSettings, cause we want the sliders to be visible as well
            foreach (Transform child in MainMenuSettings.transform)
            {
                child.gameObject.SetActive(true);
            }
            SliderSettings.SetActive(true);
            for (int i = 0; i < MainMenuSettings.transform.childCount; i++)
            {
                MainMenuSettings.transform.GetChild(i).gameObject.SetActive(true);
            }
        }
        public void OpenPauseSettings()
        {
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
                MainMenu.Instance.MainMenuVisible();
            }
        }

        private void ReturnToPauseMenu()
        {
            CloseSettings();
            PauseMenuController.Instance.ShowMenu(true);
        }
    }
}
