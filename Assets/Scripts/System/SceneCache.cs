//————————————————————————————————————————————————————————————————
// The following code is written and maintained by MoonTales Studio,
// under the creative direction of Cohen Calvert. 
// You are not allowed to use, alter, modify, or re-distribute this
// code without explicit permission from MoonTales Studio.
//————————————————————————————————————————————————————————————————

//—————— Includes ——————//
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
//——————————————————————//

namespace System
{
    /// <summary>
    /// This script is designed to run asynchronously in the background to cache scenes before the player enters them, to speed up load times.
    /// </summary>
    public class SceneCache : Singleton<SceneCache>
    {
        private HashSet<string> _cachedScenes = new HashSet<string>();
        private int _activeCacheCount = 0;
        

        public bool IsCacheInProgress() { return _activeCacheCount > 0; }

        public void RequestSceneCache(string sceneName)
        {
            if (_cachedScenes.Contains(sceneName))
            {
                Debug.Log($"[SceneCache] '{sceneName}' is already cached or in progress, skipping.");
                return;
            }
            DebugUtils.LogSuccess("Requesting cache for scene: " + sceneName);
            
            _cachedScenes.Add(sceneName);
            StartCoroutine(CacheScene(sceneName));
        }

        private IEnumerator CacheScene(string sceneName)
        {
            Debug.Log($"[SceneCache] Starting cache for '{sceneName}'...");
            _activeCacheCount++;
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            op.allowSceneActivation = false;

            while (op.progress < 0.9f)
                yield return null;

            Debug.Log($"[SceneCache] '{sceneName}' loaded into memory, unloading scene objects...");

            op.allowSceneActivation = true;
            yield return op;

            SceneManager.UnloadSceneAsync(sceneName);

            Debug.Log($"[SceneCache] '{sceneName}' cache complete.");
            _activeCacheCount--;
        }
    }
    
}
