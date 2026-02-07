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

        [SerializeField] private int _maxDrawingsPerAct = 3; public int GetMaxDrawingsPerAct() { return _maxDrawingsPerAct; }
        
        // Game state manager can send broadcats for when the game starts, pauses, resumes, and ends.
        // private local variables to track the game state
        private Types.GameState _currentGameState = Types.GameState.MainMenu; public Types.GameState GetCurrentGameState() { return _currentGameState; }
        private Types.WorldLocation _currentWorldLocation = new Types.WorldLocation(); public Types.WorldLocation GetCurrentWorldLocation() { return _currentWorldLocation; }
        
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
            TrackSubscription(() => EventBroadcaster.OnWorldLocationChangedEvent += OnWorldLocationChanged,
                () => EventBroadcaster.OnWorldLocationChangedEvent -= OnWorldLocationChanged);

        }
        
        
        public void SetWorldClockHour(int hour)
        {
            _currentWorldClockHour = hour;
            EventBroadcaster.Broadcast_OnWorldClockHourChanged(_currentWorldClockHour);
        }

        private void OnWorldLocationChanged(Types.WorldLocation worldLocation)
        {
            _currentWorldLocation = worldLocation;
        }

        private void OnPlayerHealthStateChanged(Types.PlayerMentalState newhealthstate)
        {
            // check for a player death
            if (newhealthstate == Types.PlayerMentalState.Breakdown)
            {
                
                PlayerStats.Instance.ResetAllStatsToDefault();
                
                // check the core state of the player
                Types.PlayerMentalCoreState coreState = PlayerStats.Instance.GetPlayerStats().GetPlayerMentalCoreState();
                if (coreState == Types.PlayerMentalCoreState.Anxious)
                {
                    // this means the player was anxious death (they were in the nightmare, and need to reset to bedroom)

                    SceneSwapper.Instance.SwapScene("Bedroom");
                    // swap the core state to sleep deprived
                    PlayerStats.Instance.SetMentalCoreState(Types.PlayerMentalCoreState.SleepDeprived);
                    EventBroadcaster.Broadcast_OnPlayerHealthStateChanged(Types.PlayerMentalState.Normal);
                }
                else if (coreState == Types.PlayerMentalCoreState.SleepDeprived)
                {
                    // this means the player fell asleep while in the bedroom, and should be sent to the nightmare

                    SceneSwapper.Instance.SwapScene("Nightmare1");
                    // swap the core state to anxious
                    PlayerStats.Instance.SetMentalCoreState(Types.PlayerMentalCoreState.Anxious);
                    EventBroadcaster.Broadcast_OnPlayerHealthStateChanged(Types.PlayerMentalState.Normal);

                }
                
                
            }
            
        }

        protected void Update()
        {

            if(Input.GetKeyDown(KeyCode.G))
            {
                DebugUtils.Log("Switching to Gameplay State");
                EventBroadcaster.Broadcast_GameStateChanged(Types.GameState.Gameplay);
            }
            
            if (Input.GetKeyDown(KeyCode.Y))
            {
                EventBroadcaster.Broadcast_OnWorldClockHourChanged(_currentWorldClockHour += 1);
            }
            
            if (Input.GetKeyDown(KeyCode.Z))
            {
                EventBroadcaster.Broadcast_GameRestarted();
            }
            if (Input.GetKeyDown(KeyCode.N))
            {
                Types.NotificationData data = new(
                    duration: 5.0f, 
                    messageKey: new TextKey { place = "Tutorial", id = "WASD"},
                    messageOverride: "Custom Notification: Use WASD to move around!"
                );
                data.Send();
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
