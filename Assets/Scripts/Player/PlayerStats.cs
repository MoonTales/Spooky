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
        
        protected override void RegisterSubscriptions()
        {
            base.RegisterSubscriptions();
            TrackSubscription(() => EventBroadcaster.OnPlayerDamaged += OnPlayerDamaged,
                () => EventBroadcaster.OnPlayerDamaged -= OnPlayerDamaged);
            
        }
        
        private void OnPlayerDamaged(float damageAmount)
        {
            UpdateCurrentHealth(-damageAmount);
        }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.L))
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
        
        
        // this is whats called from the FPlayerStats struct to update health
        public void UpdateCurrentHealth(float delta)
        {
            // Update the current health
            float currentHealth = _playerStats.GetCurrentHealth();
            // clamp the health between 0 and max health
            currentHealth = Mathf.Clamp(currentHealth + delta, 0, _playerStats.GetMaxHealth());
            _playerStats.SetCurrentHealth(currentHealth);
            
            // special edge cases for health changes
            // If the player health drops to 0, set state to Dead
            if (currentHealth <= 0)
            {
                _playerStats.SetPlayerState(Types.PlayerState.Dead);
            }
            else if (currentHealth < _playerStats.GetMaxHealth() * 0.25f)
            {
                _playerStats.SetPlayerState(Types.PlayerState.Critical);
            }
            else if (currentHealth < _playerStats.GetMaxHealth() * 0.75f)
            {
                _playerStats.SetPlayerState(Types.PlayerState.Injured);
            }
            else
            {
                _playerStats.SetPlayerState(Types.PlayerState.Healthy);
            }
            
        }
        
        
        
        
    }
}