using UnityEngine;

namespace Player
{
    /// <summary>
    /// Class used to mark a spawn point for the player, due to the Player being stored as a prefab in
    /// the resources folder.
    ///
    /// By default, the player will look for a respawn actor with the ID: "DEFAULT_SPAWN_POINT" when spawning.
    ///
    /// if we fail to find this actor, then we will spawn at the first PlayerSpawnAnchor found in the scene.
    ///
    /// if we fail to find any PlayerSpawnAnchor in the scene, then we will spawn at the world origin (0,0,0).
    /// </summary>
    public class PlayerSpawnAnchor : MonoBehaviour
    {
        
        [SerializeField] private string spawnPointID = "DEFAULT_SPAWN_POINT"; public string GetSpawnPointID() { return spawnPointID; }
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
        
        }

        // Update is called once per frame
        void Update()
        {
        
        }
    }
}
