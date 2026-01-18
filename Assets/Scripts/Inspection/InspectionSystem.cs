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
            HandleInspection();
        }
    }
    
    // Public function which can be called from any other script to start inspecting an object (itself or another)
    public void StartInspection(GameObject objectToInspect)
    {
        // Prevent starting a new inspection if already inspecting an object, or if we have no object
        if (isInspecting || objectToInspect == null){ return;}
        
        // Set current inspected object
        currentInspectedObject = objectToInspect;
        
        // Store original state
        originalPosition = currentInspectedObject.transform.position;
        originalRotation = currentInspectedObject.transform.rotation;
        originalParent = currentInspectedObject.transform.parent;
        
        // Disable physics
        objectRigidbody = currentInspectedObject.GetComponent<Rigidbody>();
        if (objectRigidbody != null) { objectRigidbody.isKinematic = true; }
        
        // disable collider during inspection
        objectCollider = currentInspectedObject.GetComponent<Collider>();
        if (objectCollider != null) { objectCollider.enabled = false; }
        
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
    }
    
    public void EndInspection()
    {
        // If not inspecting, do nothing
        if (!isInspecting){ return;}
        
        // Restore original state of the object
        currentInspectedObject.transform.SetParent(originalParent);
        currentInspectedObject.transform.position = originalPosition;
        currentInspectedObject.transform.rotation = originalRotation;
        
        // Re-enable physics (if we had one)
        if (objectRigidbody != null) { objectRigidbody.isKinematic = false; }
        
        // Re-enable collider (if we had one)
        if (objectCollider != null) { objectCollider.enabled = true; }
        
        // Clear inspecting flag and current object
        isInspecting = false;
        currentInspectedObject = null;
        
        // Restore and re-enable pan/tilt
        if (panTilt != null)
        {
            panTilt.PanAxis.Value = savedPanTilt.x;
            panTilt.TiltAxis.Value = savedPanTilt.y;
            panTilt.enabled = true;
        }
    }
    
    private void HandleInspection()
    {
        // Smooth move to inspection point
        currentInspectedObject.transform.localPosition = Vector3.Lerp(currentInspectedObject.transform.localPosition, Vector3.zero, Time.deltaTime * transitionSpeed);
        
        // when we press "1", i want to slowly spin the object to the left
        if (Input.GetKey(KeyCode.Alpha1))
        {
            targetRotation *= Quaternion.Euler(0, -rotationSpeed * Time.deltaTime, 0);
        }
        // when we press "3", i want to slowly spin the object to the right
        if (Input.GetKey(KeyCode.Alpha3))
        {
            targetRotation *= Quaternion.Euler(0, rotationSpeed * Time.deltaTime, 0);
        }
        
        
        // Smooth rotate to target rotation
        currentInspectedObject.transform.rotation = Quaternion.Slerp(currentInspectedObject.transform.rotation, targetRotation, Time.deltaTime * transitionSpeed);
        // Exit inspection with right click or ESC
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            EndInspection();
        }
    }
}