using System;
using Player;
using Unity.Cinemachine;
using UnityEngine;
using Types = System.Types;
using Inspection;
using Interaction.Letters;
using Managers;
using UI;

public class InspectionSystem : Singleton<InspectionSystem>
{
    [Header("Inspection Settings")]
    [SerializeField] private Transform inspectionPoint; // Create an empty GameObject as child of camera
    [SerializeField] private float inspectionDistance = 0.5f; // Distance in front of camera
    [SerializeField] private float transitionSpeed = 8f;
    [SerializeField] private float rotationStrength = 0.2f;
    [SerializeField] private float minZoomDistance = -0.5f;
    [SerializeField] private float maxZoomDistance = 0.5f;
    
    [Header("References")]
    [SerializeField] private Transform cameraTransform; // Main camera transform
    [SerializeField] private Texture2D letterWritingTexture;
    
    private GameObject _currentInspectedObject;
    private Vector3 _originalPosition;
    private Quaternion _originalRotation;
    private Transform _originalParent;
    private Rigidbody _objectRigidbody;
    private Collider _objectCollider;
    private bool _isInspecting = false;
    private bool _isExitingInspection = false; // Add this new flag
    private Vector3 _prevMousePosition;
    private CinemachineCamera cinemaCamera;
    private CinemachinePanTilt panTilt;
    private Vector2 savedPanTilt; // Save the pan/tilt values
    private bool isZooming = false;
    private Vector3 targetZoomPosition = Vector3.zero;
    
    private LayerMask _cachedLayerMask;
    
    private bool _isFirstInspection = true; // Flag to track if it's the first inspection
    
    // fix:
    // you need to be inspecting an object for atleast 0.5 seconds before you can exit
    // this is to stop the low fsp issue of it returning to the og position
    float inspectionStartTime = 0f;
    bool canExitInspection => inspectionStartTime >= 0.5f;
    
    
    void Start()
    {
        // Get the pan/tilt component from the Cinemachine camera (which is on the player)
        cinemaCamera = PlayerManager.Instance.GetCinemachineCamera();
        panTilt = cinemaCamera.GetComponent<CinemachinePanTilt>();
        // ---
        
        // If no inspection point is set, create one roughly in front of the player camera
        if (inspectionPoint == null)
        {
            GameObject inspectionObj = new GameObject("InspectionPoint");
            inspectionPoint = inspectionObj.transform;
            inspectionPoint.SetParent(cameraTransform);

            // TODO: [POLISH] think of a way to have this object sit at a position that doesn't clip into the inspection text
            // -- IF HAS TEXT, potentially designate an area for the item, have it be at the center of that
            // ----- [if HAS TEXT is basically that you'd need to check if a valid TextKey exists, because the component exists on it anyway]
            // -- ELSE, just do 0, 0 (this would be for drawings)
            inspectionPoint.localPosition = new Vector3(-0.1f, 0, inspectionDistance);
            inspectionPoint.localRotation = Quaternion.identity;
        }
    }
    
    void Update()
    {
        // check if we are currently inspecting an object
        if (_isInspecting)
        {
            if (_isExitingInspection)
            {
                HandleExitTransition();
            }
            else
            {
                HandleInspection();
            }
        }
        
        // update the time
        if (_isInspecting && !canExitInspection)
        {
            inspectionStartTime += Time.deltaTime;
        }
    }
    
    
    
    // Public function which can be called from any other script to start inspecting an object (itself or another)
    public void StartInspection(GameObject objectToInspect)
    {
        // Prevent starting a new inspection if already inspecting an object, or if we have no object
        if (_isInspecting || objectToInspect == null){ return;}
        _isExitingInspection = false;
        // Set current inspected object
        _currentInspectedObject = objectToInspect;
        
        // Store original state
        _originalPosition = _currentInspectedObject.transform.position;
        _originalRotation = _currentInspectedObject.transform.rotation;
        _originalParent = _currentInspectedObject.transform.parent;
        
        // Disable physics (on all objects, as it mayt have child rigidbodies)
        var objectRigitbodies = _currentInspectedObject.GetComponentsInChildren<Rigidbody>();
        foreach (var rb in objectRigitbodies)
        {
            rb.isKinematic = true;
        }
        
        // disable collider during inspection
        var objectColliders = _currentInspectedObject.GetComponentsInChildren<Collider>();
        foreach (var col in objectColliders)
        {
            col.enabled = false;
        }
        
        // Parent to inspection point so it follows camera
        _currentInspectedObject.transform.SetParent(inspectionPoint);
        
        //TODO: Initialize target zoom rotation to current rotation
        
        // Set inspecting flag
        _isInspecting = true;
        
        // Save and disable pan/tilt (to fix the camera glitch that was happened when we ended inspection)
        if (panTilt != null)
        {
            savedPanTilt = new Vector2(panTilt.PanAxis.Value, panTilt.TiltAxis.Value);
            panTilt.enabled = false;
        }
        EventBroadcaster.Broadcast_GameStateChanged(Types.GameState.Inspecting);
        
        // we need to do this to all child layers as well
        _cachedLayerMask = _currentInspectedObject.layer;
        SetLayerRecursively(_currentInspectedObject, LayerMask.NameToLayer("Inspection"));
        
        if (_isFirstInspection)
        {
            Types.NotificationData data = new(
                duration: 3, 
                messageKey: new TextKey { place = "tutorial", id = "inspect" },
                shouldOnlyShowOnce: true
            );
            data.Send();
            _isFirstInspection = false;
        }
    }
    
    
    public void EndInspection()
    {
        // If not inspecting, do nothing
        if (!_isInspecting){ return;}
    
        // Start the exit transition
        _isExitingInspection = true;
    
        _isExitingInspection = true;
        SetLayerRecursively(_currentInspectedObject, _cachedLayerMask);
        
        // reset the target zoom position
        targetZoomPosition = _currentInspectedObject.transform.localPosition;
        
        // Call any inspection finished logic on the inspected object
        // I should change this to just be the default
        InspectableObject inspectable = _currentInspectedObject.GetComponent<InspectableObject>();
        if (inspectable != null)
        {
            inspectable.OnInspectionFinished();
        }
        
        // reset the inspection start time for the next inspection
        inspectionStartTime = 0f; 
    }

    private void HandleInspection()
    {
        bool cursorInScrollView = false;

        if (PlayerHUDController.Instance != null)
        {
            if (PlayerHUDController.Instance.scrollViewChecker != null)
            {
                cursorInScrollView = PlayerHUDController.Instance.scrollViewChecker.IsCursorInScrollView();
            }
        }

        // Smooth move to inspection point
        // we need to ensure we are not actively zooming though
        if (!isZooming)
        {
            _currentInspectedObject.transform.localPosition = Vector3.Lerp(
                _currentInspectedObject.transform.localPosition,
                targetZoomPosition,
                Time.deltaTime * transitionSpeed
            );
        }

        if (!cursorInScrollView)
        {
            // MOUSE DRAG
            if (Input.GetMouseButtonDown(0))
            {
                _prevMousePosition = Input.mousePosition;
            }
            else if (Input.GetMouseButton(0))
            {
                Vector3 delta = Input.mousePosition - _prevMousePosition;


                // you may notice that the stuff is inverted... yeah I dont even know, this is what managed to make it work LOL
                // Horizontal drag = left/right rotation
                float horizontalRotation = -delta.x * rotationStrength;
                _currentInspectedObject.transform.Rotate(cameraTransform.up, horizontalRotation, Space.World);

                // Vertical drag = up/down rotation
                float verticalRotation = delta.y * rotationStrength;
                _currentInspectedObject.transform.Rotate(cameraTransform.right, verticalRotation, Space.World);

                _prevMousePosition = Input.mousePosition;
            }


            // ZOOM IN - OUT
            // get the value of the scroll wheel (which is between -1 and 1 ish)
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0f)
            {
                isZooming = true;

                // Adjust the local Z position (distance from camera in local space)
                Vector3 newPos = targetZoomPosition;
                newPos.z -= scroll * 2f; // Zoom speed factor
                newPos.z = Mathf.Clamp(newPos.z, minZoomDistance, maxZoomDistance);

                targetZoomPosition = newPos;
            }
            else
            {
                isZooming = false;
            }


            // Exit inspection with right click or ESC
            //TODO: fix this so that we can use F
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.F))
            {

                // ensure enough time has pass
                if (!canExitInspection) { return; }

                // only allow exit once the object is close enough to the inspection point (so we dont have weird snapping)
                if (Vector3.Distance(_currentInspectedObject.transform.localPosition, targetZoomPosition) < 0.05f)
                {
                    // determine if the object we are currently inspecting is:
                    // a) a research letter AND has not been written on yet
                    if (_currentInspectedObject.GetComponent<Letter>() != null && !_currentInspectedObject.GetComponent<Letter>().GetHasBeenWrittenOn())
                    {
                        // if its a friend letter
                        if (_currentInspectedObject.GetComponent<Letter>().GetLetterType() == Types.LetterType.Friend)
                        {
                            // if so, we want to do some unique logic for that (like showing the writing UI)
                            EndInspection();
                            return;
                        }
                        // if so, we want to do some unique logic for that (like showing the writing UI)
                        HandleUniqueInspectionLogic();
                        return;
                    }
                    else
                    {
                        EndInspection();
                    }

                    // handles anything other than a letter
                    if (!_currentInspectedObject.GetComponent<Letter>())
                    {
                        // if not, just end the inspection normally
                        EndInspection();
                    }

                }
            }
        }
        else
        {
            isZooming = false;
        }
    }

    private void HandleUniqueInspectionLogic()
    {
        // step 2) fade to black
        new Types.ScreenFadeData(3f, 3f, 3f,
            HandleFadeFinished,
            HandleScribbleNote
        ).Send();

    }

    private void HandleScribbleNote()
    {
        // at this point, we should play the scribble sounds and swap the letter model to the one with the writing on it, then fade back in
        if (GameStateManager.Instance != null
            && GameStateManager.Instance.GetCurrentWorldLocation() == Types.WorldLocation.Bedroom)
        {
            Transform sourceTransform = _currentInspectedObject != null ? _currentInspectedObject.transform : transform;
            if (!EventBroadcaster.Broadcast_OnLetterScribble(sourceTransform))
            {
                // Fallback for startup-order edge cases where AudioManager has not subscribed yet.
                AudioManager.Instance?.PlaySfx(AudioManager.SfxId.LetterScribble, sourceTransform);
            }
        }

        Letter letter = _currentInspectedObject.GetComponent<Letter>();
        letter.SetResponseTextKey();
        PlayerHUDController.Instance.RefreshInspectionText();
        
        //TODO: CHANGE THE VISUALS TOO
        // for now we will just like.. idk change the color?
        Renderer[] renderers = _currentInspectedObject.GetComponentsInChildren<Renderer>();
        foreach (Renderer thisRenderer in renderers)
        {
            thisRenderer.material.EnableKeyword("_DETAIL_MULX2"); // Essential for URP for some reason omg
            thisRenderer.material.SetTexture("_DetailAlbedoMap", letterWritingTexture);
        }
    }

    private void HandleFadeFinished()
    {
        Letter letter = _currentInspectedObject.GetComponent<Letter>();
        letter.SetHasBeenWrittenOn(true);
    }
    
    private void HandleExitTransition()
    {
        // Smoothly move back to original position
        _currentInspectedObject.transform.position = Vector3.Lerp(
            _currentInspectedObject.transform.position, 
            _originalPosition, 
            Time.deltaTime * transitionSpeed
        );
    
        // Smoothly rotate back to original rotation
        _currentInspectedObject.transform.rotation = Quaternion.Slerp(
            _currentInspectedObject.transform.rotation, 
            _originalRotation, 
            Time.deltaTime * transitionSpeed
        );
    
        // Check if we're close enough to the original position/rotation
        float positionDistance = Vector3.Distance(_currentInspectedObject.transform.position, _originalPosition);
        float rotationDistance = Quaternion.Angle(_currentInspectedObject.transform.rotation, _originalRotation);
    
        if (positionDistance < 0.01f && rotationDistance < 1f)
        {
            // Snap to final position and complete the exit
            _currentInspectedObject.transform.SetParent(_originalParent);
            _currentInspectedObject.transform.position = _originalPosition;
            _currentInspectedObject.transform.rotation = _originalRotation;
        
            // Re-enable physics (if we had one)
            var objectRigitbodies = _currentInspectedObject.GetComponentsInChildren<Rigidbody>();
            foreach (var rb in objectRigitbodies)
            {
                rb.isKinematic = false;
            }
        
            // Re-enable collider (if we had one)
            var objectColliders = _currentInspectedObject.GetComponentsInChildren<Collider>();
            foreach (var col in objectColliders)
            {
                col.enabled = true;
            }
        

        
            // Restore and re-enable pan/tilt
            if (panTilt != null)
            {
                panTilt.PanAxis.Value = savedPanTilt.x;
                panTilt.TiltAxis.Value = savedPanTilt.y;
                panTilt.enabled = true;
            }
            
            InspectableObject inspectable = _currentInspectedObject.GetComponent<InspectableObject>();
            if (inspectable != null)
            {
                inspectable.OnReturnedToOriginalPosition();
            }
            
            // Clear inspecting flags and current object
            _isInspecting = false;
            _isExitingInspection = false;
            _currentInspectedObject = null;
            
            // only set this, if we are not currently in the main menu (an edge case)
            if (GameStateManager.Instance.GetCurrentGameState() != Types.GameState.MainMenu)
            {
                EventBroadcaster.Broadcast_GameStateChanged(Types.GameState.Gameplay);
            }
        }
    }
    
    // This dosent actually work correctly for some reason!! :)
    // some stuff just isnt visible
    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    // Get inspected object thank u Cohen :D
    public InspectableObject GetCurrentInspectedObject()
    {
        if (_currentInspectedObject != null)
        {
            return _currentInspectedObject.GetComponent<InspectableObject>();
        }
        return null;
    }
    
    protected override void OnGameStateChanged(Types.GameState newstate)
    {
        // If we are inspecting and the game state changes away from inspecting, end inspection
        // UNLESS we are pausing the game while inspecting, we will allow that
        if (_isInspecting && newstate != Types.GameState.Inspecting)
        {
            if (newstate == Types.GameState.Paused) { return; }
            EndInspection();
        }
    }
}
