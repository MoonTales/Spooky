using System.Collections.Generic;
using System.DeveloperPanel;
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
        public string CommandDescription;
        
        public DeveloperCommand(KeyCode key, Action action, string description)
        {
            CommandKey = key;
            CommandAction = action;
            CommandDescription = description;
        }
    }
    public class DeveloperCommands : Singleton<DeveloperCommands>
    {
        
        [SerializeField] public GameObject templateCommandPrefab;
        [SerializeField] public GameObject contentPanel;
        
        // Internal Variables
        private bool _developerModeEnabled = true;
        private KeyCode _developerModeToggleKey = KeyCode.D;
        private KeyCode _holdDeveloperModeKey = KeyCode.LeftControl;

        private GameObject _developerCanvas;
        private List<DeveloperCommand> _developerCommands = new List<DeveloperCommand>();
        
        
        // GAME SPECIFIC STUFF
        [SerializeField] private TMP_Text _hudSanityStateText;
        [SerializeField] private TMP_Text _hudSanityValueText;

        private void Start()
        {
            //_developerCanvas = transform.Find("DevCanvas").gameObject;
            _developerCanvas = GameObject.Find("DevCanvas");
            // Register your commands here
            RegisterCommands();
            
            // Update the UI to show all commands
            UpdateCommandUI();

        }
        
        void Update()
        {
            // Toggle developer mode on/off with a key press
            if (Input.GetKeyDown(_developerModeToggleKey) && Input.GetKey(_holdDeveloperModeKey))
            {
                ToggleDeveloperMode();
            }
            
            // If we are ever not in Developmode, just return early and ignore all other commands
            if (!_developerModeEnabled) {return;}
            
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
                _hudSanityStateText.text = "Mental State: [" +PlayerStats.Instance.GetPlayerStats().GetPlayerMentalState().ToString() + "]";

            }
            if (_hudSanityValueText)
            {
                _hudSanityValueText.text = "[" + Mathf.RoundToInt(PlayerStats.Instance.GetPlayerStats().GetCurrentMentalHealth()).ToString() + "]";
            }
        }
    
        void ToggleDeveloperMode()
        {
            _developerModeEnabled = !_developerModeEnabled;
            DebugUtils.Log($"Developer Mode: {(_developerModeEnabled ? "Enabled" : "Disabled")}");

            if (_developerModeEnabled && _developerCanvas)
            {
                _developerCanvas.SetActive(true);
                // set the cursor to Game and ui mode so we can interact with the developer canvas
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else if (_developerCanvas)
            {
                _developerCanvas.SetActive(false);
                // set the cursor back to locked mode for gameplay
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
        
        /// <summary>
        /// Register a new developer command
        /// </summary>
        public void RegisterCommand(KeyCode key, Action action, string description)
        {
            _developerCommands.Add(new DeveloperCommand(key, action, description));
        }
        
        /// <summary>
        /// Register all your developer commands here
        /// </summary>
        private void RegisterCommands()
        {
            // Example commands
            RegisterCommand(KeyCode.F1, () => DebugUtils.Log("F1 Command Executed"), "F1 - Print Test Message");
            RegisterCommand(KeyCode.F2, () => Time.timeScale = 2f, "F2 - Speed Up Time (2x)");
            RegisterCommand(KeyCode.F3, () => Time.timeScale = 1f, "F3 - Normal Time Speed");
            RegisterCommand(KeyCode.F4, () => Application.LoadLevel(0), "F4 - Reload Current Scene");
            RegisterCommand(KeyCode.G, ToggleGodMode, "G - Toggle God Mode");
            
            // Official Commands
            RegisterCommand(KeyCode.Minus, () => EventBroadcaster.Broadcast_OnPlayerDamaged(10.0f), "Send off a broadcasts to simulate the player taking 10 mental damage (this is the same as raising anxiety or sleep deprivation by 10 points)");
            RegisterCommand(KeyCode.Equals, () => EventBroadcaster.Broadcast_OnPlayerDamaged(-10.0f), "Send off a broadcast to simulate the player healing 10 mental health (this is the same as lowering anxiety or sleep deprivation by 10 points)");
            
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
                    keyText.text = "KEYCODE: [" + command.CommandKey.ToString() + "]";
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
        
        
        
        private void ToggleGodMode()
        {
            DebugUtils.Log("God Mode Toggled");
            // Your god mode logic here
        }
    }
}
