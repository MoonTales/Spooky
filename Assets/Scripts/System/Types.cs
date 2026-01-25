using Player;

namespace System
{
    /// <summary>
    /// a static class used to hold all of the types used throughout the project:
    /// including but not limted to: scruts, enums, and other type definitions.
    /// </summary>
    public static class Types
    {
        /// <summary>
        /// an enum representing the different states of a game.
        /// </summary>
        ///
        /* ------------------------ System Types ------------------------ */
        [Serializable]
        public enum GameState
        {
            MainMenu,
            Gameplay,
            Paused,
            GameOver,
            Victory,
            Cutscene,
            Inspecting
        }

        /// <summary>
        /// an Enum representing the different weather states
        /// </summary>
        [Serializable]
        public enum WeatherState
        {
            Clear,
            Rain,
            Snow,
        }
        
        /* ------------------------ System Types ------------------------ */


        
        /// <summary>
        /// Player related types
        /// </summary>
        
        /* ------------------------ Player Types ------------------------ */
        
        [Serializable]
        public enum PlayerMentalCoreState
        {
            None, 
            Anxious, // whenever we are in an anxiety related area (the nightmare)
            SleepDeprived, // whenever we are in the real world (the bedroom)
            Both
        }
        
        [Serializable]
        public enum PlayerMentalState
        {
            // for the anxiety side:
            Normal, // 100% - 80%
            MildlyAnxious, // 79% - 59%
            ModeratelyAnxious, // 58% - 29%
            SeverelyAnxious, // 28% - 10%
            Panic, // 9% - 1%
            // for the sleep deprivation side:
            MildlySleepDeprived, // 100% - 80%
            ModeratelySleepDeprived, // 60% - 25%
            SeverelySleepDeprived, // 25% - 10%
            Exhausted, // 10% - 1%
            // Common states:
            Breakdown, // when both anxiety and sleep deprivation are at their worst
        }
        

        [Serializable]
        public enum PlayerMovementState
        {
            Idle,
            Walking,
            Sprinting,
            CrouchIdle,
            CrouchWalking,
        }

        /// <summary>
        /// Struct to hold all of the players primary stats
        /// Notes:
        /// Player State is hooked to broadcasts when it changes
        /// </summary>
        /// [System.Serializable]
        public struct FPlayerStats
        {
            // Primary player stats
            private float _currentMentalHealth;  // the current health of the player
            private float _maxMentalHealth;      // the maximum health of the player
            private float _currentStamina; // the current stamina of the player
            private float _maxStamina;     // the maximum stamina of the player
            private float _movementSpeed;  // the movement speed of the player
            private PlayerMentalState _playerMentalState; // the current state of the player
            private PlayerMentalCoreState _playerMentalCoreState; // the current core mental state of the player (anxiety, sleep deprivation, both, none)
            
            // Getter, Setter, and Updater methods
            public float GetCurrentMentalHealth() { return _currentMentalHealth; } public void SetCurrentMentalHealth(float value) { _currentMentalHealth = value; } public void UpdateCurrentMentalHealth(float delta) { PlayerStats.Instance.UpdateCurrentMentalHealth(delta);}
            public float GetMaxMentalHealth() { return _maxMentalHealth; } public void SetMaxMentalHealth(float value) { _maxMentalHealth = value; } public float UpdateMaxMentalHealth(float delta) { _maxMentalHealth += delta; return _maxMentalHealth; }
            public float GetCurrentStamina() { return _currentStamina; } public void SetCurrentStamina(float value) { _currentStamina = value; } public float UpdateCurrentStamina(float delta) { _currentStamina += delta; return _currentStamina; }
            public float GetMaxStamina() { return _maxStamina; } public void SetMaxStamina(float value) { _maxStamina = value; } public float UpdateMaxStamina(float delta) { _maxStamina += delta; return _maxStamina; }
            public float GetMovementSpeed() { return _movementSpeed; } public void SetMovementSpeed(float value) { _movementSpeed = value; } public float UpdateMovementSpeed(float delta) { _movementSpeed += delta; return _movementSpeed; }
            public PlayerMentalState GetPlayerMentalState() { return _playerMentalState; }
            
            public PlayerMentalCoreState GetPlayerMentalCoreState() { return _playerMentalCoreState; } public void SetPlayerMentalCoreState(PlayerMentalCoreState coreState) { _playerMentalCoreState = coreState; }
            

            public void SetPlayerMentalState(PlayerMentalState mentalState, bool bShouldBroadcast = true)
            {
                if (bShouldBroadcast)
                {
                    // Only broadcast if the state is actually changing (dont broadcast if it "changed" to the same state)
                    if (_playerMentalState != mentalState)
                    {
                        EventBroadcaster.Broadcast_OnPlayerHealthStateChanged(mentalState);
                    }
                }
                _playerMentalState = mentalState;
            }
            
     

            public bool IsPlayerDead()
            {
                return _playerMentalState == PlayerMentalState.Breakdown;
            }
            
            public float GetMentalHealthPercentage()
            {
                if (_maxMentalHealth <= 0) return 0;
                return (_currentMentalHealth / _maxMentalHealth) * 100f;
            }
            
            public float GetStaminaPercentage()
            {
                if (_maxStamina <= 0) return 0;
                return (_currentStamina / _maxStamina) * 100f;
            }
            
            public void DebugPrintStats(){
                
                DebugUtils.Log("Player Stats:" +
                               "\nCurrent Mental Health: " + PlayerStats.Instance.GetPlayerStats().GetCurrentMentalHealth() +
                               "\n Max Mental Health: " + PlayerStats.Instance.GetPlayerStats().GetMaxMentalHealth() +
                               "\n Current Stamina: " + PlayerStats.Instance.GetPlayerStats().GetCurrentStamina() +
                               "\n Max Stamina: " + PlayerStats.Instance.GetPlayerStats().GetMaxStamina() +
                               "\n Movement Speed: " + PlayerStats.Instance.GetPlayerStats().GetMovementSpeed() +
                               "\n Player State: " + PlayerStats.Instance.GetPlayerStats().GetPlayerMentalState());
            }
        }
        
        /* ------------------------ End Player Types ------------------------ */
        
        /* ------------------------ Door Related Types ------------------------ */

        /// <summary>
        /// Struct used to hold the data for a key item (as in, a literal key)
        /// </summary>
        [Serializable]
        public struct FKeyData
        {
            private string _keyID;          // Unique identifier for the key
            private bool _bSingleUse;     // Indicates if the key is single-use
            private bool _bIsMasterKey;   // Indicates if the key is a master key (can open ANY door)
            
        }

        [Serializable]
        public struct FDoorData
        {
            private string _doorID;         // Unique identifier for the door
            private bool _bIsLocked;       // Indicates if the door is locked
            private string _requiredKeyID; // The ID of the key required to unlock the door
            
        }
        
        
        /* ------------------------ End Door Related Types ------------------------ */
        
        
        /* ------------------------ Flashlight Related Types ------------------------ */
        
        // Enum to hold the different battery states
        [Serializable]
        public enum FlashlightBatteryState
        {
            High,
            Medium,
            Low,
            Critical,
            Dead
        }
        
        
        /* ------------------------ End Flashlight Related Types ------------------------ */
        
    }
}
