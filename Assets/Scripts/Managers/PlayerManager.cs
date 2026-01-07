using System;
using System.Collections;
using UnityEngine;
using Types = System.Types;

namespace Managers
{
    /// <summary>
    /// Class used to manager player related functionality
    ///     NOTE: this does not include player input, that is handled via the PlayerController class
    /// </summary>
    public class PlayerManager : Singleton<PlayerManager>
    {
        private GameObject player;
        void Start()
        {
            // Get reference to the player
            player = GameObject.FindWithTag("Player");
        }
        
        // --- Event connections --- //
        protected override void RegisterSubscriptions()
        {
            base.RegisterSubscriptions();
            TrackSubscription(() => EventBroadcaster.OnPlayerDamaged += OnPlayerDamaged,
                () => EventBroadcaster.OnPlayerDamaged -= OnPlayerDamaged);
            
        }
        
        private void OnPlayerDamaged(float InDamage)
        {
            // now, any class in the entire project can call EventBroadcaster.Broadcast_OnPlayerDamaged(damageAmount);
            // and itll damage the player, without having to know anything about the player class.
            // all functionality can be handled via the player class itself.
            DebugUtils.LogSuccess("Player took " + InDamage + " damage! This is from the Broadcast system!!!");
        }
        
        
        // Global broadcasts we have access to as a child class
        protected override void OnGameStarted()
        {
            // we can just do stuff here, like reset player stats, position, etc.
            // as we are provided this functionality via the EventSubscriberBase class.
        }
        
        /// <summary>
        /// Returns the primary instance of the player prefab
        /// </summary>
        public GameObject GetPlayer()
        {
            return player;
        }

        /// <summary>
        /// Teleports player immediately (optionally with screen fade)
        /// </summary>
        public void TeleportPlayer(Vector3 newPosition)
        {
            if (player == null) { return; }
            player.transform.position = newPosition;
        }
        
    }
}