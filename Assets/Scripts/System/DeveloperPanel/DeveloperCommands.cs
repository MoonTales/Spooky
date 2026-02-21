using System.Collections.Generic;
using Managers;
using Player;
using TMPro;
using UnityEngine;

namespace System
{
    /// <summary>
    /// A Class used to handle various commands, interactions and debugging tools for developers during the
    /// development and testing of the game.
    /// </summary>
    
    // custom struct to connect a keycode, to an Action, and a description for the command
    [Serializable]
    public struct DeveloperCommand
    {
        public KeyCode CommandKey;
        public Action CommandAction;
        public string CommandName;
        public string CommandDescription;
        
        public DeveloperCommand(KeyCode key, Action action, string name, string description)
        {
            CommandKey = key;
            CommandAction = action;
            CommandName = name;
            CommandDescription = description;
        }
    }
    public class DeveloperCommands : Singleton<DeveloperCommands>
    {
        
        [SerializeField] public GameObject templateCommandPrefab;
        [SerializeField] public GameObject contentPanel;
        
        // Internal Variables
        private bool _developerModeEnabled = true;
        private KeyCode _developerModeToggleKey = KeyCode.Backslash;
        private KeyCode _holdDeveloperModeKey = KeyCode.LeftControl;

        private GameObject _developerCanvas;
        private List<DeveloperCommand> _developerCommands = new List<DeveloperCommand>();
        
        
        // GAME SPECIFIC STUFF
        [SerializeField] private TMP_Text _hudSanityStateText;
        [SerializeField] private TMP_Text _GameStateText;
        [SerializeField] private TMP_Text _WorldLocationText;
        [SerializeField] private TMP_Text _drawingCollectionText;
        private void Start()
        {
            //_developerCanvas = transform.Find("DevCanvas").gameObject;
            _developerCanvas = GameObject.Find("DevCanvas");
            // Register your commands here
            RegisterCommands();
            
            // Update the UI to show all commands
            UpdateCommandUI();
            ToggleDeveloperMode(); // Start with developer mode off by default

        }
        
        void Update()
        {
            // Toggle developer mode on/off with a key press
            if (Input.GetKeyDown(_developerModeToggleKey) && Input.GetKey(_holdDeveloperModeKey))
            {
                ToggleDeveloperMode();
            }
            
            
            // at anytime, when the player is holding the holdDeveloperModeKey, we want to enable the cursor, and then return it to its previous state when they release it.
            // This allows developers to interact with the developer canvas without having to toggle developer mode on and off
            
            // If we are ever not in Developmode, just return early and ignore all other commands
            if (!_developerModeEnabled) {return;}
            
            if (Input.GetKeyDown(_holdDeveloperModeKey))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else if (Input.GetKeyUp(_holdDeveloperModeKey))
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            
            // Check for command key presses
            foreach (DeveloperCommand command in _developerCommands)
            {
                if (Input.GetKeyDown(command.CommandKey))
                {
                    command.CommandAction?.Invoke();
                }
            }
            
            // DISPLAY STATS

            if (_hudSanityStateText)
            {
                // this will be "Mental Core State: [STATE] - Mental State: [STATE] - Mental Health: [CURRENT / MAX]"
                _hudSanityStateText.text = "Mental State: [" + PlayerStats.Instance.GetPlayerStats().GetPlayerMentalState() +
                                           "] - [" + PlayerStats.Instance.GetPlayerStats().GetCurrentMentalHealth() + 
                                           " / " + PlayerStats.Instance.GetPlayerStats().GetMaxMentalHealth() + "]";
            }

            if (_GameStateText)
            {
                _GameStateText.text = "Game State: [" + GameStateManager.Instance.GetCurrentGameState().ToString() + "]";
            }
            if (_WorldLocationText)
            {
                _WorldLocationText.text = "World Location: [" + GameStateManager.Instance.GetCurrentWorldLocation().ToString() + "]" + " - World Clock Hour: [" + GameStateManager.Instance.GetCurrentWorldClockHour().ToString() + "]";
            }

            if (_drawingCollectionText)
            {
                _drawingCollectionText.text = "Drawings Collected: [" + PlayerInventory.Instance.GetCurrentDrawingsThisNight() + "/ " + PlayerInventory.Instance.GetMaxDrawingsPerNight() + "] - (" + PlayerInventory.Instance.GetDrawingCount() + " in Inventory)";
            }

        }
    
        void ToggleDeveloperMode()
        {
            _developerModeEnabled = !_developerModeEnabled;

            if (_developerModeEnabled && _developerCanvas)
            {
                _developerCanvas.SetActive(true);
            }
            else if (_developerCanvas)
            {
                _developerCanvas.SetActive(false);
                
                // Set the gamestate, to be the current gamestate, so that Cursor correectly updates
                EventBroadcaster.Broadcast_GameStateChanged(GameStateManager.Instance.GetCurrentGameState());
            }
        }
        
        /// <summary>
        /// Register a new developer command
        /// </summary>
        public void RegisterCommand(KeyCode inKey, Action inAction, string inName, string inDescription)
        {
            _developerCommands.Add(new DeveloperCommand(inKey, inAction, inName, inDescription));
        }
        
        /// <summary>
        /// Register all your developer commands here
        /// </summary>
        private void RegisterCommands()
        {
            // Official Commands
            RegisterCommand(KeyCode.G, () => ForceStartGameplay() , "Force Gameplay State", "Send off a broadcast to force the game state to switch to Gameplay (this will also trigger all associated events that come with entering the gameplay state)");
            RegisterCommand(KeyCode.Minus, () => EventBroadcaster.Broadcast_OnPlayerDamaged(10.0f), "Hurt Player", "Send off a broadcasts to simulate the player taking 10 mental damage (this is the same as raising anxiety or sleep deprivation by 10 points)");
            RegisterCommand(KeyCode.Equals, () => EventBroadcaster.Broadcast_OnPlayerDamaged(-10.0f), "Heal Player", "Send off a broadcast to simulate the player healing 10 mental health (this is the same as lowering anxiety or sleep deprivation by 10 points)");
            RegisterCommand(KeyCode.Delete, () => EventBroadcaster.Broadcast_OnPlayerHealthStateChanged(Types.PlayerMentalState.Breakdown), "Kill Player", "Send off a broadcast to simulate the player's mental state breaking down and reaching 0");
            RegisterCommand(KeyCode.LeftBracket, () => GameStateManager.Instance.SetWorldClockHour(GameStateManager.Instance.GetCurrentWorldClockHour() - 1), "Decrease World Clock", "Send off a broadcast to decrease the world clock hour by 1");
            RegisterCommand(KeyCode.RightBracket, () => GameStateManager.Instance.SetWorldClockHour(GameStateManager.Instance.GetCurrentWorldClockHour() + 1), "Increase World Clock", "Send off a broadcast to increase the world clock hour by 1");
            RegisterCommand(KeyCode.Comma, () => GameStateManager.Instance.CycleWorldLocation(-1), "Prev World Location", "Cycle to the previous world location enum value");
            RegisterCommand(KeyCode.Period, () => GameStateManager.Instance.CycleWorldLocation(1), "Next World Location", "Cycle to the next world location enum value");
            RegisterCommand(KeyCode.Semicolon, () => EventBroadcaster.Broadcast_GameRestarted(), "Emulate Game Restart", "Send off a broadcast to emulate the game being restarted (this simulates if we did return to main menu.)");
            RegisterCommand(KeyCode.LeftArrow, () => PlayerInventory.Instance.DebugAdjustDrawingsThisNight(-1), "Decrease Drawings", "Decrease current drawings collected this night by 1 (min 0).");
            RegisterCommand(KeyCode.RightArrow, () => PlayerInventory.Instance.DebugAdjustDrawingsThisNight(1), "Increase Drawings", "Increase current drawings collected this night by 1 (max per night).");
            
            // Cutscenes
            RegisterCommand(KeyCode.Alpha0, () => CutsceneManager.Instance.OnRequestSkipCutscene(), "Skip Cutscene", "Send off a broadcast to skip the current cutscene (if any is playing)");
            RegisterCommand(KeyCode.Backspace, () => DebugUtils.ClearConsole(), "Clear Console", "Clears the Unity Console of all messages");

            // Scene traversal commands
            RegisterCommand(KeyCode.Alpha1, () => SceneSwapper.Instance.SwapScene("Bedroom"), "Load Bedroom Scene", "Load the Bedroom Scene instantly");
            RegisterCommand(KeyCode.Alpha2, () => SceneSwapper.Instance.SwapScene("Nightmare1"), "Load Nightmare Scene", "Load the Nightmare 1 Scene instantly");
            RegisterCommand(KeyCode.Alpha3, () => SceneSwapper.Instance.SwapScene("Tutorial"), "Load Tutorial Scene", "Load the Tutorial Nightmare Scene instantly");
            
            // Frame Rate Commands
            // get the current frame rate and display it in the console
            // Increase Frame Rate by 10
            RegisterCommand(KeyCode.Alpha4, ()=>SetFrameRate(10), "Set Low FPS", "Sets the target frame rate to 10");
            // Set Frame Rate to 30
            RegisterCommand(KeyCode.Alpha5, ()=>SetFrameRate(30), "Set Medium FPS", "Sets the target frame rate to 30");
            // Set Frame Rate to 60
            RegisterCommand(KeyCode.Alpha6, ()=>SetFrameRate(60), "Set High FPS", "Sets the target frame rate to 60");
            // Set Frame Rate to 120
            RegisterCommand(KeyCode.Alpha7, ()=>SetFrameRate(120), "Set Ultra FPS", "Sets the target frame rate to 120");
        }

        private void ForceStartGameplay()
        {
            EventBroadcaster.Broadcast_GameStateChanged(Types.GameState.Gameplay);
            Flashlight.Instance.SetDoWePossessTheFlashlight(true);
        }
        private void SetFrameRate(int fps)
        {
            QualitySettings.vSyncCount = 0; // Disable VSync
            Application.targetFrameRate = fps;
        }

        /// <summary>
        /// Updates the UI to display all registered commands
        /// </summary>
        private void UpdateCommandUI()
        {
            if (_developerCanvas == null) return;
    
            // Clear existing command UI elements
            foreach (Transform child in contentPanel.transform)
            {
                Destroy(child.gameObject);
            }
            
    
            // Now create new ones
            foreach (DeveloperCommand command in _developerCommands)
            {
                GameObject newCommandUI = Instantiate(templateCommandPrefab, contentPanel.transform);
        
                TMP_Text keyText = newCommandUI.transform.Find("KeyCode").GetComponent<TMP_Text>();
                if (keyText)
                {
                    keyText.text = command.CommandName.ToString() + " - KEYCODE: [" + command.CommandKey.ToString() + "]";
                    keyText.color = Color.yellow;
                    keyText.fontStyle = TMPro.FontStyles.Bold;
                }
        
                
                TMP_Text descText = newCommandUI.transform.Find("Desc").GetComponent<TMP_Text>();
                if (descText)
                {
                    descText.text = command.CommandDescription;
                    descText.color = Color.white;
                    
                }
            }
        }
    }
}
