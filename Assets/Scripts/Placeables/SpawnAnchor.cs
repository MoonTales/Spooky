using System;
using System.Collections.Generic;
using UnityEngine;

namespace Placeables
{
    /// <summary>
    /// Class used to handle custom spawning logic for any objects that need to be spawned as part(s) of the world.
    ///
    /// This will be a placeable object in the world, which will draw a gizmo in the editor showing where objects will be spawned in relation to this anchor.
    /// the general idea is, we will have the location of the anchor, which will draw a beam down to the ground, and then (optionally) have multiple spawn points around it.
    ///
    /// The gizmos drawn will shown the spawn anchor, the beam to the ground, and the spawn points.
    /// </summary>

    [Serializable]
    public struct SpawnData
    {
        public GameObject prefab;
        public Vector3 localPosition;
        public Quaternion localRotation;
        public float size;
        
        public SpawnData(GameObject prefab, Vector3 localPosition, Quaternion localRotation, float size)
        {
            this.prefab = prefab;
            this.localPosition = localPosition;
            this.localRotation = localRotation;
            this.size = size;
        }
    }
    
    public class SpawnAnchor : MonoBehaviour
    {
        [SerializeField] private GameObject prefabToSpawn;
        [SerializeField] private float _spawnAreaSize = 10f;
        [SerializeField] private int _numberOfSpawnPoints = 15;
        [SerializeField] private int _numberOfObjectsToSpawn = 1;
        [SerializeField] private float _maxRaycastDistance = 100f;
        [SerializeField] private float _maxSlopeAngle = 15f; // Maximum angle from horizontal to be considered "flat"
        [SerializeField] private int _maxRetries = 10; // Maximum attempts to find a flat surface per spawn point
        [SerializeField] private float seed = -1; // Option to automatically generate and spawn on Start
        [SerializeField] private List<SpawnData> _spawnDataList = new List<SpawnData>();
        [SerializeField] private bool bDrawGizmos = true;
        // Check if a surface is flat enough based on its normal
        private bool IsSurfaceFlat(Vector3 normal)
        {
            float angle = Vector3.Angle(Vector3.up, normal);
            return angle <= _maxSlopeAngle;
        }
        
        // Generate spawn points based on the anchor position
        private void GenerateSpawnPoints()
        {
            _spawnDataList.Clear();
            
            RaycastHit hit;
            if (!Physics.Raycast(transform.position, Vector3.down, out hit, Mathf.Infinity))
                return;
            
            if (seed >= 0)
            {
                UnityEngine.Random.InitState((int)seed);
            }
            
            for (int i = 0; i < _numberOfSpawnPoints; i++)
            {
                Vector3 spawnPosition = Vector3.zero;
                bool foundValidSpot = false;
                
                // Try to find a flat surface
                for (int retry = 0; retry < _maxRetries; retry++)
                {
                    // Calculate random offset in the spawn area
                    Vector3 randomOffset = new Vector3(
                        UnityEngine.Random.Range(-_spawnAreaSize/2, _spawnAreaSize/2), 
                        0f,
                        UnityEngine.Random.Range(-_spawnAreaSize/2, _spawnAreaSize/2)
                    );
                    Vector3 targetPoint = hit.point + randomOffset;
                    
                    // Raycast from the anchor to the random point to find where it actually hits
                    RaycastHit spawnHit;
                    Vector3 direction = (targetPoint - transform.position).normalized;
                    float distance = Vector3.Distance(transform.position, targetPoint);
                    
                    bool hitSomething = false;
                    
                    if (Physics.Raycast(transform.position, direction, out spawnHit, distance))
                    {
                        // Hit something before reaching the target point
                        hitSomething = true;
                    }
                    else
                    {
                        // Didn't hit anything before target point - continue raycasting downward
                        if (Physics.Raycast(targetPoint, Vector3.down, out spawnHit, _maxRaycastDistance))
                        {
                            hitSomething = true;
                        }
                    }
                    
                    // Check if we hit something and if it's flat enough
                    if (hitSomething && IsSurfaceFlat(spawnHit.normal))
                    {
                        spawnPosition = spawnHit.point;
                        foundValidSpot = true;
                        break; // Found a good spot, exit retry loop
                    }
                }
                
                // If we found a valid spot, add it to the list
                if (foundValidSpot)
                {
                    // Convert to local position relative to the anchor
                    Vector3 localPos = transform.InverseTransformPoint(spawnPosition);
                    
                    // Create spawn data entry
                    SpawnData data = new SpawnData(
                        prefabToSpawn, // Prefab to be assigned later
                        localPos,
                        Quaternion.identity, // Default rotation
                        1f // Default size
                    );
                    
                    _spawnDataList.Add(data);
                }
                else
                {
                    Debug.LogWarning($"Could not find flat surface for spawn point {i} after {_maxRetries} retries");
                }
            }
            
        }
        
        // Call this to spawn all objects
        public void SpawnObjects()
        {
            foreach (var spawnData in _spawnDataList)
            {
                if (spawnData.prefab != null)
                {
                    Vector3 worldPosition = transform.TransformPoint(spawnData.localPosition);
                    Quaternion worldRotation = transform.rotation * spawnData.localRotation;
                    
                    GameObject spawned = Instantiate(spawnData.prefab, worldPosition, worldRotation);
                    spawned.transform.localScale = Vector3.one * spawnData.size;
                }
            }
        }
        
        void Start()
        {
            // Optionally generate and spawn on start
            ClearSpawnPoints();
            GenerateSpawnPoints();
            SpawnObjects();
        }

        void Update()
        {
        
        }
        
        // Gizmo drawing for the spawn anchor
        public void OnDrawGizmos()
        {
            if(!bDrawGizmos){return;}
            // draw a Gizmo at the position of this object
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(transform.position, 0.1f);
            
            // draw a line down to the ground
            RaycastHit hit;
            if (Physics.Raycast(transform.position, Vector3.down, out hit, Mathf.Infinity))
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, hit.point);
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(hit.point, 0.1f);
            }
            
            // draw a square around the hit point to show the spawn area
            if (Physics.Raycast(transform.position, Vector3.down, out hit, Mathf.Infinity))
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(hit.point, new Vector3(_spawnAreaSize, 0.1f, _spawnAreaSize));
            }
            
            // Draw spawn points from the list if they exist
            if (_spawnDataList.Count > 0)
            {
                foreach (var spawnData in _spawnDataList)
                {
                    Vector3 worldPosition = transform.TransformPoint(spawnData.localPosition);
                    
                    Gizmos.color = Color.blue;
                    Gizmos.DrawLine(transform.position, worldPosition);
                    Gizmos.DrawSphere(worldPosition, 0.05f);
                    
                    // Optionally draw a wire sphere to show the size
                    if (spawnData.size > 0)
                    {
                        Gizmos.color = Color.green;
                        Gizmos.DrawWireSphere(worldPosition, spawnData.size * 0.25f);
                    }
                }
            }
        }
        
        // Editor helper - call this from a custom inspector button
        [ContextMenu("Generate Spawn Points")]
        public void GenerateSpawnPointsFromEditor()
        {
            GenerateSpawnPoints();
        }
        
        [ContextMenu("Clear Spawn Points")]
        public void ClearSpawnPoints()
        {
            _spawnDataList.Clear();
        }
    }
}