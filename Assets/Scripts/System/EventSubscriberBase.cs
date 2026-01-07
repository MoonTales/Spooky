using System.Collections.Generic;
using UnityEngine;

namespace System
{
    /// <summary>
    /// A utility abstract class intended to go hand-in-hand with the EventBroadcaster.
    ///
    /// This provides a base MonoBehaviour class that can be inherited by any class,
    /// and will automtically handle to connection and disconnection from the EventBroadcaster.
    ///
    /// by defuault, it connects on OnEnabled, and disconnects on OnDisable or OnDestroy.
    ///
    /// To create a new event you wish to subscribe to, please use the following pattern:
    ///
    /// --- HOW TO USE ---
    /// Assume you wish to connect to the EventBroadcaster.OnPlayerDeath event
    /// Assume you have a function called Func1
    ///
    ///
    /// 1) Over-ride the RegisterSubscriptions() method, with the following code:
    ///         protected override void RegisterSubscriptions(){}
    ///         MAKE SURE TO CALL SUPER!
    /// 
    ///
    /// 2) Inside of the RegisterSubscriptions function, add the following line:
    ///    TrackSubscription( () => EventBroadcaster.PlayerDeath += Func1, () => EventBroadcaster.PlayerDeath -= Func1 );
    ///
    ///    this links up the subscription and un-subscription actions automatically.
    ///
    /// 3) Now, whenever a class broadcasts the EventBroadcaster.PlayerDeath event,
    ///    Func1 will be called automatically on this class.
    ///
    /// Created by: MoonTales
    /// </summary>
    public abstract class EventSubscriberBase : MonoBehaviour
    {
        // List of subscription actions
        private readonly List<Action> unsubscribeActions = new();
        protected bool BConnectToGlobalEvents = false;

        // virtual function used by child classes to track their subscriptions
        // these should be called exclusively within the RegisterSubscriptions method
        protected void TrackSubscription(Action subscribe, Action unsubscribe)
        {
            subscribe();
            unsubscribeActions.Add(unsubscribe);
        }

        // virtual function used by child classes to register their subscriptions
        // (by using the TrackSubscription method)
        protected virtual void RegisterSubscriptions()
        {
            if (!BConnectToGlobalEvents) { return; }
            TrackSubscription(() => EventBroadcaster.OnGameStarted += OnGameStarted,
                () => EventBroadcaster.OnGameStarted -= OnGameStarted);
            TrackSubscription(() => EventBroadcaster.OnGameInitialized += OnGameInitialized,
                () => EventBroadcaster.OnGameInitialized -= OnGameInitialized);
            TrackSubscription(() => EventBroadcaster.OnGameRestarted += OnGameRestarted,
                () => EventBroadcaster.OnGameRestarted -= OnGameRestarted);

        }

        protected virtual void OnEnable()
        {
            RegisterSubscriptions();
        }

        protected virtual void OnDisable()
        {
            Cleanup();
        }

        protected virtual void OnDestroy()
        {
            Cleanup();
        }

        private void Cleanup()
        {
            foreach (Action unsubscribe in unsubscribeActions)
            {
                if (unsubscribe != null)
                {
                    unsubscribe?.Invoke();
                }
            }

            // Clear the list after unsubscribing
            unsubscribeActions.Clear();
        }

        /* Global Subscription functions for all children*/
        protected virtual void OnGameStarted(){}
        protected virtual void OnGameInitialized(){}
        protected virtual void OnGameRestarted(){}
    }
}
