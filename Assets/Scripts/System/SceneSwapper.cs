using System;
using System.Collections;
using Managers;
using Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace System
{
    

    
    public class SceneSwapper : Singleton<SceneSwapper>
    {
        // Internal variables
        private string _spawnAnchorID = "";

        protected override void OnEnable()
        {
            base.OnEnable();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        public void SwapScene(SceneField newScene, string InSpawnAnchorID = "")
        {
            _spawnAnchorID = InSpawnAnchorID;
            StartCoroutine(LoadSceneAsync(newScene.SceneName));
        }

        public void SwapScene(string sceneName, string InSpawnAnchorID = "")
        {
            _spawnAnchorID = InSpawnAnchorID;
            StartCoroutine(LoadSceneAsync(sceneName));
        }
        

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // after the scene has been loaded, we need to ensure the player is teleported to the correct location
            Player.PlayerManager.Instance.SearchForSpawnAnchor(_spawnAnchorID);
            // This is when we want to broadcast the world clock
            EventBroadcaster.Broadcast_OnWorldClockHourChanged(GameStateManager.Instance.GetCurrentWorldClockHour());
            
            // for now we will hardcode this
            if (scene.name.ToLower() == "bedroom")
            {
                EventBroadcaster.Broadcast_OnWorldLocationChanged(Types.WorldLocation.Bedroom);
                EventBroadcaster.Broadcast_OnPlayerHealthStateChanged(Types.PlayerMentalState.Normal);

            }
            if (scene.name.ToLower() == "nightmare1")
            {
                EventBroadcaster.Broadcast_OnWorldLocationChanged(Types.WorldLocation.Nightmare);
                EventBroadcaster.Broadcast_OnPlayerHealthStateChanged(Types.PlayerMentalState.Normal);
            }

            if (scene.name.ToLower() == "tutorial")
            {
                EventBroadcaster.Broadcast_OnWorldLocationChanged(Types.WorldLocation.Tutorial);
                EventBroadcaster.Broadcast_OnPlayerHealthStateChanged(Types.PlayerMentalState.Normal);
            }
        }
        
        // Async for a smoother scene transition
        private IEnumerator LoadSceneAsync(string sceneName)
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);

            // Prevent the scene from activating the moment it finishes loading
            asyncLoad.allowSceneActivation = false;

            // Wait until the scene is fully loaded (progress reaches 0.9 — Unity's threshold before activation)
            while (asyncLoad.progress < 0.9f)
            {
                yield return null;
            }

            // Scene is ready — activate it now for a clean, snap-free transition
            asyncLoad.allowSceneActivation = true;

            // Wait one frame for OnSceneLoaded to fire before continuing
            
            //SAVE UPDATE
            // we want to save the game whenever we successfully scene swap
            SaveSystem.Instance.SaveGame();
            yield return null;
        }
    }
}