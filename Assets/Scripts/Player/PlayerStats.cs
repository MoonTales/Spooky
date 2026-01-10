using System;
using UnityEngine;
using UnityEngine.Serialization;
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
        [FormerlySerializedAs("defaultPlayerState")] [SerializeField] private Types.PlayerHealthState defaultPlayerHealthState = Types.PlayerHealthState.Healthy;
        [Space(10)]
        [SerializeField] private float injuredHealthCutoff = 0.75f; // health percentage cutoffs for the different HealthStates
        [SerializeField] private float criticalHealthCutoff = 0.25f; 
        // cutoffs for the different HealthStates
        
        
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
                _playerStats.DebugPrintStats();
            }
        }

        private void InitializeDefaultStats()
        {
            _playerStats.SetCurrentHealth(defaultCurrentHealth);
            _playerStats.SetMaxHealth(defaultMaxHealth);
            _playerStats.SetCurrentStamina(defaultCurrentStamina);
            _playerStats.SetMaxStamina(defaultMaxStamina);
            _playerStats.SetMovementSpeed(defaultMovementSpeed);
            _playerStats.SetPlayerState(defaultPlayerHealthState, false);
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
                _playerStats.SetPlayerState(Types.PlayerHealthState.Dead);
            }
            else if (currentHealth < _playerStats.GetMaxHealth() * criticalHealthCutoff)
            {
                _playerStats.SetPlayerState(Types.PlayerHealthState.Critical);
            }
            else if (currentHealth < _playerStats.GetMaxHealth() * injuredHealthCutoff)
            {
                _playerStats.SetPlayerState(Types.PlayerHealthState.Injured);
            }
            else
            {
                _playerStats.SetPlayerState(Types.PlayerHealthState.Healthy);
            }
            
        }
        
        
        
        
    }
}