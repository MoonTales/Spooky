using UnityEngine;
using UnityEngine.UI;

namespace System
{
    /// <summary>
    /// A static utility class for broadcasting and handling activity-related events.
    /// Provides functionality for notifying subscribers when an activity starts or completes.
    /// Subscribers can listen for these events and respond accordingly.
    /// 
    /// Template for how to set up a new event
    /// 
    ///  public delegate void EventNameHandler(ParameterType parameter);
    ///  public static event EventNameHandler EventName;
    ///  public static void Broadcast_EventName(ParameterType parameter) { EventName?.Invoke(parameter); }
    /// 
    /// 
    ///  in the class that is going to SUBSCRIBE to the event, do the following:
    ///  public void start(){
    ///     EventBroadcaster.EventName += YourClassMethod;
    ///  }
    /// 
    ///  in the class that is going to BROADCAST the event, do the following:
    ///  EventBroadcaster.Broadcast_EventName(parameter);
    /// 
    ///
    /// Created by: MoonTales
    /// </summary>
    public static class EventBroadcaster
    {

        /* Template for how to setup a new event
         *
         * public delegate void EventNameHandler(ParameterType parameter);
         * public static event EventNameHandler EventName;
         * public static void Broadcast_EventName(ParameterType parameter) { EventName?.Invoke(parameter); }
         *
         *
         * in the class that is going to subscribe to the event, do the following:
         * public void start(){
         *    EventBroadcaster.EventName += YourClassMethod;
         * }
         *
         * in the class that is going to broadcast the event, do the following:
         * EventBroadcaster.Broadcast_EventName(parameter);
         */

        // These are just for show, they will be removed soon

        
        /// <summary>
        /// Global Project level broadcasts.
        ///
        /// These are broadcast intended to be used as major gameplay markings, which
        /// are automatically connected to each and ever class which inherits from the
        /// EventSubscriber Base.
        ///
        ///
        /// OnGameStarted -> Called when the game originally starts (not the main menu. actual gameplay)
        /// 
        /// OnGameInitialized -> Called after the game has been initialized
        ///     Controlled by a game manager of some sort. should be called after all
        ///     Systems have been "Started" such as stats, generation, player setup, etc.
        ///
        /// OnGameRestarted -> Called when the game is restarted (after a game over)
        /// 
        /// </summary>
        
        
        public delegate void GameStartedHandler();
        public static event GameStartedHandler OnGameStarted;
        public static void Broadcast_GameStarted() { OnGameStarted?.Invoke();}

        public delegate void GameInitializedHandler();
        public static event GameInitializedHandler OnGameInitialized;
        public static void Broadcast_GameInitialized() {OnGameInitialized?.Invoke();}
        
        public delegate void GameRestartedHandler();
        public static event GameRestartedHandler OnGameRestarted;
        public static void Broadcast_GameRestarted() {OnGameRestarted?.Invoke();}

        public delegate void GameStateChangedHandler(Types.GameState newState);
        public static event GameStateChangedHandler OnGameStateChanged;
        public static void Broadcast_GameStateChanged(Types.GameState newState) { OnGameStateChanged?.Invoke(newState); }
        //---------------------------------------------------------------------------------//

    
        /// <summary>
        /// Player specific broadcasts.
        ///
        /// A series of broadcasts that effect or in some way relate to the player.
        ///
        /// DamagePlayer -> Allows anyone to easily apply damage to the player
        /// OnPlayerStateChanged -> Broadcasts when the player's state has changed (Healthy, Injured, Critical, Dead)
        /// </summary>
        
        public delegate void DamagePlayerHandler(float damageAmount);
        public static event DamagePlayerHandler OnPlayerDamaged;
        public static void Broadcast_OnPlayerDamaged(float damageAmount) { OnPlayerDamaged?.Invoke(damageAmount); }
        
        
        public delegate void OnPlayerHealthStateChangedHandler(Types.PlayerMentalState newMentalState);
        public static event OnPlayerHealthStateChangedHandler OnPlayerHealthStateChanged;
        public static void Broadcast_OnPlayerHealthStateChanged(Types.PlayerMentalState newMentalState) { OnPlayerHealthStateChanged?.Invoke(newMentalState); }

        public delegate void OnTerrorIntensityChangedHandler(float normalizedIntensity);
        public static event OnTerrorIntensityChangedHandler OnTerrorIntensityChanged;
        public static void Broadcast_OnTerrorIntensityChanged(float normalizedIntensity) { OnTerrorIntensityChanged?.Invoke(normalizedIntensity); }
        
        
        public delegate void OnFlashlightToggledHandler(bool isOn);
        public static event OnFlashlightToggledHandler OnFlashlightToggled;
        public static void Broadcast_OnFlashlightToggled(bool isOn) { OnFlashlightToggled?.Invoke(isOn); }
        
        public delegate void OnFlashlightHitEnemyHandler(GameObject enemy, bool isOnEnemy);
        public static event OnFlashlightHitEnemyHandler OnFlashlightHitEnemy;
        public static void Broadcast_OnFlashlightHitEnemy(GameObject enemy, bool isOnEnemy){ OnFlashlightHitEnemy?.Invoke(enemy, isOnEnemy); }
        //-------------------------------- End Activity Events --------------------------------//
        
        
        // broadcast to show when we started hovering our cursor ro interact with an interactable object
        public delegate void OnBeganHoverInteractableHandler(IInteractable interactable);
        public static event OnBeganHoverInteractableHandler OnBeganHoverInteractable;
        public static void Broadcast_OnBeganHoverInteractable(IInteractable interactable) { OnBeganHoverInteractable?.Invoke(interactable); }
        // broadcast to show when we stopped hovering our cursor ro interact with an interactable object
        // we dont need any references here
        public delegate void OnEndedHoverInteractableHandler();
        public static event OnEndedHoverInteractableHandler OnEndedHoverInteractable;
        public static void Broadcast_OnEndedHoverInteractable() { OnEndedHoverInteractable?.Invoke(); }
        
        ///<summary>
        /// World Clock Events
        /// </summary>
        public delegate void OnWorldClockHandler(int clockHour);
        public static event OnWorldClockHandler OnWorldClockHourChanged;
        public static void Broadcast_OnWorldClockHourChanged(int clockHour) { OnWorldClockHourChanged?.Invoke(clockHour);}
        
        public delegate void OnWorldLocationChanged(Types.WorldLocation newLocation);

        public static event OnWorldLocationChanged OnWorldLocationChangedEvent;
        public static void Broadcast_OnWorldLocationChanged(Types.WorldLocation newLocation) { OnWorldLocationChangedEvent?.Invoke(newLocation); }
    }
}
