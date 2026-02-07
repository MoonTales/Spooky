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
        private float _fadeInTime = 1f;
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
            DebugUtils.Log($"[SceneSwapper] Swapping to scene: {newScene.SceneName}");
            _spawnAnchorID = InSpawnAnchorID;
            SceneManager.LoadScene(newScene.SceneName, LoadSceneMode.Single);
        }
        public void SwapScene(string sceneName, string InSpawnAnchorID = "")
        {
            DebugUtils.Log($"[SceneSwapper] Swapping to scene: {sceneName}");
            _spawnAnchorID = InSpawnAnchorID;
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }
        

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            DebugUtils.LogSuccess($"[SceneSwapper] Scene loaded: {scene.name}");
            // after the scene has been loaded, we need to ensure the player is teleported to the correct location
            Player.PlayerManager.Instance.SearchForSpawnAnchor(_spawnAnchorID);
            // This is when we want to broadcast the world clock
            EventBroadcaster.Broadcast_OnWorldClockHourChanged(GameStateManager.Instance.GetCurrentWorldClockHour());
            
            // for now we will hardcode this
            if (scene.name.ToLower() == "bedroom")
            {
                DebugUtils.Log("Broadcasting world location change to Bedroom");
                EventBroadcaster.Broadcast_OnWorldLocationChanged(Types.WorldLocation.Bedroom);
                EventBroadcaster.Broadcast_OnPlayerHealthStateChanged(Types.PlayerMentalState.Normal);

            }
            if (scene.name.ToLower() == "nightmare1")
            {
                DebugUtils.Log("Broadcasting world location change to Nightmare");
                EventBroadcaster.Broadcast_OnWorldLocationChanged(Types.WorldLocation.Nightmare);
                EventBroadcaster.Broadcast_OnPlayerHealthStateChanged(Types.PlayerMentalState.Normal);
            }

            if (scene.name.ToLower() == "tutorialnightmare")
            {
                DebugUtils.LogError("Broadcasting world location change to Tutorial Nightmare");
                EventBroadcaster.Broadcast_OnWorldLocationChanged(Types.WorldLocation.Tutorial);
                EventBroadcaster.Broadcast_OnPlayerHealthStateChanged(Types.PlayerMentalState.Normal);
            }
        }
    }
}