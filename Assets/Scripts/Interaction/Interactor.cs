using UnityEngine;

public class Interactor : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float castDistance = 5f;
    [SerializeField] private Vector3 raycastOffset = new Vector3(0f, 0f, 0f);
    [SerializeField] private KeyCode interactKey = KeyCode.E;

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

        if (Input.GetKeyDown(interactKey))
        {
            if (Physics.Raycast(origin, dir, out RaycastHit hitInfo, castDistance))
            {
                var interactable = hitInfo.collider.GetComponentInParent<IInteractable>();
                if (interactable != null && interactable.CanInteract(this))
                {
                    interactable.Interact(this);
                }
                else
                {
                    // play "denied" sound or show "locked" UI here.
                    // Debug.Log("Can't interact.");
                }
            }
        }
    }

}
