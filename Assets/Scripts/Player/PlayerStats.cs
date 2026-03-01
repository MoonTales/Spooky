using System;
using System.Collections;
using Managers;
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
        [SerializeField] private float defaultCurrentMentalHealth = 100f;
        [SerializeField] private float defaultMaxMentalHealth = 100f;
        [SerializeField] private float defaultCurrentStamina = 100f;
        [SerializeField] private float defaultMaxStamina = 100f;
        [SerializeField] private float defaultMovementSpeed = 5f;
        [SerializeField] private Types.PlayerMentalState defaultPlayerMentalState = Types.PlayerMentalState.Normal;
        [Space(10)]
        [Header("Cutoffs for each Mental Health State")]
        // Anxious Mental Health Cutoffs
        private float MildlyAnxiousMentalHealthCutoff = 0.75f;
        private float ModeratlyAnxiousMentalHealthCutoff = 0.4f;
        private float SeverlyAnxiousMentalHealthCutoff = 0.20f;
        private float PanicMentalHealthCutoff = 0.1f;
        // Sleep Deprived Mental Health Cutoffs
        private float MildlySleepDeprivedMentalHealthCutoff = 0.5f;
        private float ModeratlySleepDeprivedMentalHealthCutoff = 0.35f;
        private float SeverlySleepDeprivedMentalHealthCutoff = 0.20f;
        private float ExhaustedMentalHealthCutoff = 0.1f;
        
        
        // Internal field used for the Sanity draining
        private Coroutine _sanityDrainCoroutine;
        
        protected override void OnGameStateChanged(Types.GameState newGameState)
        {
            // based on the gamestate, we need to do specific things to the sanity drain
            // we also do not want to do this, if we are in the Tutorial or Main Menu
            if (newGameState == Types.GameState.Gameplay)
            {
                // start sanity drain
                if (_sanityDrainCoroutine == null)
                {
                    _sanityDrainCoroutine = StartCoroutine(StartSanityDrain());
                }
            }
            else
            {
                // stop sanity drain
                if (_sanityDrainCoroutine != null)
                {
                    StopCoroutine(_sanityDrainCoroutine);
                    _sanityDrainCoroutine = null;
                }
            }
        }

        private IEnumerator StartSanityDrain()
        {
            while (_playerStats.GetCurrentMentalHealth() > 0 )
            {
                
                // A few edge cases to look for:
                // A good wake up, while in the nightmare, should pause our sanity drain
                if(GameStateManager.Instance.GetCurrentWorldLocation() == Types.WorldLocation.Nightmare && SleepTrackerManager.Instance.GetIsGoodWakeup())
                {
                    yield return null; // skip this frame and check again on the next frame
                    continue;
                }
                // a good sleep, while in the bedroom (
                
                
                
                // Drain sanity over time based on core state
                Types.PlayerMentalCoreState coreState = _playerStats.GetPlayerMentalCoreState();
                float drainAmount = 0f;
                
                
                if (coreState == Types.PlayerMentalCoreState.SleepDeprived)
                {
                    drainAmount = 1f; // Drain 1 mental health per interval
                }
                else if (coreState == Types.PlayerMentalCoreState.Anxious)
                {
                    drainAmount = 1.5f; // Drain 2 mental health per interval
                }

                bool isInTutorialOrMainMenu = GameStateManager.Instance.GetCurrentWorldLocation() == Types.WorldLocation.Tutorial || GameStateManager.Instance.GetCurrentGameState() == Types.GameState.MainMenu;
                if (isInTutorialOrMainMenu)
                {
                    drainAmount = 0f; // No drain in tutorial or main menu
                }
                UpdateCurrentMentalHealth(-drainAmount);

                // Wait for a set interval before draining again
                yield return new WaitForSeconds(5f); // Adjust the interval as needed
            }
        }

        public void Start()
        {
            // Initialize the player stats
            InitializeDefaultStats();
            // only set once on the start
            
            
        }

        protected override void OnGameStarted()
        {
            // whenever we load from the main menu, we reset the player stats (like setting us to sleep deprived)
            _playerStats.SetPlayerMentalCoreState(Types.PlayerMentalCoreState.Anxious);
        }
        
        protected override void RegisterSubscriptions()
        {
            base.RegisterSubscriptions();
            TrackSubscription(() => EventBroadcaster.OnPlayerDamaged += OnPlayerDamaged,
                () => EventBroadcaster.OnPlayerDamaged -= OnPlayerDamaged);
            TrackSubscription(() => EventBroadcaster.OnWorldLocationChangedEvent += OnWorldLocationChanged,
                () => EventBroadcaster.OnWorldLocationChangedEvent -= OnWorldLocationChanged);
            
        }
        
        private void OnWorldLocationChanged(Types.WorldLocation newLocation)
        {
            // regardless, whenever we change locations, we wanna reset sanity
            InitializeDefaultStats();
            // but also set the core state based on location
            if (newLocation == Types.WorldLocation.Bedroom)
            {
                _playerStats.SetPlayerMentalCoreState(Types.PlayerMentalCoreState.SleepDeprived);
            }
            else if (newLocation == Types.WorldLocation.Nightmare)
            {
                _playerStats.SetPlayerMentalCoreState(Types.PlayerMentalCoreState.Anxious);
            }
            else if (newLocation == Types.WorldLocation.Tutorial)
            {
                _playerStats.SetPlayerMentalCoreState(Types.PlayerMentalCoreState.Anxious);
            }
        }
        
        public void ResetAllStatsToDefault()
        {
            InitializeDefaultStats();
        }
        
        private void OnPlayerDamaged(float damageAmount)
        {
            UpdateCurrentMentalHealth(-damageAmount);
        }
        

        private void InitializeDefaultStats()
        {
            _playerStats.SetCurrentMentalHealth(defaultCurrentMentalHealth);
            _playerStats.SetMaxMentalHealth(defaultMaxMentalHealth);
            _playerStats.SetCurrentStamina(defaultCurrentStamina);
            _playerStats.SetMaxStamina(defaultMaxStamina);
            _playerStats.SetMovementSpeed(defaultMovementSpeed);
            _playerStats.SetPlayerMentalState(defaultPlayerMentalState, false);
            UpdateCurrentMentalHealth(0); // to ensure mental state is set correctly based on current health
            
        }
        
        public void SetMentalCoreState(Types.PlayerMentalCoreState coreState)
        {
            _playerStats.SetPlayerMentalCoreState(coreState);
        }
        
        public void SetMentalState(Types.PlayerMentalState mentalState)
        {
            _playerStats.SetPlayerMentalState(mentalState);
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
                else if (currentMentalHealth <= ExhaustedMentalHealthCutoff * _playerStats.GetMaxMentalHealth())
                {
                    _playerStats.SetPlayerMentalState(Types.PlayerMentalState.Exhausted);
                }
                else if (currentMentalHealth <= SeverlySleepDeprivedMentalHealthCutoff * _playerStats.GetMaxMentalHealth())
                {
                    _playerStats.SetPlayerMentalState(Types.PlayerMentalState.SeverelySleepDeprived);
                }
                else if (currentMentalHealth <= ModeratlySleepDeprivedMentalHealthCutoff * _playerStats.GetMaxMentalHealth())
                {
                    _playerStats.SetPlayerMentalState(Types.PlayerMentalState.ModeratelySleepDeprived);
                }
                else if (currentMentalHealth <= MildlySleepDeprivedMentalHealthCutoff * _playerStats.GetMaxMentalHealth())
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