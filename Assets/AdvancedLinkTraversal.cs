using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class AdvancedLinkTraversal : MonoBehaviour
{
    public Transform visualChild; // Assign the child model here
    private NavMeshAgent agent;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.autoTraverseOffMeshLink = false;
    }

    private bool isTraversing = false;

    void Update()
    {
        // ONLY start if we are on a link AND not already busy traversing one
        if (agent.isOnOffMeshLink && !isTraversing)
        {
            StartCoroutine(TraverseLinkSmoothly());
        }
    }

    IEnumerator TraverseLinkSmoothly()
    {
        isTraversing = true; // Lock the coroutine

        // 1. Setup Data
        OffMeshLinkData data = agent.currentOffMeshLinkData;
        Vector3 startPos = transform.position;
        Vector3 endPos = data.endPos + (Vector3.up * agent.baseOffset);

        // 2. Kill Agent Interference
        agent.updatePosition = false;
        agent.updateRotation = false;

        // 3. Find Surface Normal (Floor detection)
        Vector3 endNormal = Vector3.up;
        if (Physics.Raycast(data.endPos + Vector3.up * 1f, Vector3.down, out RaycastHit hit, 2f))
        {
            endNormal = hit.normal;
        }

        float duration = 0.8f;
        float elapsed = 0f;
        Quaternion startRot = visualChild.rotation;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float easeT = t * t * (3f - 2f * t);

            // Move Parent
            transform.position = Vector3.Lerp(startPos, endPos, easeT);

            // Rotate Child (Visuals)
            Vector3 currentUp = Vector3.Slerp(visualChild.up, endNormal, easeT);
            Vector3 moveDir = (endPos - startPos).normalized;
            Vector3 forward = Vector3.ProjectOnPlane(moveDir, currentUp);

            if (forward != Vector3.zero)
                visualChild.rotation = Quaternion.LookRotation(forward, currentUp);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 4. Cleanup
        transform.position = endPos;
        agent.CompleteOffMeshLink();

        // Important: Wait a frame for Unity to register the link is closed
        yield return new WaitForEndOfFrame();

        agent.updatePosition = true;
        agent.updateRotation = true;
        isTraversing = false; // Unlock
    }

}
