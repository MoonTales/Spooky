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
        [SerializeField] private Types.PlayerMentalState defaultPlayerMentalState = Types.PlayerMentalState.Normal;
        [Space(10)]
        [Header("Cutoffs for each Mental Health State")]
        [SerializeField] private float NormalMentalHealthCutoff = 1.0f; 
        // Anxious Mental Health Cutoffs
        [SerializeField] private float MildlyAnxiousMentalHealthCutoff = 0.8f;
        [SerializeField] private float ModeratlyAnxiousMentalHealthCutoff = 0.6f;
        [SerializeField] private float SeverlyAnxiousMentalHealthCutoff = 0.25f;
        [SerializeField] private float PanicMentalHealthCutoff = 0.1f;
        // Sleep Deprived Mental Health Cutoffs
        [SerializeField] private float MildlySleepDeprivedMentalHealthCutoff = 0.8f;
        [SerializeField] private float ModeratlySleepDeprivedMentalHealthCutoff = 0.6f;
        [SerializeField] private float SeverlySleepDeprivedMentalHealthCutoff = 0.25f;
        [SerializeField] private float ExhaustedMentalHealthCutoff = 0.1f;
        
        
        
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
        
        public void ResetAllStatsToDefault()
        {
            InitializeDefaultStats();
        }
        
        private void OnPlayerDamaged(float damageAmount)
        {
            UpdateCurrentMentalHealth(-damageAmount);
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
            _playerStats.SetCurrentMentalHealth(defaultCurrentHealth);
            _playerStats.SetMaxMentalHealth(defaultMaxHealth);
            _playerStats.SetCurrentStamina(defaultCurrentStamina);
            _playerStats.SetMaxStamina(defaultMaxStamina);
            _playerStats.SetMovementSpeed(defaultMovementSpeed);
            _playerStats.SetPlayerMentalState(defaultPlayerMentalState, false);
            
        }
        
        
        // this is whats called from the FPlayerStats struct to update health
        public void UpdateCurrentMentalHealth(float delta)
        {
            // Update the current health
            float currentMentalHealth = _playerStats.GetCurrentMentalHealth();
            // clamp the health between 0 and max health
            currentMentalHealth = Mathf.Clamp(currentMentalHealth + delta, 0, _playerStats.GetMaxMentalHealth());
            _playerStats.SetCurrentMentalHealth(currentMentalHealth);
            
            
            // now we determine the mental state based on the current health and core state
            
            Types.PlayerMentalCoreState coreState = _playerStats.GetPlayerMentalCoreState();

            // this means we are in the nightmare
            if (coreState == Types.PlayerMentalCoreState.Anxious)
            {
                if (currentMentalHealth <= 0)
                {
                    _playerStats.SetPlayerMentalState(Types.PlayerMentalState.Breakdown);
                }
                else if (currentMentalHealth <= PanicMentalHealthCutoff * _playerStats.GetMaxMentalHealth())
                {
                    _playerStats.SetPlayerMentalState(Types.PlayerMentalState.Panic);
                }
                else if (currentMentalHealth <= SeverlyAnxiousMentalHealthCutoff * _playerStats.GetMaxMentalHealth())
                {
                    _playerStats.SetPlayerMentalState(Types.PlayerMentalState.SeverelyAnxious);
                }
                else if (currentMentalHealth <= ModeratlyAnxiousMentalHealthCutoff * _playerStats.GetMaxMentalHealth())
                {
                    _playerStats.SetPlayerMentalState(Types.PlayerMentalState.ModeratelyAnxious);
                }
                else if (currentMentalHealth <= MildlyAnxiousMentalHealthCutoff * _playerStats.GetMaxMentalHealth())
                {
                    _playerStats.SetPlayerMentalState(Types.PlayerMentalState.MildlyAnxious);
                }
                else
                {
                    _playerStats.SetPlayerMentalState(Types.PlayerMentalState.Normal);
                }
            } 
            else if (coreState == Types.PlayerMentalCoreState.SleepDeprived)
            {
                if (currentMentalHealth <= 0)
                {
                    _playerStats.SetPlayerMentalState(Types.PlayerMentalState.Breakdown);
                }
                else if (currentMentalHealth <= PanicMentalHealthCutoff * _playerStats.GetMaxMentalHealth())
                {
                    _playerStats.SetPlayerMentalState(Types.PlayerMentalState.Exhausted);
                }
                else if (currentMentalHealth <= SeverlyAnxiousMentalHealthCutoff * _playerStats.GetMaxMentalHealth())
                {
                    _playerStats.SetPlayerMentalState(Types.PlayerMentalState.SeverelySleepDeprived);
                }
                else if (currentMentalHealth <= ModeratlyAnxiousMentalHealthCutoff * _playerStats.GetMaxMentalHealth())
                {
                    _playerStats.SetPlayerMentalState(Types.PlayerMentalState.ModeratelySleepDeprived);
                }
                else if (currentMentalHealth <= MildlyAnxiousMentalHealthCutoff * _playerStats.GetMaxMentalHealth())
                {
                    _playerStats.SetPlayerMentalState(Types.PlayerMentalState.MildlySleepDeprived);
                }
                else
                {
                    _playerStats.SetPlayerMentalState(Types.PlayerMentalState.Normal);
                }
            }

            
        }
        
        
        
        
    }
}