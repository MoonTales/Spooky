using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class AdvancedLinkTraversal : MonoBehaviour
{
    public float transitionSpeed = 2.0f;
    private NavMeshAgent agent;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        // Prevent the agent from forcing itself to stay upright (World Y-Up)
        agent.updateUpAxis = false;
    }

    void Update()
    {
        if (agent.isOnOffMeshLink)
        {
            StartCoroutine(TraverseWithNormalAlignment());
        }
    }

    IEnumerator TraverseWithNormalAlignment()
    {
        OffMeshLinkData data = agent.currentOffMeshLinkData;
        Vector3 startPos = transform.position;
        Vector3 endPos = data.endPos + Vector3.up * agent.baseOffset;

        // 1. Find the surface normal at the end point
        Vector3 targetNormal = Vector3.up;
        if (Physics.Raycast(endPos + Vector3.up * 1.0f, Vector3.down, out RaycastHit hit, 2.0f))
        {
            targetNormal = hit.normal;
        }

        float elapsed = 0;
        float duration = Vector3.Distance(startPos, endPos) / agent.speed;

        Quaternion startRot = transform.rotation;

        // 2. Calculate the end rotation (Face forward, align UP with surface normal)
        Vector3 forwardDir = (endPos - startPos).normalized;
        if (forwardDir == Vector3.zero) forwardDir = transform.forward;
        Quaternion endRot = Quaternion.LookRotation(forwardDir, targetNormal);

        while (elapsed < duration)
        {
            float t = elapsed / duration;

            transform.position = Vector3.Lerp(startPos, endPos, t);
            transform.rotation = Quaternion.Slerp(startRot, endRot, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = endPos;
        transform.rotation = endRot;
        agent.CompleteOffMeshLink();
    }
}
