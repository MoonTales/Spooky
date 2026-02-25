using System;
using Unity.Cinemachine;
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
        [SerializeField] private string defaultSpawnPointID = "DEFAULT_SPAWN_POINT";
        
        
        void Start()
        {
            
            SearchForSpawnAnchor(defaultSpawnPointID);
        }
        
        protected override void Awake()
        {
            base.Awake();
            // Get reference to the player
            _player = GameObject.FindWithTag("Player");
        }
        
        public CinemachineCamera GetCinemachineCamera()
        {
            CinemachineCamera cineCam = _player.GetComponentInChildren<CinemachineCamera>();
            if (cineCam == null)
            {
                DebugUtils.LogError("CinemachineCamera component not found on player!");
            }
            return cineCam;
        }
        

        
        // --- Event connections --- //
        protected override void RegisterSubscriptions()
        {
            base.RegisterSubscriptions();
            TrackSubscription(() => EventBroadcaster.OnPlayerHealthStateChanged += OnPlayerStateChanged,
                () => EventBroadcaster.OnPlayerHealthStateChanged -= OnPlayerStateChanged);
        }
        
        public void SearchForSpawnAnchor(string spawnPointID = "")
        {
            // reset flashlight to default intensity upon respawn
            Flashlight.Instance.GetComponent<Animator>().SetBool("Increase", false);

            // logic to search for a spawn anchor in the scene
            PlayerSpawnAnchor[] spawnAnchors = GameObject.FindObjectsByType<PlayerSpawnAnchor>(FindObjectsSortMode.None);
            
            // debug print the number of spawn anchors found
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
                if (Anchor != null && Anchor.gameObject != null && Anchor.GetSpawnPointID() == spawnPointID)
                {
                    // found the default spawn point, move the player here
                    if (_player != null)
                    {
                        TeleportPlayer(Anchor.gameObject.transform.position, Anchor.gameObject.transform.rotation);
                        return;
                    }
                }
            }
            // if we reach here, we did not find the default spawn point, so we will just use the first one we found
            if (FirstAnchor)
            {
                TeleportPlayer(FirstAnchor.gameObject.transform.position, FirstAnchor.gameObject.transform.rotation);
                
                return;
            }
            
            // if we still dont have a player position, we will just spawn at the world origin
            TeleportPlayer(Vector3.zero);
            
        }
        
        private void OnPlayerStateChanged(Types.PlayerMentalState newMentalState)
        {
            
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
        public void TeleportPlayer(Vector3 newPosition, Quaternion? rotation = null)
        {
            if (_player == null) return;

            CharacterController controller = _player.GetComponent<CharacterController>();

            if (controller != null) {controller.enabled = false;}

            _player.transform.position = newPosition;
    
            if (rotation.HasValue)
            {
                float yaw = rotation.Value.eulerAngles.y;
                var CinemaCamera = GetCinemachineCamera();
                var panTilt = CinemaCamera.GetComponent<CinemachinePanTilt>();
    
                if (panTilt != null)
                {
                    panTilt.PanAxis.Value = yaw;
                    panTilt.TiltAxis.Value = 0;
                }
            }

            if (controller != null) {controller.enabled = true;}

        }
        
        public Transform GetPlayerHandTransform()
        {
            if (_player == null) {return null;}
            
            // look through all child transforms to find the one named "HAND"
            Transform[] childTransforms = _player.GetComponentsInChildren<Transform>();
            foreach (Transform child in childTransforms)
            {
                if (child.name == "HAND")
                {
                    return child;
                }
            }
            return null;
        }

        public float GetDistance(Vector3 objectPosition)
        {
            // Calculates the Euclidian distance between the requesting object and player
            // Takes in Vector3 of the requesting object obtained through transform.position
            // Returns 0.0f and throws error if _player not found

            if (_player != null)
            {
                return Vector3.Distance(objectPosition, _player.transform.position);
            }
            else
            {
                return 0.0f;
            }

        }
    }
}