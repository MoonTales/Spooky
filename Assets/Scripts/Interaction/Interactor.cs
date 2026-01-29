using System;
using UnityEngine;

public class Interactor : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float castDistance = 2f;
    [SerializeField] private KeyCode interactKey = KeyCode.F;

    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    
    private void Update()
    {
        if (playerCamera == null) return;

        Vector3 origin = playerCamera.transform.position;
        Vector3 dir = playerCamera.transform.forward;

        Debug.DrawRay(origin, dir * castDistance, Color.red);

        
        
        if (Physics.Raycast(origin, dir, out RaycastHit hitInfo, castDistance))
        {
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
            if (Input.GetKeyDown(interactKey))
            {
                if (interactable.CanInteract(this))
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

}
