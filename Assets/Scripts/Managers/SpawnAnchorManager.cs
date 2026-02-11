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
            if (spawnAnchorsInScene.Count == 0)
            {
                Debug.LogWarning("No Spawn Anchors found in the scene! Cannot spawn prefab.");
                return;
            }

            // pick a random anchor from the list of spawn anchors in the scene, and spawn the prefab at that location
            
            SpawnAnchor anchor = spawnAnchorsInScene[UnityEngine.Random.Range(0, spawnAnchorsInScene.Count)];
            anchor.ManualSpawn(prefabToSpawn);
        }

        public void RequestSpawnAtAnchorByIdentifier(GameObject prefabToSpawn, AnchorIdentifier identifier)
        {

            // look through all of spawnAnchorsInScene, and pick a random one that matches the given identifier, then spawn the prefab at that location
            List<SpawnAnchor> matchingAnchors = spawnAnchorsInScene.FindAll(anchor => anchor.GetAnchorIdentifier() == identifier);
            if (matchingAnchors.Count == 0)
            {
                Debug.LogWarning($"No Spawn Anchors with identifier {identifier} found in the scene! Cannot spawn prefab.");
                return;
            }
            // pick a random matching anchor
            SpawnAnchor selectedAnchor = matchingAnchors[UnityEngine.Random.Range(0, matchingAnchors.Count)];
            selectedAnchor.ManualSpawn(prefabToSpawn);
        }
    }
}
