using System;
using Managers;
using Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using Types = System.Types;

public class Drawing : MonoBehaviour, IInteractable
{
    [Header("Drawing Settings")]
    [SerializeField, Tooltip("What area does this item exist within?")] 
    private Types.WorldLocation location = Types.WorldLocation.Bedroom;
    [SerializeField] private int drawingID;
    
    [Header("Pickup Settings")]
    [SerializeField] private float pickupTransitionSpeed = 8f;
    [SerializeField] private float returnTransitionSpeed = 8f;
    [SerializeField] private Vector3 handOffset = new Vector3(0, 0, 0.3f);
    
    // Pickup state
    private bool _isPickedUp = false;
    private bool _isTransitioningToHand = false;
    private Transform _handTransform;
    private Vector3 _originalPosition;
    private Quaternion _originalRotation;
    private Vector3 _originalScale;
    private Transform _originalParent;
    private Rigidbody _rigidbody;
    private Collider[] _colliders;
    private static Drawing _currentlyHeldDrawing = null;
    
    private bool _isReturningToPosition = false;
    private Vector3 _returnTargetPosition;
    private Quaternion _returnTargetRotation;
    private Vector3 _returnTargetScale;
    private Transform _returnTargetParent;
    
    // Interface Properties
    public string Prompt 
    { 
        get 
        {
            return "Examine Drawing";
        }
    }
    
    private void Start()
    {
        // setup references
        _rigidbody = GetComponent<Rigidbody>();
        _colliders = GetComponentsInChildren<Collider>();
        InitializeDrawingState();
    }
    
    public bool CanInteract(Interactor interactor)
    {
        // Can't interact with yourself if you're being held or returning
        if (_isPickedUp || _isReturningToPosition) return false;
        return true;
    }


    
    
    private void Update()
    {
        if (_isTransitioningToHand)
        {
            HandlePickupTransition();
        }
        else if (_isPickedUp)
        {
            HandleHoldingDrawing();
        }
        else if (_isReturningToPosition)
        {
            HandleReturnTransition();
        }
    }

    public void Interact(Interactor interactor)
    {
        // If player is holding a drawing and this slot has a drawing, swap them
        if (_currentlyHeldDrawing != null)
        {
            SwapDrawings(_currentlyHeldDrawing);
            return;
        }
    
        // Normal interaction logic
        DebugUtils.Log($"Player interacted with Drawing ID {drawingID}");
    
        if (IsDrawingInInventory())
        {
            PickupDrawing();
        }
        else
        {
            CollectDrawing();
        }
    }
    
    private void SwapDrawings(Drawing heldDrawing)
    {
        DebugUtils.Log($"Swapping Drawing ID {heldDrawing.drawingID} with Drawing ID {drawingID}");

        // Store the location where the held drawing came from
        Vector3 heldOriginalPos = heldDrawing._originalPosition;
        Quaternion heldOriginalRot = heldDrawing._originalRotation;
        Vector3 heldOriginalScale = heldDrawing._originalScale;
        Transform heldOriginalParent = heldDrawing._originalParent;

        // Store this drawing's ID
        int tempID = drawingID;

        // Swap IDs
        drawingID = heldDrawing.drawingID;
        heldDrawing.drawingID = tempID;

        // Setup return transition for held drawing to THIS slot
        heldDrawing._returnTargetPosition = this.transform.position;
        heldDrawing._returnTargetRotation = this.transform.rotation;
        heldDrawing._returnTargetScale = this.transform.localScale;
        heldDrawing._returnTargetParent = this.transform.parent;

        // Update held drawing's original to THIS location
        heldDrawing._originalPosition = this.transform.position;
        heldDrawing._originalRotation = this.transform.rotation;
        heldDrawing._originalScale = this.transform.localScale;
        heldDrawing._originalParent = this.transform.parent;

        // Start smooth return for held drawing
        heldDrawing.StartReturnTransition();
        heldDrawing.gameObject.SetActive(true);

        // Clear the held reference before picking up new one
        _currentlyHeldDrawing = null;

        // Get the hand transform
        _handTransform = PlayerManager.Instance.GetPlayerHandTransform();
        
        if (_handTransform == null)
        {
            DebugUtils.LogWarning("HAND transform not found on player!");
            return;
        }

        // Set where THIS drawing should return to if dropped
        _originalPosition = heldOriginalPos;
        _originalRotation = heldOriginalRot;
        _originalScale = heldOriginalScale;
        _originalParent = heldOriginalParent;

        // Pickup this drawing
        SetPhysicsState(true);
        ParentToHand();
        StartPickupTransition();
        _currentlyHeldDrawing = this;
    }
    
    private void StartReturnTransition()
    {
        // Unparent from hand first
        transform.SetParent(_returnTargetParent);
        
        // Move to active scene
        SceneManager.MoveGameObjectToScene(gameObject, SceneManager.GetActiveScene());
        
        // Disable physics during transition
        SetPhysicsState(true);
        
        // Start the return animation
        _isReturningToPosition = true;
        _isPickedUp = false;
        _isTransitioningToHand = false;
    }
    
    private void HandleReturnTransition()
    {
        // Smoothly move to target position
        transform.position = Vector3.Lerp(
            transform.position,
            _returnTargetPosition,
            Time.deltaTime * returnTransitionSpeed
        );
        
        // Smoothly rotate to target rotation
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            _returnTargetRotation,
            Time.deltaTime * returnTransitionSpeed
        );
        
        // Smoothly scale to target scale
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            _returnTargetScale,
            Time.deltaTime * returnTransitionSpeed
        );
        
        // Check if transition is complete
        if (IsReturnTransitionComplete())
        {
            SnapToReturnPosition();
            CompleteReturnTransition();
        }
    }
    
    private bool IsReturnTransitionComplete()
    {
        float positionDistance = Vector3.Distance(transform.position, _returnTargetPosition);
        float rotationDistance = Quaternion.Angle(transform.rotation, _returnTargetRotation);
        float scaleDistance = Vector3.Distance(transform.localScale, _returnTargetScale);
        
        return positionDistance < 0.01f && rotationDistance < 1f && scaleDistance < 0.01f;
    }
    
    private void SnapToReturnPosition()
    {
        transform.position = _returnTargetPosition;
        transform.rotation = _returnTargetRotation;
        transform.localScale = _returnTargetScale;
    }
    
    private void CompleteReturnTransition()
    {
        _isReturningToPosition = false;
        SetPhysicsState(false); // Re-enable physics
    }
    
    #region Initialization
    
    private void InitializeDrawingState()
    {
        // check if we have the drawing currently in our inventory
        bool hasDrawing = IsDrawingInInventory();
        
        // if we do, we need to adjust the visibility based on our location (well.. the location the drawing exists in)
        if (hasDrawing)
        {
            HandleDrawingVisibility(bedroomVisible: true, nightmareVisible: false);
        }
        else
        {
            HandleDrawingVisibility(bedroomVisible: false, nightmareVisible: true);
        }
    }
    
    private void HandleDrawingVisibility(bool bedroomVisible, bool nightmareVisible)
    {
        if (IsInBedroom())
        {
            gameObject.SetActive(bedroomVisible);
        }
        else if (IsInNightmare())
        {
            gameObject.SetActive(nightmareVisible);
        }
    }
    
    #endregion

    #region Collection
    
    private void CollectDrawing()
    {
        PlayerInventory.Instance.AddDrawing(drawingID);
        gameObject.SetActive(false);
    }
    
    #endregion

    #region Pickup & Examination
    
    private void PickupDrawing()
    {
        DebugUtils.Log($"Player picked up Drawing ID {drawingID} to examine it.");
        
        _handTransform = PlayerManager.Instance.GetPlayerHandTransform();
        
        if (_handTransform == null)
        {
            DebugUtils.LogWarning("HAND transform not found on player!");
            return;
        }
        
        StoreOriginalTransform();
        SetPhysicsState(true);
        ParentToHand();
        StartPickupTransition();
        _currentlyHeldDrawing = this;
    }
    
    private void StoreOriginalTransform()
    {
        _originalPosition = transform.position;
        _originalRotation = transform.rotation;
        _originalParent = transform.parent;
        _originalScale = transform.localScale;
    }
    
    private void SetPhysicsState(bool isKinematic)
    {
        if (_rigidbody != null)
        {
            _rigidbody.isKinematic = isKinematic;
        }
    
        if (_colliders != null)
        {
            foreach (var col in _colliders)
            {
                if (col != null)
                {
                    col.enabled = !isKinematic;
                }
            }
        }
    }
    
    private void ParentToHand()
    {
        Vector3 worldScale = transform.lossyScale;
        transform.SetParent(_handTransform, true);
        transform.localScale = worldScale;
    }
    
    private void StartPickupTransition()
    {
        _isPickedUp = true;
        _isTransitioningToHand = true;
    }
    
    private void HandlePickupTransition()
    {
        transform.localPosition = Vector3.Lerp(
            transform.localPosition, 
            handOffset, 
            Time.deltaTime * pickupTransitionSpeed
        );
        
        transform.localRotation = Quaternion.Slerp(
            transform.localRotation, 
            Quaternion.identity, 
            Time.deltaTime * pickupTransitionSpeed
        );
        
        if (IsTransitionComplete())
        {
            SnapToFinalPosition();
            _isTransitioningToHand = false;
        }
    }
    
    private bool IsTransitionComplete()
    {
        float positionDistance = Vector3.Distance(transform.localPosition, handOffset);
        float rotationDistance = Quaternion.Angle(transform.localRotation, Quaternion.identity);
        
        return positionDistance < 0.01f && rotationDistance < 1f;
    }
    
    private void SnapToFinalPosition()
    {
        transform.localPosition = handOffset;
        transform.localRotation = Quaternion.identity;
    }
    
    private void HandleHoldingDrawing()
    {
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.F))
        {
            DropDrawing();
        }
    }
    
    private void DropDrawing()
    {
        DebugUtils.Log($"Player dropped Drawing ID {drawingID}");
    
        // Setup return transition
        _returnTargetPosition = _originalPosition;
        _returnTargetRotation = _originalRotation;
        _returnTargetScale = _originalScale;
        _returnTargetParent = _originalParent;
        
        // Start smooth return
        StartReturnTransition();
        _currentlyHeldDrawing = null;
    
        EventBroadcaster.Broadcast_GameStateChanged(Types.GameState.Gameplay);
    }
    
    #endregion

    #region Helper Methods
    
    private bool IsDrawingInInventory()
    {
        return PlayerInventory.Instance.HasDrawing(drawingID);
    }
    
    private bool IsInBedroom()
    {
        return location == Types.WorldLocation.Bedroom;
    }
    
    private bool IsInNightmare()
    {
        return location == Types.WorldLocation.Nightmare;
    }
    
    #endregion
}