using System;
using Managers;
using Player;
using UnityEngine;
using Types = System.Types;

public class Interactor : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float castDistance = 2f;
    [SerializeField] private KeyCode interactKey = KeyCode.F;
    
    private int interactionLayerMask;
    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;
        
        interactionLayerMask = ~LayerMask.GetMask("SoundAttractor", "Ignore Raycast");
    }
    
    
    
    private void Update()
    {
        if (playerCamera == null) return;

        Vector3 origin = playerCamera.transform.position;
        Vector3 dir = playerCamera.transform.forward;

        Debug.DrawRay(origin, dir * castDistance, Color.red);

        // if we are not in the Gameplay state, we should not allow interaction
        if (GameStateManager.Instance.GetCurrentGameState() != Types.GameState.Gameplay)
        {
            EventBroadcaster.Broadcast_OnEndedHoverInteractable();
            return;
        }
        
        // I want my raycats to Ignore the SoundAttractor layer
        
        
        if (Physics.Raycast(origin, dir, out RaycastHit hitInfo, castDistance, interactionLayerMask))
        {
            // if we currently have an object we are inspecting, we should not allow any of this to happen
            if (InspectionSystem.Instance.GetCurrentInspectedObject() != null)
            {
                EventBroadcaster.Broadcast_OnEndedHoverInteractable();
                return;
            }
            
            // determine if the object is interactable
            var interactable = hitInfo.collider.GetComponentInParent<IInteractable>();
            if (interactable == null)
            {
                EventBroadcaster.Broadcast_OnEndedHoverInteractable();
                return;
            }
            // handle updating any HUD options
            if (interactable.CanInteract(this))
            {
                EventBroadcaster.Broadcast_OnBeganHoverInteractable(interactable);
            }
            if (Input.GetKeyDown(interactKey) )
            {
                if (interactable.CanInteract(this) && IsAllowedToInteract())
                {
                    interactable.Interact(this);
                }
            }

        }
        else
        {
            EventBroadcaster.Broadcast_OnEndedHoverInteractable();
        }
    }

    private bool IsAllowedToInteract()
    {
        // for now, we are assuming its always the player who is an interactor
        return PlayerController.Instance.IsGrounded();
    }
}
