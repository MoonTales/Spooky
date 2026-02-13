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


        private void Update()
        {
            // for testing purposes, we will call PopulateNightmare every frame, but in the actual game, this should only be called once when we enter the nightmare scene
            if (Input.GetKeyDown(KeyCode.P))
            {
                PopulateNightmare();
            }
        }

        /// <summary>
        /// This is called when we enter the nightmare scene, to populate all of the drawings in the nightmare
        /// </summary>
        private void PopulateNightmare()
        {
            
            
            // Refresh the list of spawn anchors in the scene
            UpdateSpawnAnchorsInScene();

            //Step 3. Attempt to find available Spawn Anchors, that match the worlds anchorIdentifier
            int currentAct = GameStateManager.Instance.GetCurrentWorldClockHour();
            // Hour 1: Act1
            AnchorIdentifier anchorIdentifierToUse = AnchorIdentifier.Act1;
            if (currentAct == 1){anchorIdentifierToUse = AnchorIdentifier.Act1;}
            else if (currentAct == 2){anchorIdentifierToUse = AnchorIdentifier.Act2;}
            else if (currentAct >= 3){anchorIdentifierToUse = AnchorIdentifier.Act3;}

            //Step 4. Attempt to find available Spawn Anchors, that match has the correct priorityID
            List<SpawnAnchor> matchingAnchors = spawnAnchorsInScene.FindAll(anchor => anchor.GetAnchorIdentifier() == anchorIdentifierToUse);

            //Step 5. Spawn the drawings at a spawn anchor that has NOT been used yet
            int numberOfDrawingsInGame = GameStateManager.Instance.GetMaxDrawingsInGame();
            
            // we are gonna attempt to spawn the max number of drawings, as once we spawn on thats already collected, it auto handles turning itself off
            for (int i = 1; i < numberOfDrawingsInGame + 1; i++)
            {
                // Attempt to find a spawnAnchor with a matching GetPriorityDrawingNumber()
                List<SpawnAnchor> matchingPriorityAnchors = matchingAnchors.FindAll(anchor => anchor.GetPriorityDrawingNumber() == i);
                if (matchingPriorityAnchors.Count == 0)
                {
                    // we have no anchors with this priority number, so we will check for a -1 priority number, which means it can spawn regardless of the drawing number
                    matchingPriorityAnchors = matchingAnchors.FindAll(anchor => anchor.GetPriorityDrawingNumber() == -1);
                    if (matchingPriorityAnchors.Count == 0)
                    {
                        // we have no anchors with this priority number, so we will just skip spawning this drawing, as there is no available anchor for it
                        Debug.LogWarning($"No available Spawn Anchors with priority {i} or -1 found for anchor identifier {anchorIdentifierToUse}! Skipping spawn for drawing with priority {i}.");
                        continue;
                    }
                }
                
                // By this point, we have a list of matching anchors that match we can spawn at, so we randomly pick one from the list
                SpawnAnchor selectedAnchor = matchingPriorityAnchors[UnityEngine.Random.Range(0, matchingPriorityAnchors.Count)];
                
                // WE HAVE FINALLY REACHED OUR ANCHOR!!
                // we need to load in the drawing prefab from resources
                // our naming convention is "N_Drawing_i"
                string prefabName = $"Prefabs/Drawings/Nightmare/N_Drawing_{i}";
                GameObject prefabToSpawn = Resources.Load<GameObject>(prefabName);
                if (prefabToSpawn == null)
                {
                    Debug.LogError($"Failed to load prefab with name {prefabName} from Resources! Make sure the prefab exists and is located in a Resources folder.");
                    continue;
                }
                selectedAnchor.ManualSpawn(prefabToSpawn);
            }
            
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
