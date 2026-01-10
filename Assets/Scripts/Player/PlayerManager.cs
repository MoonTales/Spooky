using System;
using UnityEngine;
using Types = System.Types;

namespace Player
{
    /// <summary>
    /// Class used to manager player related functionality
    ///     NOTE: this does not include player input, that is handled via the PlayerController class
    /// </summary>
    public class PlayerManager : Singleton<PlayerManager>
    {
        private GameObject _player;
        
        
        void Start()
        {
            // Get reference to the player
            _player = GameObject.FindWithTag("Player");
        }
        
        // --- Event connections --- //
        protected override void RegisterSubscriptions()
        {
            base.RegisterSubscriptions();
            TrackSubscription(() => EventBroadcaster.OnPlayerStateChanged += OnPlayerStateChanged,
                () => EventBroadcaster.OnPlayerStateChanged -= OnPlayerStateChanged);
            
        }
        
        private void OnPlayerStateChanged(Types.PlayerState newState)
        {
            DebugUtils.LogSuccess("Player state changed to: " + newState.ToString());
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
            return _player;
        }

        /// <summary>
        /// Teleports player immediately
        /// </summary>
        public void TeleportPlayer(Vector3 newPosition)
        {
            if (_player == null) { return; }
            _player.transform.position = newPosition;
        }
        
        
    }
}