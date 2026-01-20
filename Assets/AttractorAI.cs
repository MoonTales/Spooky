using System.Collections.Generic;
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
	[Header("Behaviours")]
	public EnemyState defaultState = EnemyState.Stand;
	private EnemyState currentState = EnemyState.Stand;

	public enum AttractorType
	{
		visual,
		audio
	}

	[System.Serializable]
	public class EnemyReactions
	{
		public AttractorType attractorType;
		[Tooltip("Inclusve")]
		public float minIntensity;
		[Tooltip("Non-inclusve")]
		public float maxIntensity;
		public List<EnemyState> stateRestriction;
		public EnemyState stateChange;
		[Tooltip("Set to true whenever the stateChange is a state that requires a target to focus on" +
			"and you want the enemy to focus on the relevant detected target. If this is false and the state requires a target," +
			"it will automatically target the player")]
		public bool targetDetectedObject = false;
		[Tooltip("When choosing an Attractor to focus on, the enemy will choose the Attractor nearest to it," +
			"instead of the Attractor with the highest intensity")]
		public bool prioritizeDistanceInsteadOfIntensity = false;
		[Tooltip("Enemy will focus on farthest Attractor or the Attractor with the lowest intensity")]
		public bool invertPriority = false;
	}

	public List<EnemyReactions> behaviourHierarchy;

	private Transform currentFocus;


	[System.Serializable]
	public class EnemySense
	{
		public float detectionRadius = 10f;
		public float detectionAngle = 360f;
		public LayerMask targetLayer;
		public LayerMask obstacleLayer;
		public Transform[] senseOrgans;
		[Tooltip("Attractors detected by this sense will instead be clasified as not detected, " +
			"while every Attractor in the targetLayer NOT detected by this sense is considered detected (as of right now, this does nothing)")]
		public bool invertDetection = false;  // finish this later
	}

	private Collider[] hitColliders;

	public EnemySense[] senses;

	[Header("AnimationSetup")]
	[SerializeField] private Animator animator;
	[Tooltip("How fast is navmesh speed per walk animation speed, for syncing up animations")]
	[SerializeField] private float walkSpdAnimMult;

	[Header("WanderState")]
	[SerializeField] private float walkSpeed;
	[Tooltip("Maximum distance from current position that the enemy can choose to walk to")]
	[SerializeField] private float patrolRadius;
	[Tooltip("The shortest amount of time before the enemy decides to move to a new location")]
	[SerializeField] private float minPatrolTimer;
	[Tooltip("The longest amount of time before the enemy decides to move to a new location")]
	[SerializeField] private float maxPatrolTimer;

	private float patrolTimer;
	
	#endregion

	void Start()
	{
		currentState = defaultState;
		agent = GetComponent<NavMeshAgent>();
		patrolTimer = Random.Range(minPatrolTimer, maxPatrolTimer);
	}

	Dictionary<AttractorType, List<Attractor>> DetectedAttractors()
	{
		Dictionary<AttractorType, List<Attractor>> tempAttractorDictionary = new Dictionary<AttractorType, List<Attractor>>();
		foreach (EnemySense sense in senses)
		{
			foreach(Attractor attractor in DetectTarget(sense, sense.invertDetection))
			{
				AttractorType tempAttractorType = attractor.GetComponent<Attractor>().attractorType;
				// Try to get the existing value; if it exists, add to it
				if (tempAttractorDictionary.ContainsKey(tempAttractorType))
				{
					tempAttractorDictionary[tempAttractorType].Add(attractor);
				}
				// If it doesn't exist, add the new key with the initial value
				else
				{
					tempAttractorDictionary.Add(tempAttractorType, new List<Attractor>());
					tempAttractorDictionary[tempAttractorType].Add(attractor);
				}
			}
		}
		return tempAttractorDictionary;
	}

	List<Attractor> DetectTarget(EnemySense currentSense, bool inverted)
	{
		List<Attractor> tempAttractorList = new List<Attractor>();
		// For efficiency, check for all targets in range before fireing any raycasts
		hitColliders = Physics.OverlapSphere(transform.position, currentSense.detectionRadius, currentSense.targetLayer);

		foreach (var hitCollider in hitColliders)
		{
			Transform target = hitCollider.transform;

			if (CheckConeVisibility(target, currentSense))
			{
				tempAttractorList.Add(target.GetComponent<Attractor>());
			}
		}
		return tempAttractorList;
	}

	bool CheckConeVisibility(Transform target, EnemySense currentSense)
	{
		foreach (Transform organ in currentSense.senseOrgans)
		{
			Vector3 directionToTarget = (target.position - organ.position).normalized;

			if (Vector3.Angle(organ.forward, directionToTarget) < currentSense.detectionAngle)
			{
				float distanceToTarget = Vector3.Distance(organ.position, target.position);
				RaycastHit hit;

				if (Physics.Raycast(organ.position, directionToTarget, out hit, distanceToTarget, currentSense.obstacleLayer | currentSense.targetLayer))
				{
					// Check if the hit object is the target itself (ignoring obstacles)
					if (((1 << hit.collider.gameObject.layer) & currentSense.targetLayer) != 0)
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.red;
		foreach (EnemySense sense in senses)
			foreach (Transform senseOrgan in sense.senseOrgans)
				DrawCone(senseOrgan, sense);
	}

	void DrawCone(Transform organ, EnemySense sense) // Just for viewing in the inspector
	{
		Vector3 forward = organ.forward * sense.detectionRadius;
		Vector3 right = Quaternion.Euler(0, sense.detectionAngle, 0) * forward;
		Vector3 left = Quaternion.Euler(0, -sense.detectionAngle, 0) * forward;

		Gizmos.DrawRay(organ.position, forward);
		Gizmos.DrawRay(organ.position, right);
		Gizmos.DrawRay(organ.position, left);
		Gizmos.DrawLine(organ.position + right, organ.position + left);
	}

	void Update()
	{
		animator.SetFloat("Speed", agent.velocity.magnitude / walkSpdAnimMult);  // this keeps the animation in sync with the enemy speed

		Dictionary<AttractorType, List<Attractor>> tempDetectedAttractors = DetectedAttractors();

		bool tempCheck = false;
		foreach (EnemyReactions reaction in behaviourHierarchy)
		{
			if (reaction.stateRestriction.Count < 1 || reaction.stateRestriction.Contains(currentState))
			{
				Transform tempFocus = Player.PlayerManager.Instance.GetPlayer().transform; ;
				float tempValue = -1;
				List<Attractor> tempAttractors = new List<Attractor>();
				if (tempDetectedAttractors.ContainsKey(reaction.attractorType))
				{
					foreach (Attractor attractor in tempDetectedAttractors[reaction.attractorType])
					{
						if (reaction.minIntensity <= attractor.intensity && attractor.intensity < reaction.maxIntensity)
						{
							if (reaction.targetDetectedObject && (tempValue < 0 || (reaction.prioritizeDistanceInsteadOfIntensity && ((reaction.invertPriority &&
								Vector3.Distance(transform.position, attractor.transform.position) > tempValue) || (!reaction.invertPriority &&
								Vector3.Distance(transform.position, attractor.transform.position) < tempValue))) || (!reaction.prioritizeDistanceInsteadOfIntensity
								&& ((reaction.invertPriority && attractor.intensity < tempValue) || (!reaction.invertPriority && attractor.intensity > tempValue)))))
							{
								tempFocus = attractor.transform;
							}

							tempAttractors.Add(attractor);
						}
					}
				}

				if (tempAttractors.Count > 0)
				{
					if (!reaction.targetDetectedObject)
					{
						currentFocus = Player.PlayerManager.Instance.GetPlayer().transform;
						currentState = reaction.stateChange;
					}

					else
					{
						currentFocus = tempFocus;
						currentState = reaction.stateChange;
					}

					tempCheck = true;
					break;
				}
			}
		}

		if (!tempCheck)
		{
			currentFocus = Player.PlayerManager.Instance.GetPlayer().transform;
			currentState = defaultState;
		}

		// Check if the agent has reached its destination and is not calculating a new path
		if (currentState == EnemyState.Wander)
		{
			agent.speed = walkSpeed;
			patrolTimer -= Time.deltaTime;
			if (!agent.pathPending && agent.remainingDistance < 0.5f)
				patrolTimer -= Time.deltaTime;
			if (patrolTimer <= 0)
			{
				patrolTimer = Random.Range(minPatrolTimer, maxPatrolTimer);
				SetNewRandomDestination();
			}
		}
		else if (currentState == EnemyState.Stand)
		{
			agent.speed = 0;
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
