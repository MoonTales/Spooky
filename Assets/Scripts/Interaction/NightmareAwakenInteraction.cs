using System;
using System.Collections.Generic;
using Interaction.drawings;
using Managers;
using Player;
using UnityEngine;
using Types = System.Types;

namespace Interaction
{
    /// <summary>
    /// Class for handling waking up from the nightmare with a "good wakeup"
    ///
    /// Ontop of this table (which is what the interaction is connected too), we want to have 3 "drawings" to illustrate what the player is looking for within the nightmare
    /// </summary>
    public class NightmareAwakenInteraction : MonoBehaviour, IInteractable
    {
        [SerializeField] private SceneField sceneName;
    
        [Header("Text Keys (CSV row pointers)")]
        [SerializeField] private TextKey promptTextKey;
        public TextKey PromptKey => promptTextKey;
        
        [SerializeField] private GameObject drawingSpawnLocationOne; // location above the table where the first drawing will spawn
        [SerializeField] private GameObject drawingSpawnLocationTwo; // location above the table where the second drawing will spawn
        [SerializeField] private GameObject drawingSpawnLocationThree; // location above the table where the third drawing will spawn


        private void Start()
        {

            // we will spawn in the "placeholder_outline" drawing to all of these locations, to show we are looking for 3 things
            GameObject placeholderPrefab = Resources.Load<GameObject>("Prefabs/Drawings/Empty/E_Drawing");
            var obj = Instantiate(placeholderPrefab, drawingSpawnLocationOne.transform);
            // we should zero our the local position of these drawings, since the prefab might have some weird offset, and we want them to be centered on the location game objects
            obj.transform.localPosition = Vector3.zero;
            obj =Instantiate(placeholderPrefab, drawingSpawnLocationTwo.transform);
            obj.transform.localPosition = Vector3.zero;
            obj = Instantiate(placeholderPrefab, drawingSpawnLocationThree.transform);
            obj.transform.localPosition = Vector3.zero;
        }
        
        public bool CanInteract(Interactor interactor)
        {
            return true;
        }

        public void Interact(Interactor interactor)
        {
            
            // check if we have atleast 1 drawing in the inventory
            if (PlayerInventory.Instance.GetCurrentDrawingsThisNight() > 0)
            {
                HandleGoodWakeup();
                return;
            }
            
            // otherwise, we cant return yet
            Types.NotificationData data = new(
                duration: 1, 
                messageKey: new TextKey(),
                messageOverride: "You feel like you haven't done enough tonight. Maybe you should explore a bit more?"
            );
            data.Send();
            
        }

        private void HandleGoodWakeup()
        {
            // Disable the collider so that we cant interact with this again while the fadeout is happening
            GetComponent<Collider>().enabled = false;
            SpawnInCollectedDrawingsOnTable();
            const int timeToFadeOut = 5; 
            //TODO: <SFX> Start an alarm sound fading in over "timToFadeOut"
            SleepTrackerManager.Instance.StartSleepTrackerFadeIn(timeToFadeOut);
            Types.ScreenFadeData data = new Types.ScreenFadeData(fadeInDuration:1, 2, fadeOutDuration:timeToFadeOut, null, FadeOutCompleted);
            data.Send();

        }

        private void SpawnInCollectedDrawingsOnTable()
        {
            // this is purley aesthetic, and it will spawn in the drawins we have collected (1 to 3 drawings), at one of the 3 drawing locations on the table, so that we can see them in the nightmare as we wake up

            bool hasSpawnedDrawingOne = false;
            bool hasSpawnedDrawingTwo = false;
            bool hasSpawnedDrawingThree = false;
            HashSet<int> collectedDrawingIds = PlayerInventory.Instance.GetCollectedDrawingIDs();
            foreach (int drawingID in collectedDrawingIds)
            {
                string prefabName = $"Prefabs/Drawings/Nightmare/N_Drawing_{drawingID}";
                GameObject prefabToSpawn = Resources.Load<GameObject>(prefabName);
                if (prefabToSpawn == null) { Debug.LogError($"Unable to find prefab for drawing ID {drawingID} at path {prefabName}"); continue; }
                
                // we will just always start with location 1, then 2, then 3. for simplicity
                // we will need to take the current child of the spawn point (which is our empty place holder), destroy it, and then spawn in the new drawing prefab
                if (!hasSpawnedDrawingOne)
                {
                    // get the first child of the first location, and destroy it
                    Transform firstChild = drawingSpawnLocationOne.transform.GetChild(0);
                    Destroy(firstChild.gameObject);
                    // spawn in the new drawing prefab as a child of the first location, and zero out its local position
                    var obj = Instantiate(prefabToSpawn, drawingSpawnLocationOne.transform);
                    obj.transform.localPosition = Vector3.zero;
                    hasSpawnedDrawingOne = true;
                    DisableDrawing(obj);
                    // unique handling, where we need to disable the collider and the Drawing component
                    
                } else if (!hasSpawnedDrawingTwo)
                {
                    // get the first child of the second location, and destroy it
                    Transform firstChild = drawingSpawnLocationTwo.transform.GetChild(0);
                    Destroy(firstChild.gameObject);
                    // spawn in the new drawing prefab as a child of the second location, and zero out its local position
                    var obj = Instantiate(prefabToSpawn, drawingSpawnLocationTwo.transform);
                    obj.transform.localPosition = Vector3.zero;
                    hasSpawnedDrawingTwo = true;
                    DisableDrawing(obj);
                } else if (!hasSpawnedDrawingThree)
                {
                    // get the first child of the third location, and destroy it
                    Transform firstChild = drawingSpawnLocationThree.transform.GetChild(0);
                    Destroy(firstChild.gameObject);
                    // spawn in the new drawing prefab as a child of the third location, and zero out its local position
                    var obj = Instantiate(prefabToSpawn, drawingSpawnLocationThree.transform);
                    obj.transform.localPosition = Vector3.zero;
                    DisableDrawing(obj);
                    hasSpawnedDrawingThree = true;
                }
            }
        }

        private void DisableDrawing(GameObject drawing)
        {
            drawing.GetComponent<Collider>().enabled = false;
            drawing.GetComponent<Drawing>().enabled = false;
        }

        private void FadeOutCompleted()
        {
            SleepTrackerManager.Instance.SetIsGoodWakeup(true);
            SceneSwapper.Instance.SwapScene(sceneName);
            GameStateManager.Instance.SetCurrentZoneId(-1);
        }
    
    }
}
