using System;
using System.Collections.Generic;
using Placeables;
using Player;
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
            
            if (GameStateManager.Instance.GetCurrentWorldLocation() == Types.WorldLocation.Nightmare)
            {
                // if we are entering the nightmare, we want to populate the nightmare with drawings
                PopulateNightmare();
            }
        }

        private void UpdateSpawnAnchorsInScene()
        {
            spawnAnchorsInScene.Clear();
            // find all spawn anchors in the scene and add them to our list
            SpawnAnchor[] anchors = FindObjectsByType<SpawnAnchor>(FindObjectsSortMode.None);
            spawnAnchorsInScene.AddRange(anchors);
        }


        private void HandleDrawingsInZones()
        {
            // Get access to ALL of the spawn anchors in the scene, with a machine Zone ID
            List<SpawnAnchor> zoneAnchors = spawnAnchorsInScene.FindAll(anchor => anchor.GetZoneID() == GameStateManager.Instance.GetCurrentZoneId());
            
            // Get access to the list of dropped drawings, and attempt to spawn them at these zone anchors, as these are the only ones that will spawn in the correct location
            HashSet<int> droppedDrawings = PlayerInventory.Instance.GetDroppedDrawingIDs();
            
            // we can loop through all of the dropped drawings, and attempt to spawn them at the correct location
            // within the zone, we will look for spawn anchors with the correct anchor.GetPriorityDrawingNumber() (which should match the drawing id)
            foreach (int drawingID in droppedDrawings)
            {
                List<SpawnAnchor> matchingPriorityAnchors = zoneAnchors.FindAll(anchor => anchor.GetPriorityDrawingNumber() == drawingID);
                if (matchingPriorityAnchors.Count == 0)
                {
                    // Every zone should have atleast 1 anchor with a -1 prority, with means if nothing else, look for a 01 priority anchor
                    matchingPriorityAnchors = zoneAnchors.FindAll(anchor => anchor.GetPriorityDrawingNumber() == -1);
                    if (matchingPriorityAnchors.Count == 0)
                    {
                        // we have no anchors with this priority number, so we will just skip spawning this drawing, as there is no available anchor for it
                        Debug.LogWarning($"No available Spawn Anchors with priority {drawingID} found for zone {GameStateManager.Instance.GetCurrentZoneId()}! Skipping spawn for drawing with ID {drawingID}.");
                        continue;
                    }
                }
                // pick a random matching anchor
                SpawnAnchor selectedAnchor = matchingPriorityAnchors[UnityEngine.Random.Range(0, matchingPriorityAnchors.Count)];
                
                // we need to load in the drawing prefab from resources
                // our naming convention is "N_Drawing_i"
                string prefabName = $"Prefabs/Drawings/Nightmare/N_Drawing_{drawingID}";
                GameObject prefabToSpawn = Resources.Load<GameObject>(prefabName);
                if (prefabToSpawn == null)
                {
                    Debug.LogError($"Failed to load prefab with name {prefabName} from Resources! Make sure the prefab exists and is located in a Resources folder.");
                    continue;
                }
                selectedAnchor.ManualSpawn(prefabToSpawn);
            }
            
            // now we have spawned in all of the weird edge case drawings that the player dropped on their last life
            // the rest can be spawned normally, we just need to ensure we dont spawn duplicates
            
            
        }
        /// <summary>
        /// This is called when we enter the nightmare scene, to populate all of the drawings in the nightmare
        /// </summary>
        private void PopulateNightmare()
        {
            
            // Refresh the list of spawn anchors in the scene
            UpdateSpawnAnchorsInScene();

            /* There is a priority to how these will work:
             1st) We will attempt to find a matching zone, in the case our player previously died within a zone. 
                  if this matches the currentAct, we can spawn normally
                  
                  The idea is that if we are currently in act 2, and we reach zone 1 and die, the drawings should try to spawn in zone 1, rather than Act 2
             */
            int lastSeenZone = GameStateManager.Instance.GetCurrentZoneId(); // as this was the last zone the player was in, before death
            DebugUtils.Log($"Last seen zone before death: {lastSeenZone}");
            // if this is ever -1, that means we had a "good" wakeup and didnt drop anything, and we can just spawn normally based on the act
            if (lastSeenZone != -1 && lastSeenZone != 0) // as zero is the default value at startup
            {
                HandleDrawingsInZones();
                // after this call we have handled the edge cases
            }
            
            // if its not negative 1, treat like normal
            
            
            
            //Step 3. Attempt to find available Spawn Anchors, that match the worlds anchorIdentifier
            int currentAct = GameStateManager.Instance.GetCurrentWorldClockHour();
            // Hour 1: Act1
            AnchorIdentifier anchorIdentifierToUse = AnchorIdentifier.Act1;
            if (currentAct == 1){anchorIdentifierToUse = AnchorIdentifier.Act1;}
            else if (currentAct == 2){anchorIdentifierToUse = AnchorIdentifier.Act2;}
            else if (currentAct >= 3){anchorIdentifierToUse = AnchorIdentifier.Act3;}

            //Step 4. Attempt to find available Spawn Anchors, that match has the correct priorityID
            List<SpawnAnchor> matchingAnchors = spawnAnchorsInScene.FindAll(anchor => anchor.GetAnchorIdentifier() == anchorIdentifierToUse);
            // if there is none, try with the -1 identifier, which means it can spawn regardless of the act. keep trying untill we reach -1
            for (int i = (int)anchorIdentifierToUse; i >= -1; i--)
            {
                matchingAnchors = spawnAnchorsInScene.FindAll(anchor => anchor.GetAnchorIdentifier() == (AnchorIdentifier)i);
                if (matchingAnchors.Count > 0)
                {
                    // we found some matching anchors, so we can break out of the loop and use these anchors to spawn our drawings
                    break;
                }
            }
            if (matchingAnchors.Count == 0)
            {
                // we found no anchors at all, so we cannot spawn any drawings
                Debug.LogError($"No Spawn Anchors with identifier {anchorIdentifierToUse} or lower found in the scene! Cannot spawn any drawings for this act.");
                return;
            }

            //Step 5. Spawn the drawings at a spawn anchor that has NOT been used yet
            int numberOfDrawingsInGame = GameStateManager.Instance.GetMaxDrawingsInGame();
            
            // we are gonna attempt to spawn the max number of drawings, as once we spawn on thats already collected, it auto handles turning itself off
            for (int i = 1; i < numberOfDrawingsInGame + 1; i++)
            {
                // if i equals one of the drawing IDs that have been already spawned in (like, from the dropped ones, skip it)
                HashSet<int> droppedDrawings = PlayerInventory.Instance.GetDroppedDrawingIDs();
                if (droppedDrawings.Contains(i))
                {
                    // we have already spawned this drawing in the "dropped drawings" step, so we can skip it here
                    continue;
                }
                
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
            
            // finally we can clear the dropped drawings list
            PlayerInventory.Instance.ClearDroppedDrawings();
            
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
