using System;
using System.Collections.Generic;
using Placeables;
using UnityEngine;
using Types = System.Types;

namespace Managers
{
    /// <summary>
    /// Singleton class used to Control and Manage all Spawn Anchors throughout the game
    /// </summary>
    public class SpawnAnchorManager : Singleton<SpawnAnchorManager>
    {

        private List<SpawnAnchor> spawnAnchorsInScene = new List<SpawnAnchor>();

        protected override void RegisterSubscriptions()
        {
            base.RegisterSubscriptions();
            TrackSubscription(()=> EventBroadcaster.OnWorldLocationChangedEvent += OnWorldLocationChanged,
                () => EventBroadcaster.OnWorldLocationChangedEvent -= OnWorldLocationChanged);
        }

        private void Start()
        {
            UpdateSpawnAnchorsInScene();
        }

        private void OnWorldLocationChanged(Types.WorldLocation newlocation)
        {
            // anytime we change world locations, we want to update our list of spawn anchors in the scene to reflect the new location
            UpdateSpawnAnchorsInScene();
        }

        private void UpdateSpawnAnchorsInScene()
        {
            spawnAnchorsInScene.Clear();
            // find all spawn anchors in the scene and add them to our list
            SpawnAnchor[] anchors = FindObjectsOfType<SpawnAnchor>();
            spawnAnchorsInScene.AddRange(anchors);
        }



        // This function will be called, which will look for any available Spawn Anchor and spawn the given prefab at that location
        public void RequestSpawnAtAnyAnchor(GameObject prefabToSpawn)
        {
            // inefficient, but safe
            UpdateSpawnAnchorsInScene();
            
            if (spawnAnchorsInScene.Count == 0)
            {
                Debug.LogWarning("No Spawn Anchors found in the scene! Cannot spawn prefab.");
                return;
            }

            // For simplicity, we'll just use the first available spawn anchor. You could implement more complex logic here if needed.
            SpawnAnchor anchor = spawnAnchorsInScene[0];
            anchor.ManualSpawn(prefabToSpawn);
        }
    }
}
