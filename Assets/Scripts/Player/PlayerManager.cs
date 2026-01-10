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
        private 
        
        
        void Start()
        {
            // Get reference to the player
            _player = GameObject.FindWithTag("Player");
            SearchForSpawnAnchor();
        }
        
        // --- Event connections --- //
        protected override void RegisterSubscriptions()
        {
            base.RegisterSubscriptions();
            TrackSubscription(() => EventBroadcaster.OnPlayerStateChanged += OnPlayerStateChanged,
                () => EventBroadcaster.OnPlayerStateChanged -= OnPlayerStateChanged);
        }
        
        private void SearchForSpawnAnchor()
        {
            // logic to search for a spawn anchor in the scene
            PlayerSpawnAnchor[] spawnAnchors = GameObject.FindObjectsOfType<PlayerSpawnAnchor>();
            
            // loop through all of the spawn anchors to find the default one
            PlayerSpawnAnchor FirstAnchor = null;
            foreach (PlayerSpawnAnchor Anchor in spawnAnchors)
            {
                // store the first anchor we find
                if (FirstAnchor == null)
                {
                    FirstAnchor = Anchor;
                }
                // Search for the default spawn point ID
                if (Anchor != null && Anchor.gameObject != null && Anchor.GetSpawnPointID() == "DEFAULT_SPAWN_POINT")
                {
                    // found the default spawn point, move the player here
                    if (_player != null)
                    {
                        TeleportPlayer(Anchor.gameObject.transform.position);
                        DebugUtils.LogSuccess("Player spawned at default spawn point: " + Anchor.GetSpawnPointID());
                        return;
                    }
                }
            }
            // if we reach here, we did not find the default spawn point, so we will just use the first one we found
            if (FirstAnchor)
            {
                TeleportPlayer(FirstAnchor.gameObject.transform.position);
            }
            
            // if we still dont have a player position, we will just spawn at the world origin
            DebugUtils.LogWarning("No PlayerSpawnAnchor found, spawning player at world origin (0,0,0)");
            TeleportPlayer(Vector3.zero);
            
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