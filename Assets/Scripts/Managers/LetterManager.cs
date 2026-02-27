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
    public class LetterManager : Singleton<LetterManager>, ISaveSystemInterface<LetterManager.LetterSaveData>
    {

        public struct LetterSaveData
        {
            public bool HasReadAct1ResearcherLetter;
            public bool HasReadAct1FriendLetter;
            public bool HasReadAct2ResearcherLetter;
            public bool HasReadAct2FriendLetter;
            public bool HasReadAct3ResearcherLetter;
            public bool HasReadAct3FriendLetter;
        }
        
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

        
        // we will hold a little reference of what letters the player has read, and only spawn ones they havent read yet.
        //these options are:
        // Act1: Researcher Letter
        // Act1: Friend Letter
        // Act2: Researcher Letter
        // Act2: Friend Letter
        // Act3: Researcher Letter
        // Act3: Friend Letter
        private bool _hasReadAct1ResearcherLetter = false;
        private bool _hasReadAct1FriendLetter = false;
        private bool _hasReadAct2ResearcherLetter = false;
        private bool _hasReadAct2FriendLetter = false;
        private bool _hasReadAct3ResearcherLetter = false;
        private bool _hasReadAct3FriendLetter = false;
        
        
        
        
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

        public void HandleLetterRead(string id)
        {
            // brute force for now LOL
            if(id == "fren_letter_1"){ _hasReadAct1FriendLetter = true;}
            else if(id == "res_letter_1"){ _hasReadAct1ResearcherLetter = true;}
            else if(id == "fren_letter_2"){ _hasReadAct2FriendLetter = true;}
            else if(id == "res_letter_2"){ _hasReadAct2ResearcherLetter = true;}
            else if(id == "fren_letter_3"){ _hasReadAct3FriendLetter = true;}
            else if(id == "res_letter_3"){ _hasReadAct3ResearcherLetter = true;}
            
            SaveSystem.Instance.RequestSave(this);
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

                if (currentAct == 1)
                {
                    if (!_hasReadAct1ResearcherLetter)
                    {
                        // spawn the researcher letter for act 1
                        SpawnResearcherLetter(spawnLocation);
                    }
                    if (!_hasReadAct1FriendLetter)
                    {
                        SpawnFriendLetter(spawnLocation);
                    }
                }
                if (currentAct == 2)
                {
                    if (!_hasReadAct2ResearcherLetter)
                    {
                        // spawn the researcher letter for act 2
                        SpawnResearcherLetter(spawnLocation);
                    }
                    if (!_hasReadAct2FriendLetter)
                    {
                        SpawnFriendLetter(spawnLocation);
                    }
                }

                if (currentAct == 3)
                {
                    if (!_hasReadAct3ResearcherLetter)
                    {
                        // spawn the researcher letter for act 3
                        SpawnResearcherLetter(spawnLocation);
                    }

                    if (!_hasReadAct3FriendLetter)
                    {
                        SpawnFriendLetter(spawnLocation);
                    }
                }

            }
            
        }

        private void SpawnFriendLetter(GameObject spawnLocation)
        {
            // now we will also send a friend letter, but we will delay it by a few seconds and have it slide in after the researcher letter
            _currentNoteFriend = Instantiate(_notePrefab, spawnLocation.transform.position, Quaternion.Euler(-90f, 0f, -90f));
            _currentNoteFriend.GetComponent<Letter>().SetLetterType(Types.LetterType.Friend);
            StartCoroutine(DelayedSlideNote(_currentNoteFriend, 2f)); // Delay of 2 second before sliding in the friend note
        }

        private void SpawnResearcherLetter(GameObject spawnLocation)
        {
            _currentNoteResearcher = Instantiate(_notePrefab, spawnLocation.transform.position, Quaternion.Euler(-90f, 0f, -90f));
            // cast to a Letter and set the letter type to researcher
            _currentNoteResearcher.GetComponent<Letter>().SetLetterType(Types.LetterType.Researcher);
            // Start the sliding coroutine
            StartCoroutine(SlideNote(_currentNoteResearcher));
        }

        private IEnumerator DelayedSlideNote(GameObject note, float delay)
        {
            yield return new WaitForSeconds(delay);
            StartCoroutine(SlideNote(note));
        }
        private IEnumerator SlideNote(GameObject note)
        {
            if (note == null){ yield break;}
            EventBroadcaster.Broadcast_OnLetterSlide(note.transform);
            
            // Start at the spawn location (center of door)
            Vector3 startPosition = note.transform.position;
            
            // Calculate random offset destination with varied distance
            float randomXOffset = UnityEngine.Random.Range(-horizontalOffsetRange, horizontalOffsetRange);
            float randomDistance = slideDistance + UnityEngine.Random.Range(-slideDistanceVariation, slideDistanceVariation);
            Vector3 endPosition = startPosition + new Vector3(randomXOffset, 0, randomDistance);
            
            // Start with no rotation
            Quaternion startRotation = Quaternion.Euler(-90f, 0f, -90f);
            
            // Random target rotation angle (left or right) - rotates around Y-axis
            float randomYRotation = UnityEngine.Random.Range(-maxRotationAngle, maxRotationAngle);
            Quaternion targetRotation = Quaternion.Euler(-90f, randomYRotation, -90f);
            
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
        }

        public IEnumerator ReverseSlideNote(GameObject note)
        {
            // this will work identically to the slide note, but it will just reverse the start and end positions and rotations, so it will slide back to the center and unrotate itself
                if (note == null){ yield break;}
                EventBroadcaster.Broadcast_OnLetterSlide(note.transform);
                Vector3 startPosition = note.transform.position;
                GameObject spawnLocation = GameObject.Find("NoteSpawnLocation");
                // spawn the note prefab at the location of the "NoteSpawnLocation" object
                if (!spawnLocation) { yield break;}
                Vector3 endPosition = spawnLocation.transform.position;
                Quaternion startRotation = note.transform.rotation;
                Quaternion targetRotation = Quaternion.Euler(-90f, 0f, -90f);
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

        public string SaveId => "LetterManager";
        public LetterSaveData OnSave()
        {
            return new LetterSaveData
            {
                HasReadAct1ResearcherLetter = _hasReadAct1ResearcherLetter,
                HasReadAct1FriendLetter = _hasReadAct1FriendLetter,
                HasReadAct2ResearcherLetter = _hasReadAct2ResearcherLetter,
                HasReadAct2FriendLetter = _hasReadAct2FriendLetter,
                HasReadAct3ResearcherLetter = _hasReadAct3ResearcherLetter,
                HasReadAct3FriendLetter = _hasReadAct3FriendLetter
            };
        }
        public void OnLoad(LetterSaveData data)
        {
            _hasReadAct1ResearcherLetter = data.HasReadAct1ResearcherLetter;
            _hasReadAct1FriendLetter = data.HasReadAct1FriendLetter;
            _hasReadAct2ResearcherLetter = data.HasReadAct2ResearcherLetter;
            _hasReadAct2FriendLetter = data.HasReadAct2FriendLetter;
            _hasReadAct3ResearcherLetter = data.HasReadAct3ResearcherLetter;
            _hasReadAct3FriendLetter = data.HasReadAct3FriendLetter;
        }
    }
}
