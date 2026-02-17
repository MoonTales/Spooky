using System;
using System.Collections;
using Interaction.Letters;
using Placeables;
using UnityEngine;
using Types = System.Types;

namespace Managers
{
    /// <summary>
    /// The goal of this class, will handle the notes that spawn into the game at the start of each "Act"
    /// </summary>
    public class LetterManager : Singleton<LetterManager>
    {
        
        // Internal variables
        private GameObject _notePrefab;
        private GameObject _currentNoteResearcher;
        private GameObject _currentNoteFriend;
        
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
            TrackSubscription(()=> EventBroadcaster.OnWorldLocationChangedEvent += OnWorldLocationChanged,
                () => EventBroadcaster.OnWorldLocationChangedEvent -= OnWorldLocationChanged);
        }

        private void OnWorldLocationChanged(Types.WorldLocation worldLocation)
        {
            if (worldLocation == Types.WorldLocation.Bedroom)
            {
                SpawnNoteForCurrentAct();
            }
        }




        private void SpawnNoteForCurrentAct()
        {
            // we only bother with this in the bedroom
            if (GameStateManager.Instance.GetCurrentWorldLocation() != Types.WorldLocation.Bedroom) { return; }
            
            int currentAct = GameStateManager.Instance.GetCurrentWorldClockHour();
            // look through each act
            if (currentAct == 1 || currentAct == 2 || currentAct == 3)
            {
                // look for the object in the scene called "NoteSpawnLocation"
                GameObject spawnLocation = GameObject.Find("NoteSpawnLocation");
                // spawn the note prefab at the location of the "NoteSpawnLocation" object
                if (!spawnLocation) { return;}
                
                _currentNoteResearcher = Instantiate(_notePrefab, spawnLocation.transform.position, Quaternion.identity);
                // cast to a Letter and set the letter type to researcher
                _currentNoteResearcher.GetComponent<Letter>().SetLetterType(Types.LetterType.Researcher);
                // Start the sliding coroutine
                StartCoroutine(SlideNote(_currentNoteResearcher));
                
                // now we will also send a friend letter, but we will delay it by a few seconds and have it slide in after the researcher letter
                _currentNoteFriend = Instantiate(_notePrefab, spawnLocation.transform.position, Quaternion.identity);
                _currentNoteFriend.GetComponent<Letter>().SetLetterType(Types.LetterType.Friend);
                StartCoroutine(DelayedSlideNote(_currentNoteFriend, 2f)); // Delay of 2 second before sliding in the friend note
                
            }
            
        }
        
        private IEnumerator DelayedSlideNote(GameObject note, float delay)
        {
            yield return new WaitForSeconds(delay);
            StartCoroutine(SlideNote(note));
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

        public IEnumerator ReverseSlideNote(GameObject note)
        {
            // this will work identically to the slide note, but it will just reverse the start and end positions and rotations, so it will slide back to the center and unrotate itself
                if (note == null){ yield break;}
                Vector3 startPosition = note.transform.position;
                GameObject spawnLocation = GameObject.Find("NoteSpawnLocation");
                // spawn the note prefab at the location of the "NoteSpawnLocation" object
                if (!spawnLocation) { yield break;}
                Vector3 endPosition = spawnLocation.transform.position;
                Quaternion startRotation = note.transform.rotation;
                Quaternion targetRotation = Quaternion.identity;
                float elapsedTime = 0f;
                while (elapsedTime < slideDuration)
                {
                    elapsedTime += Time.deltaTime;
                    float normalizedTime = elapsedTime / slideDuration;
                    float curveValue = slideCurve.Evaluate(normalizedTime);
                    note.transform.position = Vector3.Lerp(startPosition, endPosition, curveValue);
                    note.transform.rotation = Quaternion.Lerp(startRotation, targetRotation, curveValue);
                    yield return null;
                }
                note.transform.position = endPosition;
                note.transform.rotation = targetRotation;
                Destroy(note);
        }
        
    }
}