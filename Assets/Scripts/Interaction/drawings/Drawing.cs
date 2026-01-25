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
            if (IsEmptySlot())
            {
                return _currentlyHeldDrawing != null ? "Place Drawing" : "Empty Frame";
            }
            return "Examine Drawing";
        }
    }
    
    public bool CanInteract(Interactor interactor)
    {
        // Can't interact with yourself if you're being held
        if (_isPickedUp) return false;
    
        // Empty slots can only be interacted with if holding a drawing
        if (IsEmptySlot())
        {
            return _currentlyHeldDrawing != null;
        }
    
        return true;
    }

    private void Start()
    {
        CachePhysicsComponents();
        InitializeDrawingState();
    }
    
    private void CachePhysicsComponents()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _colliders = GetComponentsInChildren<Collider>();
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
    }

    public void Interact(Interactor interactor)
    {
        // If this is an empty slot and player is holding a drawing, place it
        if (IsEmptySlot() && _currentlyHeldDrawing != null)
        {
            PlaceDrawingHere(_currentlyHeldDrawing);
            return;
        }
    
        // If player is holding a drawing and this slot has a drawing, swap them
        if (_currentlyHeldDrawing != null && !IsEmptySlot())
        {
            SwapDrawings(_currentlyHeldDrawing);
            return;
        }
    
        // Can't interact with empty slots when not holding anything
        if (IsEmptySlot())
        {
            DebugUtils.LogWarning("This is an empty frame slot.");
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

    
    private void PlaceDrawingHere(Drawing heldDrawing)
    {
        DebugUtils.Log($"Placing Drawing ID {heldDrawing.drawingID} into empty slot");

        // Update the held drawing's transform to match this slot
        heldDrawing._originalPosition = this.transform.position;
        heldDrawing._originalRotation = this.transform.rotation;
        heldDrawing._originalScale = this.transform.localScale;
        heldDrawing._originalParent = this.transform.parent;

        // Position the held drawing at this location
        heldDrawing.transform.SetParent(this.transform.parent);
        heldDrawing.transform.position = this.transform.position;
        heldDrawing.transform.rotation = this.transform.rotation;
        heldDrawing.transform.localScale = this.transform.localScale;
        
        SceneManager.MoveGameObjectToScene(heldDrawing.gameObject, SceneManager.GetActiveScene());


        // Clean up the held drawing's state
        heldDrawing.ForceDropWithoutReturning();
        heldDrawing.gameObject.SetActive(true);

        // Only disable colliders if THIS is an empty slot (-1)
        // This makes the -1 slot "invisible" to raycasts after placement
        if (IsEmptySlot() && _colliders != null)
        {
            foreach (var col in _colliders)
            {
                if (col != null)
                {
                    col.enabled = false;
                }
            }
        }

        // Clear the static reference
        _currentlyHeldDrawing = null;

        EventBroadcaster.Broadcast_GameStateChanged(Types.GameState.Gameplay);
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

    // Place the held drawing at THIS slot's location
    heldDrawing.transform.SetParent(this.transform.parent);
    heldDrawing.transform.position = this.transform.position;
    heldDrawing.transform.rotation = this.transform.rotation;
    heldDrawing.transform.localScale = this.transform.localScale;

    // Update held drawing's original to THIS location
    heldDrawing._originalPosition = this.transform.position;
    heldDrawing._originalRotation = this.transform.rotation;
    heldDrawing._originalScale = this.transform.localScale;
    heldDrawing._originalParent = this.transform.parent;

    SceneManager.MoveGameObjectToScene(heldDrawing.gameObject, SceneManager.GetActiveScene());
    
    // Clean up the held drawing
    heldDrawing.ForceDropWithoutReturning();
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

    // IMPORTANT: Set the original position BEFORE parenting
    // This is where THIS drawing should return to if dropped
    _originalPosition = heldOriginalPos;
    _originalRotation = heldOriginalRot;
    _originalScale = heldOriginalScale;
    _originalParent = heldOriginalParent;

    // Now manually do the pickup without calling StoreOriginalTransform()
    SetPhysicsState(true); // Disable physics
    ParentToHand();
    StartPickupTransition();
    _currentlyHeldDrawing = this;
}
    
    private void ForceDropWithoutReturning()
    {
        // Used when placing/swapping - doesn't return to original position
        SetPhysicsState(false);
        ResetPickupState();
    }
    
    #region Initialization
    
    private void InitializeDrawingState()
    {
        // Empty slots are always visible in bedroom
        if (IsEmptySlot())
        {
            if (!IsInBedroom())
            {
                DebugUtils.LogWarning($"Empty slot (ID -1) found in {location}. Empty slots should only be in Bedroom!");
            }
            gameObject.SetActive(true);
            return;
        }
        bool hasDrawing = IsDrawingInInventory();
        
        if (hasDrawing)
        {
            HandleAlreadyCollectedDrawing();
        }
        else
        {
            HandleUnCollectedDrawing();
        }
    }
    
    private void HandleAlreadyCollectedDrawing()
    {
        if (IsInBedroom())
        {
            // In bedroom, keep it visible for examination
            DebugUtils.Log($"Drawing ID {drawingID} already collected. Keeping visible in Bedroom for examination.");
            gameObject.SetActive(true);
        }
        else if (IsInNightmare())
        {
            // In nightmare, hide it since it's collected
            DebugUtils.Log($"Drawing ID {drawingID} already collected. Hiding in Nightmare.");
            gameObject.SetActive(false);
        }
    }
    
    private void HandleUnCollectedDrawing()
    {
        if (IsInBedroom())
        {
            // In bedroom, hide until collected in nightmare
            DebugUtils.Log($"Drawing ID {drawingID} not yet collected. Hiding in Bedroom.");
            gameObject.SetActive(false);
        }
        else if (IsInNightmare())
        {
            // In nightmare, show for collection
            DebugUtils.Log($"Drawing ID {drawingID} not yet collected. Showing in Nightmare.");
            gameObject.SetActive(true);
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
        
        // Get the hand transform from the player
        
        _handTransform = PlayerManager.Instance.GetPlayerHandTransform();
        
        if (_handTransform == null)
        {
            DebugUtils.LogWarning("HAND transform not found on player!");
            return;
        }
        
        StoreOriginalTransform();
        SetPhysicsState(false);
        ParentToHand();
        StartPickupTransition();
        _currentlyHeldDrawing = this;
        
        //EventBroadcaster.Broadcast_GameStateChanged(Types.GameState.Inspecting);
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
        // Store the world scale
        Vector3 worldScale = transform.lossyScale;
    
        // Parent to hand (worldPositionStays: true keeps world position/rotation)
        transform.SetParent(_handTransform, true);
    
        // Force the scale to stay the same
        transform.localScale = worldScale;
    }
    
    private void StartPickupTransition()
    {
        _isPickedUp = true;
        _isTransitioningToHand = true;
    }
    
    private void HandlePickupTransition()
    {
        // Smoothly move to hand position
        transform.localPosition = Vector3.Lerp(
            transform.localPosition, 
            handOffset, 
            Time.deltaTime * pickupTransitionSpeed
        );
        
        // Smoothly rotate to hand orientation
        transform.localRotation = Quaternion.Slerp(
            transform.localRotation, 
            Quaternion.identity, 
            Time.deltaTime * pickupTransitionSpeed
        );
        
        // Check if transition is complete
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
        // Drop drawing with right click or ESC (returns to original position)
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.F))
        {
            DropDrawing();
        }
    }
    

    
    private void DropDrawing()
    {
        DebugUtils.Log($"Player dropped Drawing ID {drawingID}");
    
        ReturnToOriginalTransform();
        
        SceneManager.MoveGameObjectToScene(gameObject, SceneManager.GetActiveScene());

        
        SetPhysicsState(false);
        ResetPickupState();
        _currentlyHeldDrawing = null; // Add this line
    
        EventBroadcaster.Broadcast_GameStateChanged(Types.GameState.Gameplay);
    }
    
    private void ReturnToOriginalTransform()
    {
        transform.SetParent(_originalParent);
        transform.position = _originalPosition;
        transform.rotation = _originalRotation;
        transform.localScale = _originalScale;
    }
    
    
    private void ResetPickupState()
    {
        _isPickedUp = false;
        _isTransitioningToHand = false;
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
    private bool IsEmptySlot()
    {
        return drawingID == -1;
    }
    
    #endregion
}