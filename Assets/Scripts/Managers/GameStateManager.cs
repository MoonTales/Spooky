using System;
using Player;
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
        private Types.GameState _currentGameState = Types.GameState.MainMenu;
        private int _currentWorldClockHour = 1; public int GetCurrentWorldClockHour() { return _currentWorldClockHour; }
        public void Start()
        {
            // Initialize the game state
            _currentGameState = Types.GameState.MainMenu;
            // for now, we will assume the game starts
            EventBroadcaster.Broadcast_GameStateChanged(_currentGameState);
        }
        
        protected override void RegisterSubscriptions()
        {
            base.RegisterSubscriptions();
            // Example subscription to player health state changes
            TrackSubscription(() => EventBroadcaster.OnPlayerHealthStateChanged += OnPlayerHealthStateChanged,
                () => EventBroadcaster.OnPlayerHealthStateChanged -= OnPlayerHealthStateChanged);
        }

        private void OnPlayerHealthStateChanged(Types.PlayerMentalState newhealthstate)
        {
            // check for a player death
            if (newhealthstate == Types.PlayerMentalState.Breakdown)
            {
                // Handle what should happens when the player Dies
                PlayerStats.Instance.ResetAllStatsToDefault();
                SceneSwapper.Instance.SwapScene("Bedroom");
            }
        }

        protected void Update()
        {
            // small update to show how this would work
            if(Input.GetKeyDown(KeyCode.X))
            {
                DebugUtils.Log("Player Damaged Event Broadcasted with 10.0f damage");
                EventBroadcaster.Broadcast_OnPlayerDamaged(10.0f);
            }
            if(Input.GetKeyDown(KeyCode.G))
            {
                DebugUtils.Log("Switching to Gameplay State");
                EventBroadcaster.Broadcast_GameStateChanged(Types.GameState.Gameplay);
            }
            if(Input.GetKeyDown(KeyCode.K))
            {
                DebugUtils.Log("Switching to Cutscene State");
                EventBroadcaster.Broadcast_GameStateChanged(Types.GameState.Cutscene);
            }
            if(Input.GetKeyDown(KeyCode.M))
            {
                DebugUtils.Log("Switching to MainMenu State");
                EventBroadcaster.Broadcast_GameStateChanged(Types.GameState.MainMenu);
            }
            
            if(Input.GetKeyDown(KeyCode.O))
            {
                SceneSwapper.Instance.SwapScene("Bedroom");
            }

            if (Input.GetKeyDown(KeyCode.I))
            {
                SceneSwapper.Instance.SwapScene("Cohen");
            }
            if (Input.GetKeyDown(KeyCode.P))
            {
                SceneSwapper.Instance.SwapScene("FirstAiTest");
            }
            
            if (Input.GetKeyDown(KeyCode.Y))
            {
                EventBroadcaster.Broadcast_OnWorldClockHourChanged(_currentWorldClockHour += 1);
            }
            
            
        }


        protected override void OnGameStateChanged(Types.GameState newState)
        {
            
            // we need to watch for a few edge cases
            //1. If we go from MainMenu -> Gameplay, we know the game has started
            if (_currentGameState == Types.GameState.MainMenu && newState == Types.GameState.Gameplay)
            {
                EventBroadcaster.Broadcast_GameStarted();
                // this also means we can broadcast the first WorldClock tick
                DebugUtils.Log("Broadcasting Initial World Clock Hour Change: " + _currentWorldClockHour);
                EventBroadcaster.Broadcast_OnWorldClockHourChanged(_currentWorldClockHour);
                
            }
            
            //2. If we EVER return to the main menu, we can consider that a game restart
            if (_currentGameState != Types.GameState.MainMenu && newState == Types.GameState.MainMenu)
            {
                EventBroadcaster.Broadcast_GameRestarted();
            }
            
            
            _currentGameState = newState;
        }
    }
}
