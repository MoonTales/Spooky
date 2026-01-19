using System;
using Player;
using Unity.Cinemachine;
using UnityEngine;
using Types = System.Types;

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
            inspectionPoint.localPosition = new Vector3(0, 0, inspectionDistance);
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
        
        // Initialize target rotation to current rotation
        
        // Set inspecting flag
        _isInspecting = true;
        
        // Save and disable pan/tilt (to fix the camera glitch that was happened when we ended inspection)
        if (panTilt != null)
        {
            savedPanTilt = new Vector2(panTilt.PanAxis.Value, panTilt.TiltAxis.Value);
            panTilt.enabled = false;
        }
        EventBroadcaster.Broadcast_GameStateChanged(Types.GameState.Inspecting);
    }
    
    public void EndInspection()
    {
        // If not inspecting, do nothing
        if (!_isInspecting){ return;}
    
        // Start the exit transition
        _isExitingInspection = true;
    
        // Lock and hide cursor again
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    private void HandleInspection()
{
    // Smooth move to inspection point
    // we need to ensure we are not actively zooming though
    if (!isZooming)
    {
        _currentInspectedObject.transform.localPosition = Vector3.Lerp(_currentInspectedObject.transform.localPosition, targetZoomPosition, Time.deltaTime * transitionSpeed);
    }
    
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
    if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
    {
        EndInspection();
    }
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
        
            // Clear inspecting flags and current object
            _isInspecting = false;
            _isExitingInspection = false;
            _currentInspectedObject = null;
        
            // Restore and re-enable pan/tilt
            if (panTilt != null)
            {
                panTilt.PanAxis.Value = savedPanTilt.x;
                panTilt.TiltAxis.Value = savedPanTilt.y;
                panTilt.enabled = true;
            }
            EventBroadcaster.Broadcast_GameStateChanged(Types.GameState.Gameplay);
        }
    }
}