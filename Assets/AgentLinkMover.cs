using UnityEngine;
using UnityEngine.AI;
using System.Collections;

// This script is provided by Unity Technologies!

public enum OffMeshLinkMoveMethod
{
    Teleport,
    NormalSpeed,
    Parabola,
    CustomJump,
    Curve
}

[RequireComponent(typeof(NavMeshAgent))]
public class AgentLinkMover : MonoBehaviour
{
    public OffMeshLinkMoveMethod m_Method = OffMeshLinkMoveMethod.Parabola;
    public AnimationCurve m_Curve = new AnimationCurve();

    public float distanceThreshold;
    public float maxPrepareTime;
    public float heightThreshold;

    IEnumerator Start()
    {
        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        agent.autoTraverseOffMeshLink = false;
        while (true)
        {
            if (agent.isOnOffMeshLink)
            {
                if (m_Method == OffMeshLinkMoveMethod.NormalSpeed)
                    yield return StartCoroutine(NormalSpeed(agent));
                else if (m_Method == OffMeshLinkMoveMethod.Parabola)
                    yield return StartCoroutine(Parabola(agent, 2.0f, 0.5f));
                else if (m_Method == OffMeshLinkMoveMethod.CustomJump)
                    yield return StartCoroutine(CustomJump(agent, -0.1f, 0.5f));
                else if (m_Method == OffMeshLinkMoveMethod.Curve)
                    yield return StartCoroutine(Curve(agent, 0.5f));
                agent.CompleteOffMeshLink();
            }
            yield return null;
        }
    }

    IEnumerator NormalSpeed(NavMeshAgent agent)
    {
        OffMeshLinkData data = agent.currentOffMeshLinkData;
        Vector3 endPos = data.endPos + Vector3.up * agent.baseOffset;
        while (agent.transform.position != endPos)
        {
            agent.transform.position = Vector3.MoveTowards(agent.transform.position, endPos, agent.speed * Time.deltaTime);
            yield return null;
        }
    }

    IEnumerator Parabola(NavMeshAgent agent, float height, float duration)
    {
        OffMeshLinkData data = agent.currentOffMeshLinkData;
        Vector3 startPos = agent.transform.position;
        Vector3 endPos = data.endPos + Vector3.up * agent.baseOffset;
        float normalizedTime = 0.0f;
        while (normalizedTime < 1.0f)
        {
            float yOffset = height * 4.0f * (normalizedTime - normalizedTime * normalizedTime);
            agent.transform.position = Vector3.Lerp(startPos, endPos, normalizedTime) + yOffset * Vector3.up;
            normalizedTime += Time.deltaTime / duration;
            yield return null;
        }
    }

    IEnumerator CustomJump(NavMeshAgent agent, float height, float duration)
    {
        OffMeshLinkData data = agent.currentOffMeshLinkData;
        Vector3 startPos = agent.transform.position;
        Vector3 endPos = data.endPos + Vector3.up * agent.baseOffset;
        float jumpDistance = Vector3.Distance(startPos, endPos);
        float previousRotationSpeed = agent.angularSpeed;
        agent.angularSpeed = 999;
        if (jumpDistance > distanceThreshold + agent.velocity.magnitude)
		{
            yield return new WaitForSeconds(Mathf.Min((jumpDistance - distanceThreshold) / 2, maxPrepareTime));
		}
        else
		{
            duration = (duration * 10f) / agent.velocity.magnitude;
		}
        float normalizedTime = 0.0f;
        agent.updateRotation = false;
        Vector3 endPoint = agent.currentOffMeshLinkData.endPos;
        Quaternion oldTarget = Quaternion.identity;

        while (normalizedTime < 1.0f)
        {
            Vector3 direction = (endPoint - transform.position).normalized;

            if (direction != Vector3.zero)
            {
                // Calculate target rotation and slerp towards it
                if (normalizedTime < 0.6f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    oldTarget = targetRotation;
                }
                transform.rotation = Quaternion.Slerp(transform.rotation, oldTarget, Time.deltaTime * agent.angularSpeed);
            }
            float yOffset = height * 4.0f * (normalizedTime - normalizedTime * normalizedTime);
            agent.transform.position = Vector3.Lerp(startPos, endPos, normalizedTime) + yOffset * Vector3.up;
            normalizedTime += Time.deltaTime / duration;
            yield return null;
        }
        agent.updateRotation = true;
        agent.angularSpeed = previousRotationSpeed;
    }

    IEnumerator Curve(NavMeshAgent agent, float duration)
    {
        OffMeshLinkData data = agent.currentOffMeshLinkData;
        Vector3 startPos = agent.transform.position;
        Vector3 endPos = data.endPos + Vector3.up * agent.baseOffset;
        float normalizedTime = 0.0f;
        while (normalizedTime < 1.0f)
        {
            float yOffset = m_Curve.Evaluate(normalizedTime);
            agent.transform.position = Vector3.Lerp(startPos, endPos, normalizedTime) + yOffset * Vector3.up;
            normalizedTime += Time.deltaTime / duration;
            yield return null;
        }
    }
}
