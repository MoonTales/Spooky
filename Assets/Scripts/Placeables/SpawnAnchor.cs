using System;
using System.Collections.Generic;
using UnityEngine;

namespace Placeables
{
    /// <summary>
    ///
    /// /// Class Created by: MoonTales Studios 
    /// 
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
        public Vector3 localPosition;
        public Quaternion localRotation;
        public float size;
        
        public SpawnData(Vector3 localPosition, Quaternion localRotation, float size)
        {
            this.localPosition = localPosition;
            this.localRotation = localRotation;
            this.size = size;
        }
    }
    
    public class SpawnAnchor : MonoBehaviour
    {
        [Header("Spawn Data")]
        [SerializeField] private List<GameObject> prefabsToSpawn;
        [SerializeField] private float _spawnAreaSize = 10f;
        [SerializeField] private int _numberOfSpawnPoints = 15;
        [SerializeField] private int _numberOfObjectsToSpawn = 1;
        [SerializeField] private float _maxRaycastDistance = 100f;
        [SerializeField] private float _maxSlopeAngle = 15f; // Maximum angle from horizontal to be considered "flat"
        [SerializeField] private int _maxRetries = 10; // Maximum attempts to find a flat surface per spawn point
        [SerializeField] private float seed = -1; // Option to automatically generate and spawn on Start
        [SerializeField] private List<SpawnData> _spawnDataList = new List<SpawnData>();
        [SerializeField] private bool _randomizeOnSpawn = true; public bool IsRandomizingOnSpawn(){return _randomizeOnSpawn;}
        [SerializeField] private GameObject _parentObjectForSpawns = null; public GameObject GetParentObjectForSpawns(){return _parentObjectForSpawns;}
        private bool _bDrawGizmos = true; public bool IsDrawingGizmos(){return _bDrawGizmos;}

        
        
        // Internal Information used to store previous gameobjects we have spawned, so we can clean them up if needed
        // this will be a list of lists, where each inner list corresponds to ALL of the objects spawned from a single Spawn() call
        private List<List<GameObject>> _spawnedObjects = new List<List<GameObject>>(); public int GetNumberOfSpawnedLists(){return _spawnedObjects.Count;}
        private List<List<SpawnData>> _undoneObjects = new List<List<SpawnData>>(); public int GetNumberOfUndoneLists(){return _undoneObjects.Count;}
        
        // Generate spawn points based on the anchor position
        private void GenerateSpawnPoints(bool bSpawnInEditor = false)
        {
            _spawnDataList.Clear();
            
            RaycastHit hit;
            if (!Physics.Raycast(transform.position, Vector3.down, out hit, Mathf.Infinity)) {return;}
            
            if (seed >= 0) { UnityEngine.Random.InitState((int)seed); }
            
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
                        localPos,
                        Quaternion.identity,
                        1f
                    );
                    
                    _spawnDataList.Add(data);
                }
                else
                {
                    Debug.LogWarning($"Could not find flat surface for spawn point {i} after {_maxRetries} retries");
                }
            }
            
            if (bSpawnInEditor){ SpawnObjects();}
            
        }
        
        // Call this to spawn all objects
        public void SpawnObjects()
        {
            if (prefabsToSpawn.Count == 0){return;}
            if(_randomizeOnSpawn){ GenerateSpawnPoints(); }
            List<GameObject> currentSpawnBatch = new List<GameObject>();
            // Randomly select [_numberOfObjectsToSpawn] spawn points from the list and spawn the prefab there
            // Once something has been spawned, we can remove it from the list to avoid spawning multiple objects in the same spot
            List<SpawnData> availableSpawnPoints = new List<SpawnData>(_spawnDataList);
            for (int i = 0; i < _numberOfObjectsToSpawn; i++)
            {
                // if we run out of spawn points, break out of the loop
                if (availableSpawnPoints.Count <= 0) { break; }
                
                // get a random spawn point from the list
                int randomIndex = UnityEngine.Random.Range(0, availableSpawnPoints.Count);
                
                // read the spawnData
                SpawnData spawnData = availableSpawnPoints[randomIndex];
                
                // Convert local position back to world position
                Vector3 worldPosition = transform.TransformPoint(spawnData.localPosition);
                
                // Instantiate the prefab at the world position
                // randomly select a prefab from the list to spawn
                GameObject selectedPrefab = prefabsToSpawn[UnityEngine.Random.Range(0, prefabsToSpawn.Count)];
                GameObject obj = Instantiate(selectedPrefab, worldPosition, spawnData.localRotation);
                if (_parentObjectForSpawns)
                {
                    obj.transform.parent = _parentObjectForSpawns.transform;
                }
                currentSpawnBatch.Add(obj);
                
                
                // Remove this spawn point from the available list
                availableSpawnPoints.RemoveAt(randomIndex);
            }
            _spawnedObjects.Add(currentSpawnBatch);
            
        }

        public void ManualSpawn(GameObject prefabToSpawn)
        {
            GenerateSpawnPoints();
            if (_spawnDataList.Count == 0){return;}
            // pick a random spawn point from the list
            int randomIndex = UnityEngine.Random.Range(0, _spawnDataList.Count);
            SpawnData spawnData = _spawnDataList[randomIndex];
            // Convert local position back to world position
            Vector3 worldPosition = transform.TransformPoint(spawnData.localPosition);
            // Instantiate the prefab at the world position
            GameObject obj = Instantiate(prefabToSpawn, worldPosition, spawnData.localRotation);
        }
        
        
        private void Start()
        {
            // Optionally generate and spawn on start
            //ClearSpawnPoints();
            GenerateSpawnPoints();
            SpawnObjects();
        }


        
        // Gizmo drawing for the spawn anchor
        public void OnDrawGizmos()
        {
            if(!_bDrawGizmos){return;}
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
                    
                    
                    if (spawnData.size > 0)
                    {
                        Gizmos.color = Color.green;
                        Gizmos.DrawWireSphere(worldPosition, spawnData.size * 0.25f);
                    }
                }
            }
        }

        #region Editor Functions
        [ContextMenu("Visualize Spawn Points")]
        public void Editor_VisualizeSpawnPoints()
        {
            GenerateSpawnPoints(bSpawnInEditor: false);
        }
        [ContextMenu("Spawn Objects")]
        public void Editor_SpawnObjects()
        {
            SpawnObjects();
        }
        
        [ContextMenu("Clear Spawn Points")]
        public void Editor_ClearSpawnPoints()
        {
            _spawnDataList.Clear();
        }
        
        [ContextMenu("Undo Last Spawn")]
        public void Editor_UndoLastSpawn()
        {
            if (_spawnedObjects.Count > 0)
            {
                List<GameObject> lastBatch = _spawnedObjects[_spawnedObjects.Count - 1];
                List<SpawnData> spawnDataBatch = new List<SpawnData>();

                foreach (var obj in lastBatch)
                {
                    if (obj) 
                    { 
                        // Convert world position to local position relative to anchor
                        Vector3 localPos = transform.InverseTransformPoint(obj.transform.position);
                        Quaternion localRot = Quaternion.Inverse(transform.rotation) * obj.transform.rotation;
                
                        spawnDataBatch.Add(new SpawnData(
                            localPos, 
                            localRot, 
                            obj.transform.localScale.x
                        ));
                        DestroyImmediate(obj); 
                    }
                }
                _spawnedObjects.RemoveAt(_spawnedObjects.Count - 1);
                _undoneObjects.Add(spawnDataBatch);
            }
        }

        [ContextMenu("Redo Last Undo")]
        public void Editor_RedoLastUndo()
        {
            if (_undoneObjects.Count > 0)
            {
                List<SpawnData> lastUndoneBatch = _undoneObjects[_undoneObjects.Count - 1];
                List<GameObject> reSpawnedBatch = new List<GameObject>();

                foreach (var data in lastUndoneBatch)
                {
                    // Convert local position back to world position
                    Vector3 worldPos = transform.TransformPoint(data.localPosition);
                    Quaternion worldRot = transform.rotation * data.localRotation;
            
                    GameObject selectedPrefab = prefabsToSpawn[UnityEngine.Random.Range(0, prefabsToSpawn.Count)];
                    GameObject reSpawnedObj = Instantiate(selectedPrefab, worldPos, worldRot);
                    reSpawnedObj.transform.localScale = Vector3.one * data.size;
    
                    if (_parentObjectForSpawns)
                    {
                        reSpawnedObj.transform.parent = _parentObjectForSpawns.transform;
                    }
                    reSpawnedBatch.Add(reSpawnedObj);
                }

                _spawnedObjects.Add(reSpawnedBatch);
                _undoneObjects.RemoveAt(_undoneObjects.Count - 1);
            }
        }
        
        [ContextMenu("Toggle Gizmos")]
        public void Editor_ToggleGizmos()
        {
            _bDrawGizmos = !_bDrawGizmos;
        }
        
        #endregion
        
        # region Helper Functions
        // Check if a surface is flat enough based on its normal
        private bool IsSurfaceFlat(Vector3 normal)
        {
            float angle = Vector3.Angle(Vector3.up, normal);
            return angle <= _maxSlopeAngle;
        }
        #endregion
    }
}