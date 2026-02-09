using System;
using System.Collections;
using UnityEngine;
using Types = System.Types;

namespace Managers
{
    /// <summary>
    /// The goal of this class, will handle the notes that spawn into the game at the start of each "Act"
    /// </summary>
    public class NoteManager : Singleton<NoteManager>
    {
        
        // Internal variables
        private GameObject _notePrefab;
        private GameObject _currentNote;
        
        [Header("Note Slide Settings")]
        [SerializeField] private float slideDistance = 1.5f; // Base distance the note slides
        [SerializeField] private float slideDistanceVariation = 0.5f; // Random variation in slide distance (+/-)
        [SerializeField] private float slideDuration = 0.5f; // How long the slide takes
        [SerializeField] private AnimationCurve slideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // Smooth movement
        
        [Header("Random Offset Settings")]
        [SerializeField] private float horizontalOffsetRange = 1f; // Random left/right offset range
        
        [Header("Rotation Settings")]
        [SerializeField] private float maxRotationAngle = 15f; // Max degrees to rotate left or right (Y-axis)


        protected override void Awake()
        {
            base.Awake();
            _notePrefab = Resources.Load<GameObject>("Prefabs/Letter");

        }
        
        protected override void RegisterSubscriptions()
        {
            base.RegisterSubscriptions();
            TrackSubscription(() => EventBroadcaster.OnWorldClockHourChanged += OnWorldClockUpdated,
                () => EventBroadcaster.OnWorldClockHourChanged -= OnWorldClockUpdated);
        }

        private void OnWorldClockUpdated(int clockHour)
        {
            // this will be called each time the world clock updates
            SpawnNoteForCurrentAct();
        }



        private void SpawnNoteForCurrentAct()
        {
            // we only bother with this in the bedroom
            if (GameStateManager.Instance.GetCurrentWorldLocation() != Types.WorldLocation.Bedroom) { return; }
            
            int currentAct = GameStateManager.Instance.GetCurrentWorldClockHour();
            // look through each act
            if (currentAct == 1)
            {
                // look for the object in the scene called "NoteSpawnLocation"
                GameObject spawnLocation = GameObject.Find("NoteSpawnLocation");
                // spawn the note prefab at the location of the "NoteSpawnLocation" object
                if (!spawnLocation) { return;}
                
                _currentNote = Instantiate(_notePrefab, spawnLocation.transform.position, Quaternion.identity);
                
                // Start the sliding coroutine
                StartCoroutine(SlideNote(_currentNote));
            }
            
        }
        
        private IEnumerator SlideNote(GameObject note)
        {
            if (note == null){ yield break;}
            
            // Start at the spawn location (center of door)
            Vector3 startPosition = note.transform.position;
            
            // Calculate random offset destination with varied distance
            float randomXOffset = UnityEngine.Random.Range(-horizontalOffsetRange, horizontalOffsetRange);
            float randomDistance = slideDistance + UnityEngine.Random.Range(-slideDistanceVariation, slideDistanceVariation);
            Vector3 endPosition = startPosition + new Vector3(randomXOffset, 0, randomDistance);
            
            // Start with no rotation
            Quaternion startRotation = Quaternion.identity;
            
            // Random target rotation angle (left or right) - rotates around Y-axis
            float randomYRotation = UnityEngine.Random.Range(-maxRotationAngle, maxRotationAngle);
            Quaternion targetRotation = Quaternion.Euler(0, randomYRotation, 0);
            
            float elapsedTime = 0f;
            
            while (elapsedTime < slideDuration)
            {
                elapsedTime += Time.deltaTime;
                float normalizedTime = elapsedTime / slideDuration;
                
                // Use the animation curve for smooth movement
                float curveValue = slideCurve.Evaluate(normalizedTime);
                
                // Interpolate both position and rotation
                note.transform.position = Vector3.Lerp(startPosition, endPosition, curveValue);
                note.transform.rotation = Quaternion.Lerp(startRotation, targetRotation, curveValue);
                
                yield return null;
            }
            
            // Ensure we end at exactly the target position and rotation
            note.transform.position = endPosition;
            note.transform.rotation = targetRotation;
        }
        
        private void ForceSpawnNote()
        {
            // Force spawn for testing purposes
            GameObject spawnLocation = GameObject.Find("NoteSpawnLocation");
            if (!spawnLocation) { return;}
            
            _currentNote = Instantiate(_notePrefab, spawnLocation.transform.position, Quaternion.identity);
            StartCoroutine(SlideNote(_currentNote));
        }
        
        
    }
}