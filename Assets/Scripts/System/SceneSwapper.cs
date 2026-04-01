using System;
using System.Collections;
using Managers;
using Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace System
{
    

    
    public class SceneSwapper : Singleton<SceneSwapper>, ISaveSystemInterface<SceneSwapper.SceneSwapSaveData>
    {
        private string _oldSceneName = "";
        
        public struct SceneSwapSaveData
        {
            // get the name of our current scene
            public string CurrentSceneName;
        }
        
        // Internal variables
        private string _spawnAnchorID = "";
        private bool _sceneInitialized = false;
        public void NotifySceneInitialized() => _sceneInitialized = true;

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
            _oldSceneName = SceneManager.GetActiveScene().name;
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
            float saveDelay = 2.5f;
            // for now we will hardcode this
            if (scene.name.ToLower() == "bedroom")
            {
                EventBroadcaster.Broadcast_OnWorldLocationChanged(Types.WorldLocation.Bedroom);
                EventBroadcaster.Broadcast_OnPlayerHealthStateChanged(Types.PlayerMentalState.Normal);
                // only save if we did not come from the mainmenu
                if (_oldSceneName.ToLower() != "mainmenu"){Invoke(nameof(DelayedSave), saveDelay);}
                
            }
            if (scene.name.ToLower() == "nightmare1")
            {
                EventBroadcaster.Broadcast_OnWorldLocationChanged(Types.WorldLocation.Nightmare);
                EventBroadcaster.Broadcast_OnPlayerHealthStateChanged(Types.PlayerMentalState.Normal);
                if (_oldSceneName.ToLower() != "mainmenu"){Invoke(nameof(DelayedSave), saveDelay);}
            }

            if (scene.name.ToLower() == "finalenightmare")
            {
                EventBroadcaster.Broadcast_OnWorldLocationChanged(Types.WorldLocation.Nightmare);
                EventBroadcaster.Broadcast_OnPlayerHealthStateChanged(Types.PlayerMentalState.ModeratelyAnxious);
                if (_oldSceneName.ToLower() != "mainmenu"){Invoke(nameof(DelayedSave), saveDelay);}
            }

            if (scene.name.ToLower() == "tutorial")
            {
                EventBroadcaster.Broadcast_OnWorldLocationChanged(Types.WorldLocation.Tutorial);
                EventBroadcaster.Broadcast_OnPlayerHealthStateChanged(Types.PlayerMentalState.Normal);
            }
            

            NotifySceneInitialized();
        }

        private void DelayedSave()
        {
            // we want to delay to ensure that stuff is fully loaded and saved
            SaveSystem.Instance.SaveGame();
        }
        // Async for a smoother scene transition
        public IEnumerator LoadSceneAsync(string sceneName)
        {
            _sceneInitialized = false;
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
            
            yield return new WaitUntil(() => _sceneInitialized);
            
            yield return new WaitForEndOfFrame();
        }

        public string SaveId => "SceneSwapper";
        public SceneSwapSaveData OnSave()
        {
            // we want to save the name of our current scene, so that way we can return to it if we need to
            SceneSwapSaveData saveData = new SceneSwapSaveData
            {
                CurrentSceneName = SceneManager.GetActiveScene().name
            };
            return saveData;
        }

        public void OnLoad(SceneSwapSaveData data)
        {
            // when we load, we want to immediately swap to the scene that we were in when we saved
            // we no longer need to worry about this
            //SwapScene(data.CurrentSceneName);
        }
    }
}