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
        [SerializeField] private int _MaxDrawingsInGame = 9; public int GetMaxDrawingsInGame() { return _MaxDrawingsInGame; }
        // Game state manager can send broadcats for when the game starts, pauses, resumes, and ends.
        // private local variables to track the game state
        private Types.GameState _currentGameState = Types.GameState.MainMenu; public Types.GameState GetCurrentGameState() { return _currentGameState; }
        private Types.GameState _previousGameState = Types.GameState.MainMenu; public Types.GameState GetPreviousGameState() { return _previousGameState; }
        private Types.WorldLocation _currentWorldLocation = new Types.WorldLocation(); public Types.WorldLocation GetCurrentWorldLocation() { return _currentWorldLocation; }
        
        private int _currentZoneId = 0; public int GetCurrentZoneId() { return _currentZoneId; } public void SetCurrentZoneId(int zoneId) { _currentZoneId = zoneId; }
        
        private int _currentWorldClockHour = 1; public int GetCurrentWorldClockHour() { return _currentWorldClockHour; }
        public void Start()
        {
            // Initialize the game state
            _currentGameState = Types.GameState.MainMenu;
            _previousGameState = _currentGameState;
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
            TextDB.SetCurrentAct(_currentWorldClockHour);
            EventBroadcaster.Broadcast_OnWorldClockHourChanged(_currentWorldClockHour);
        }

        public void CycleWorldLocation(int direction)
        {
            int count = Enum.GetValues(typeof(Types.WorldLocation)).Length;
            int nextIndex = ((int)_currentWorldLocation + direction) % count;
            if (nextIndex < 0)
            {
                nextIndex += count;
            }

            EventBroadcaster.Broadcast_OnWorldLocationChanged((Types.WorldLocation)nextIndex);
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

                    SleepTrackerManager.Instance.SetIsGoodWakeup(false);
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

        protected override void OnGameRestarted()
        {
            // when we restart, we wanna reset the world clock hour back to 1
            SetWorldClockHour(1);
        }


        
        protected override void OnGameStateChanged(Types.GameState newState)
        {
            
            // we need to watch for a few edge cases
            //1. If we go from MainMenu -> Gameplay, we know the game has started
            if (_currentGameState == Types.GameState.MainMenu && newState == Types.GameState.Gameplay)
            {
                EventBroadcaster.Broadcast_GameStarted();
                // this also means we can broadcast the first WorldClock tick
                EventBroadcaster.Broadcast_OnWorldClockHourChanged(_currentWorldClockHour);
                
            }
            
            //2. If we EVER return to the main menu, we can consider that a game restart
            if (_currentGameState != Types.GameState.MainMenu && newState == Types.GameState.MainMenu)
            {
                
                EventBroadcaster.Broadcast_GameRestarted();
            }
            
            _previousGameState = _currentGameState;
            _currentGameState = newState;
        }
    }
}
