using System;
using UnityEngine;
using Types = System.Types;

namespace Player
{
    /// <summary>
    /// Class used to handle and maintain all stats related to the player
    /// </summary>
    public class PlayerStats : Singleton<PlayerStats>
    {
        private Types.FPlayerStats _playerStats; public Types.FPlayerStats GetPlayerStats() { return _playerStats; }
        
        [Header("Default Player Stats")]
        [SerializeField] private float defaultCurrentHealth = 100f;
        [SerializeField] private float defaultMaxHealth = 100f;
        [SerializeField] private float defaultCurrentStamina = 100f;
        [SerializeField] private float defaultMaxStamina = 100f;
        [SerializeField] private float defaultMovementSpeed = 5f;
        [SerializeField] private Types.PlayerState defaultPlayerState = Types.PlayerState.Healthy;
        
        
        public void Start()
        {
            // Initialize the player stats
            InitializeDefaultStats();
        }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.P))
            {
                // debug print the current player stats
                Types.DebugPrintStats();
            }
        }

        private void InitializeDefaultStats()
        {
            _playerStats.SetCurrentHealth(defaultCurrentHealth);
            _playerStats.SetMaxHealth(defaultMaxHealth);
            _playerStats.SetCurrentStamina(defaultCurrentStamina);
            _playerStats.SetMaxStamina(defaultMaxStamina);
            _playerStats.SetMovementSpeed(defaultMovementSpeed);
            _playerStats.SetPlayerState(defaultPlayerState, false);
        }
        
        
        
        
        
    }
}