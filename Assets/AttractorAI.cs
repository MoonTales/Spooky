using System.Collections;
using UnityEngine;
using Unity.AI.Navigation;
using UnityEngine.AI;

public class AttractorAI : MonoBehaviour
{
	#region InitialSetup
	private NavMeshAgent agent;
	#endregion

	#region States
	public enum EnemyState
	{
		Stand,
		Wander,
		Investigate,
		RushOver,
		Chase,
		Attack
	}
	#endregion

	#region InspectorVariables
	public EnemyState defaultState = EnemyState.Stand;

	[Header("WanderState")]
	public float WalkSpeed;
	public float patrolRadius;
	#endregion

	void Start()
	{
		agent = GetComponent<NavMeshAgent>();
		if (agent != null && defaultState == EnemyState.Wander)
		{
			SetNewRandomDestination();
		}
	}

	void Update()
	{
		// Check if the agent has reached its destination and is not calculating a new path
		if (defaultState == EnemyState.Wander && !agent.pathPending && agent.remainingDistance < 0.5f)
		{
			SetNewRandomDestination();
		}
	}

	private void SetNewRandomDestination()
	{
		Vector3 randomPoint = FindObjectOfType<NavMeshSurface>().transform.position + Random.insideUnitSphere * patrolRadius;
		NavMeshHit hit;

		// Sample the NavMesh to find the closest valid point within the specified range
		if (NavMesh.SamplePosition(randomPoint, out hit, patrolRadius, NavMesh.AllAreas))
		{
			agent.SetDestination(hit.position);
		}
	}
}
