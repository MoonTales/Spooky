using System;
using System.Collections.Generic;
using Managers;
using Player;
using UI.PauseMenu;
using UnityEngine;
using UnityEngine.UI;
using Types = System.Types;


namespace UI.Main_Menu
{
    public class MainMenu : MonoBehaviour
    {
    
        [SerializeField] private GameObject mainMenuCanvas;
    
        // Internal variables to link to all of the buttons on the main menu
        private Button _newgameButton;
        private Button _continueButton;
        private Button _settingsButton;
        private Button _quitButton;
        
        // Internal Variables for polish
        private int savedAct;
        private List<int> savedDrawingIDs;
        private string savedSceneName;
        
        
        private void Start()
        {
            mainMenuCanvas.SetActive(true);
            // at the start of the game, get access to our buttons, and add listeners to them
            // the children may be in the children
            Button[] allButtons = GetComponentsInChildren<Button>();
            foreach (Button button in allButtons)
            { 
                UI.UIButtonSfx.Ensure(button, enableHover: true, enableClick: true);

                if (button.name == "NewGame")
                {
                    _newgameButton = button;
                    _newgameButton.onClick.AddListener(OnNewGameButtonClicked);
                    _newgameButton.enabled = true;
                } 
                else if (button.name == "Continue")
                {
                    _continueButton = button;
                    _continueButton.enabled = true;
                    if (SaveSystem.Instance.DoesSaveGameExist()){
                        _continueButton.interactable = true; 
                        _continueButton.onClick.AddListener(OnContinueButtonClicked);
                    } else {
                        _continueButton.interactable = false;  
                    }
                }
                else if (button.name == "Settings")
                {
                    _settingsButton = button;
                    _settingsButton.onClick.AddListener(OnSettingsButtonClicked);
                    _settingsButton.enabled = true;
                }
                else if (button.name == "Quit")
                {
                    _quitButton = button;
                    _quitButton.onClick.AddListener(OnQuitButtonClicked);
                    _quitButton.enabled = true;
                }
            }
            
            // CHANGES ALLOW TO LOAD SAVE DATA
            if (SaveSystem.Instance.DoesSaveGameExist())
            {
                SaveSystem.Instance.ReadSaveFromDisk();

                var sceneData = SaveSystem.Instance.GetData("SceneSwapper");
                var gameStateData = SaveSystem.Instance.GetData("GameState");
                var inventoryData = SaveSystem.Instance.GetData("PlayerInventory");

                if (sceneData.HasValue)
                    savedSceneName = JsonUtility.FromJson<SceneSwapper.SceneSwapSaveData>(sceneData.Value.Data).CurrentSceneName;

                if (gameStateData.HasValue)
                    savedAct = JsonUtility.FromJson<GameStateManager.GameStateSaveData>(gameStateData.Value.Data).worldClockHour;

                if (inventoryData.HasValue)
                    savedDrawingIDs = JsonUtility.FromJson<PlayerInventory.PlayerInventorySaveData>(inventoryData.Value.Data).collectedDrawingIDs;
            }
            
            if(savedSceneName != ""){ Debug.Log("Saved scene name: " + savedSceneName);} else {Debug.Log("No saved scene name found.");}
            if(savedAct != 0){ Debug.Log("Saved act: " + savedAct);} else {Debug.Log("No saved act found.");}
            if(savedDrawingIDs != null){ Debug.Log("Saved drawing IDs: " + string.Join(", ", savedDrawingIDs));} else {Debug.Log("No saved drawing IDs found.");}
   
            
        }
        // Button connections
        private void OnContinueButtonClicked()
        {
            DisableButtons();
            new Types.ScreenFadeData(3f, 1f, 3f, null,SwapToExistGame).Send();
        }

        private void OnNewGameButtonClicked()
        {
             if (SaveSystem.Instance.DoesSaveGameExist())
            {
                UiPopupConfirmation.Instance.RequestPopupConfirmation(TextDB.GetText("popup", "newgame"), DeleteSave);
            }
            else
            {
                DisableButtons();
                new Types.ScreenFadeData(3f, 1f, 3f, () => Debug.Log(""),SwapToNewGame).Send();
            }
        }

        private void DisableButtons()
        {
            // we will just disable all of the buttons, so that nothing can be clicked while we fade in
            _newgameButton.enabled = false;
            _newgameButton.interactable = false;
            _continueButton.enabled = false;
            _continueButton.interactable = false;
            _settingsButton.enabled = false;
            _settingsButton.interactable = false;
            _quitButton.enabled = false;
            _quitButton.interactable = false;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.TriggerMainMenuMusicTransition();
            }
        }

        
        
        private void SwapToNewGame()
        {
            mainMenuCanvas.SetActive(false);
            EventBroadcaster.Broadcast_GameStateChanged(Types.GameState.Gameplay);
            SceneSwapper.Instance.SwapScene("Tutorial"); 
        }

        private void SwapToExistGame()
        {
            mainMenuCanvas.SetActive(false);
            SaveSystem.Instance.LoadGame();
        }

        private void DeleteSave()
        {
            SaveSystem.Instance.DeleteSaveData();
            DisableButtons();
            new Types.ScreenFadeData(3f, 1f, 3f, null,SwapToNewGame).Send();
        }

        private void OnSettingsButtonClicked()
        {
            mainMenuCanvas.SetActive(false);
            SettingsController.Instance.OpenMainMenuSettings();
        }

        private void OnQuitButtonClicked()
        {
            UiPopupConfirmation.Instance.RequestPopupConfirmation(TextDB.GetText("popup", "quit"), CloseGame);
        }
    
        public void MainMenuVisible()
        {
            mainMenuCanvas.SetActive(true);
        }

        private void CloseGame()
        {
            Application.Quit();
        }

    }
}
