using System;
using Player;
using Unity.Cinemachine;
using UnityEngine;

public class InspectionSystem : Singleton<InspectionSystem>
{
    [Header("Inspection Settings")]
    [SerializeField] private Transform inspectionPoint; // Create an empty GameObject as child of camera
    [SerializeField] private float inspectionDistance = 0.5f; // Distance in front of camera
    [SerializeField] private float transitionSpeed = 8f;
    [SerializeField] private float rotationSpeed = 100f;
    
    [Header("References")]
    [SerializeField] private Transform cameraTransform; // Main camera transform
    
    private GameObject currentInspectedObject;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Transform originalParent;
    private Rigidbody objectRigidbody;
    private Collider objectCollider;
    
    private bool isInspecting = false;
    private bool isExitingInspection = false; // Add this new flag

    private Vector3 prevMousePosition;
    private Quaternion targetRotation;
    
    private CinemachineCamera cinemaCamera;
    private CinemachinePanTilt panTilt;
    private Vector2 savedPanTilt; // Save the pan/tilt values
    
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
        if (isInspecting)
        {
            if (isExitingInspection)
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
        if (isInspecting || objectToInspect == null){ return;}
        isExitingInspection = false;
        // Set current inspected object
        currentInspectedObject = objectToInspect;
        
        // Store original state
        originalPosition = currentInspectedObject.transform.position;
        originalRotation = currentInspectedObject.transform.rotation;
        originalParent = currentInspectedObject.transform.parent;
        
        // Disable physics (on all objects, as it mayt have child rigidbodies)
        var objectRigitbodies = currentInspectedObject.GetComponentsInChildren<Rigidbody>();
        foreach (var rb in objectRigitbodies)
        {
            rb.isKinematic = true;
        }
        
        // disable collider during inspection
        var objectColliders = currentInspectedObject.GetComponentsInChildren<Collider>();
        foreach (var col in objectColliders)
        {
            col.enabled = false;
        }
        
        // Parent to inspection point so it follows camera
        currentInspectedObject.transform.SetParent(inspectionPoint);
        
        // Initialize target rotation to current rotation
        targetRotation = currentInspectedObject.transform.rotation;
        
        // Set inspecting flag
        isInspecting = true;
        
        // Save and disable pan/tilt (to fix the camera glitch that was happened when we ended inspection)
        if (panTilt != null)
        {
            savedPanTilt = new Vector2(panTilt.PanAxis.Value, panTilt.TiltAxis.Value);
            panTilt.enabled = false;
        }
        
        // Enable and unlock cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    
    public void EndInspection()
    {
        // If not inspecting, do nothing
        if (!isInspecting){ return;}
    
        // Start the exit transition
        isExitingInspection = true;
    
        // Lock and hide cursor again
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    private void HandleInspection()
    {
        // Smooth move to inspection point
        currentInspectedObject.transform.localPosition = Vector3.Lerp(currentInspectedObject.transform.localPosition, Vector3.zero, Time.deltaTime * transitionSpeed);
    
        // when we press "1", i want to slowly spin the object to the left
        if (Input.GetKey(KeyCode.Alpha1))
        {
            currentInspectedObject.transform.Rotate(Vector3.up, -rotationSpeed * Time.deltaTime, Space.World);
        }
        // when we press "3", i want to slowly spin the object to the right
        if (Input.GetKey(KeyCode.Alpha3))
        {
            currentInspectedObject.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        }
    
        // When mouse is dragged, rotate object based on mouse movement
        if (Input.GetMouseButtonDown(0))
        {
            prevMousePosition = Input.mousePosition;
        }
        else if (Input.GetMouseButton(0))
        {
            Vector3 delta = Input.mousePosition - prevMousePosition;
        
            // Rotate around camera's up axis (horizontal drag = left/right rotation)
            float horizontalRotation = -delta.x * 0.2f;
            currentInspectedObject.transform.Rotate(cameraTransform.up, horizontalRotation, Space.World);
        
            // Rotate around camera's right axis (vertical drag = up/down rotation)
            float verticalRotation = delta.y * 0.2f;
            currentInspectedObject.transform.Rotate(cameraTransform.right, verticalRotation, Space.World);
        
            prevMousePosition = Input.mousePosition;
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
        currentInspectedObject.transform.position = Vector3.Lerp(
            currentInspectedObject.transform.position, 
            originalPosition, 
            Time.deltaTime * transitionSpeed
        );
    
        // Smoothly rotate back to original rotation
        currentInspectedObject.transform.rotation = Quaternion.Slerp(
            currentInspectedObject.transform.rotation, 
            originalRotation, 
            Time.deltaTime * transitionSpeed
        );
    
        // Check if we're close enough to the original position/rotation
        float positionDistance = Vector3.Distance(currentInspectedObject.transform.position, originalPosition);
        float rotationDistance = Quaternion.Angle(currentInspectedObject.transform.rotation, originalRotation);
    
        if (positionDistance < 0.01f && rotationDistance < 1f)
        {
            // Snap to final position and complete the exit
            currentInspectedObject.transform.SetParent(originalParent);
            currentInspectedObject.transform.position = originalPosition;
            currentInspectedObject.transform.rotation = originalRotation;
        
            // Re-enable physics (if we had one)
            var objectRigitbodies = currentInspectedObject.GetComponentsInChildren<Rigidbody>();
            foreach (var rb in objectRigitbodies)
            {
                rb.isKinematic = false;
            }
        
            // Re-enable collider (if we had one)
            var objectColliders = currentInspectedObject.GetComponentsInChildren<Collider>();
            foreach (var col in objectColliders)
            {
                col.enabled = true;
            }
        
            // Clear inspecting flags and current object
            isInspecting = false;
            isExitingInspection = false;
            currentInspectedObject = null;
        
            // Restore and re-enable pan/tilt
            if (panTilt != null)
            {
                panTilt.PanAxis.Value = savedPanTilt.x;
                panTilt.TiltAxis.Value = savedPanTilt.y;
                panTilt.enabled = true;
            }
        }
    }
}