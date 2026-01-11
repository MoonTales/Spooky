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
        public enum PlayerHealthState
        {
            Healthy,
            Injured,
            Critical,
            Dead
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
            private float _currentHealth;  // the current health of the player
            private float _maxHealth;      // the maximum health of the player
            private float _currentStamina; // the current stamina of the player
            private float _maxStamina;     // the maximum stamina of the player
            private float _movementSpeed;  // the movement speed of the player
            private PlayerHealthState _playerHealthState; // the current state of the player
            
            // Getter, Setter, and Updater methods
            public float GetCurrentHealth() { return _currentHealth; } public void SetCurrentHealth(float value) { _currentHealth = value; } public void UpdateCurrentHealth(float delta) { PlayerStats.Instance.UpdateCurrentHealth(delta);}
            public float GetMaxHealth() { return _maxHealth; } public void SetMaxHealth(float value) { _maxHealth = value; } public float UpdateMaxHealth(float delta) { _maxHealth += delta; return _maxHealth; }
            public float GetCurrentStamina() { return _currentStamina; } public void SetCurrentStamina(float value) { _currentStamina = value; } public float UpdateCurrentStamina(float delta) { _currentStamina += delta; return _currentStamina; }
            public float GetMaxStamina() { return _maxStamina; } public void SetMaxStamina(float value) { _maxStamina = value; } public float UpdateMaxStamina(float delta) { _maxStamina += delta; return _maxStamina; }
            public float GetMovementSpeed() { return _movementSpeed; } public void SetMovementSpeed(float value) { _movementSpeed = value; } public float UpdateMovementSpeed(float delta) { _movementSpeed += delta; return _movementSpeed; }
            public PlayerHealthState GetPlayerState() { return _playerHealthState; }

            public void SetPlayerState(PlayerHealthState healthState, bool bShouldBroadcast = true)
            {
                if (bShouldBroadcast)
                {
                    // Only broadcast if the state is actually changing (dont broadcast if it "changed" to the same state)
                    if (_playerHealthState != healthState)
                    {
                        EventBroadcaster.Broadcast_OnPlayerStateChanged(healthState);
                    }
                }
                _playerHealthState = healthState;
            }

            public bool IsPlayerDead()
            {
                return _playerHealthState == PlayerHealthState.Dead;
            }
            
            public float GetHealthPercentage()
            {
                if (_maxHealth <= 0) return 0;
                return (_currentHealth / _maxHealth) * 100f;
            }
            
            public float GetStaminaPercentage()
            {
                if (_maxStamina <= 0) return 0;
                return (_currentStamina / _maxStamina) * 100f;
            }
            
            public void DebugPrintStats(){
                
                DebugUtils.Log("Player Stats:" +
                               "\nCurrent Health: " + PlayerStats.Instance.GetPlayerStats().GetCurrentHealth() +
                               "\n Max Health: " + PlayerStats.Instance.GetPlayerStats().GetMaxHealth() +
                               "\n Current Stamina: " + PlayerStats.Instance.GetPlayerStats().GetCurrentStamina() +
                               "\n Max Stamina: " + PlayerStats.Instance.GetPlayerStats().GetMaxStamina() +
                               "\n Movement Speed: " + PlayerStats.Instance.GetPlayerStats().GetMovementSpeed() +
                               "\n Player State: " + PlayerStats.Instance.GetPlayerStats().GetPlayerState());
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
        
        
    }
}
