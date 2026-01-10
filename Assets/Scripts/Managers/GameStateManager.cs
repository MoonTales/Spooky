using System;
using UnityEngine;
using Types = System.Types;

namespace Managers
{
    /// <summary>
    /// Class used to manage the overall state of the game.
    /// </summary>
    public class GameStateManager : Singleton<GameStateManager>
    {

        
        // Game state manager can send broadcats for when the game starts, pauses, resumes, and ends.
        // private local variables to track the game state
        private Types.GameState _currentGameState = Types.GameState.Gameplay;

        public void Start()
        {
            // Initialize the game state
            _currentGameState = Types.GameState.Gameplay;
            DebugUtils.LogSuccess("GameStateManager initialized. Current Game State: " + _currentGameState);
            
            // for now, we will assume the game starts
            EventBroadcaster.Broadcast_GameStateChanged(_currentGameState);
            // Broadcast that the game has started
            EventBroadcaster.Broadcast_GameStarted();
        }

        protected void Update()
        {
            // small update to show how this would work
            if(Input.GetKeyDown(KeyCode.P))
            {
                EventBroadcaster.Broadcast_OnPlayerDamaged(10.0f);
            }
        }
        
        
        
    }
}
