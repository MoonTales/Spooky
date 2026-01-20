using UnityEngine;

public class Interactor : MonoBehaviour
{
    [SerializeField] private float castDistance = 5f;
    [SerializeField] private Vector3 raycastOffset = new Vector3(0f, 0f, 0f);
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    private void Update()
    {
        Vector3 origin = transform.position + raycastOffset;
        Vector3 dir = transform.forward;
        Debug.DrawRay(origin, dir * castDistance, Color.red);

        if (Input.GetKeyDown(interactKey))
        {
            if (TryGetInteractable(out IInteractable interactable))
            {
                if (interactable.CanInteract(this))
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

    private bool TryGetInteractable(out IInteractable interactable)
    {
        interactable = null;

        Ray ray = new Ray(transform.position + raycastOffset, transform.forward);


        if (Physics.Raycast(ray, out RaycastHit hitInfo, castDistance))
        {

            interactable = hitInfo.collider.GetComponentInParent<IInteractable>();
            return interactable != null;
        }

        return false;
    }
}
