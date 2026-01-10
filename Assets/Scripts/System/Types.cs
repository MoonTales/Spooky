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
        public enum GameState
        {
            MainMenu,
            Gameplay,
            Paused,
            GameOver,
            Victory
        }

        /// <summary>
        /// an Enum representing the different weather states
        /// </summary>
        public enum WeatherState
        {
            Clear,
            Rain,
            Snow,
        }


        
        /// <summary>
        /// Player related types
        /// </summary>
        
        /* ------------------------ Player Types ------------------------ */
        public enum PlayerState
        {
            Healthy,
            Injured,
            Critical,
            Dead,
            // System states
            Cutscene,
        }

        /// <summary>
        /// Struct to hold all of the players primary stats
        /// Notes:
        /// Player State is hooked to broadcasts when it changes
        /// </summary>
        public struct FPlayerStats
        {
            // Primary player stats
            private float _currentHealth;  // the current health of the player
            private float _maxHealth;      // the maximum health of the player
            private float _currentStamina; // the current stamina of the player
            private float _maxStamina;     // the maximum stamina of the player
            private float _movementSpeed;  // the movement speed of the player
            private PlayerState _playerState; // the current state of the player
            
            // Getter, Setter, and Updater methods
            public float GetCurrentHealth() { return _currentHealth; } public void SetCurrentHealth(float value) { _currentHealth = value; } public void UpdateCurrentHealth(float delta) { PlayerStats.Instance.UpdateCurrentHealth(delta);}
            public float GetMaxHealth() { return _maxHealth; } public void SetMaxHealth(float value) { _maxHealth = value; } public float UpdateMaxHealth(float delta) { _maxHealth += delta; return _maxHealth; }
            public float GetCurrentStamina() { return _currentStamina; } public void SetCurrentStamina(float value) { _currentStamina = value; } public float UpdateCurrentStamina(float delta) { _currentStamina += delta; return _currentStamina; }
            public float GetMaxStamina() { return _maxStamina; } public void SetMaxStamina(float value) { _maxStamina = value; } public float UpdateMaxStamina(float delta) { _maxStamina += delta; return _maxStamina; }
            public float GetMovementSpeed() { return _movementSpeed; } public void SetMovementSpeed(float value) { _movementSpeed = value; } public float UpdateMovementSpeed(float delta) { _movementSpeed += delta; return _movementSpeed; }
            public PlayerState GetPlayerState() { return _playerState; }

            public void SetPlayerState(PlayerState state, bool bShouldBroadcast = true)
            {
                if (bShouldBroadcast)
                {
                    // Only broadcast if the state is actually changing (dont broadcast if it "changed" to the same state)
                    if (_playerState != state)
                    {
                        EventBroadcaster.Broadcast_OnPlayerStateChanged(state);
                    }
                }
                _playerState = state;
            } 
        }

        
            public static void DebugPrintStats(){
                
                DebugUtils.Log("Player Stats:" +
                               "\nCurrent Health: " + PlayerStats.Instance.GetPlayerStats().GetCurrentHealth() +
                               "\n Max Health: " + PlayerStats.Instance.GetPlayerStats().GetMaxHealth() +
                               "\n Current Stamina: " + PlayerStats.Instance.GetPlayerStats().GetCurrentStamina() +
                               "\n Max Stamina: " + PlayerStats.Instance.GetPlayerStats().GetMaxStamina() +
                               "\n Movement Speed: " + PlayerStats.Instance.GetPlayerStats().GetMovementSpeed() +
                               "\n Player State: " + PlayerStats.Instance.GetPlayerStats().GetPlayerState());
            }
        
        /* ------------------------ End Player Types ------------------------ */
        
    }
}
