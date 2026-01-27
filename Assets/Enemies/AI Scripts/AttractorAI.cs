using System.Collections.Generic;
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
	[Header("Behaviours")]
	public EnemyState defaultState = EnemyState.Stand;
	private EnemyState currentState = EnemyState.Stand;
	private EnemyState nextState = EnemyState.Stand;
	[Tooltip("Leave blank to default focus on player")]
	public Transform defaultFocus;

	public enum AttractorType
	{
		visual,
		audio,
		attackRange,
		self
	}

	[System.Serializable]
	public class EnemyReactions
	{
		public AttractorType attractorType;
		[Tooltip("Inclusve")]
		public float minIntensity;
		[Tooltip("Non-inclusve")]
		public float maxIntensity;
		public List<EnemyState> stateRestriction = new List<EnemyState>();
		public EnemyState stateChange;
		//[Tooltip("Some states have 'buffers' that must complete before transitioning to another state. This is set to true so that those buffers are ignored" +
			//"when this behaviour is activated. Set to false if you want previous states to finish before transitioning to the new state")]
		//public bool immediateStateTransition = true;
		//[Tooltip("Forces the new state to finish its buffer before changing to any other states")]
		//public bool forceStateBuffer = false;
		//[Tooltip("Forces the new state to skip its buffer when changing to any other states")]
		//public bool forceSkipStateBuffer = false;
		[Tooltip("Set to true whenever the stateChange is a state that requires a target to focus on" +
			"and you want the enemy to focus on the relevant detected target. If this is false and the state requires a target," +
			"it will automatically target the defaultFocus/Player")]
		public bool targetDetectedObject = false;
		[Tooltip("When choosing an Attractor to focus on, the enemy will choose the Attractor nearest to it," +
			"instead of the Attractor with the highest intensity")]
		public bool prioritizeDistanceInsteadOfIntensity = false;
		[Tooltip("Enemy will focus on farthest Attractor or the Attractor with the lowest intensity")]
		public bool invertPriority = false;
	}

	public List<EnemyReactions> behaviourHierarchy;
	private bool forceCurrentStateBuffer = false;
	private bool awaitingStateWithForcedBuffer = false;
	private bool forceSkipCurrentStateBuffer = false;
	private bool awaitingStateWithSkippedBuffer = false;

	private Transform currentFocus;
	private Transform nextFocus;
	private Vector3 ghostPosition;
	private int currentStatePriority;
	private int nextStatePriority;
	private int lowestPriority;

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
		[Tooltip("Setting this to true makes this sense only work when in a space that is lit up, or if a light is nearby")]
		public bool lightSensitive = false;
	}

	private Collider[] hitColliders;

	public EnemySense[] senses;

	[Header("AnimationSetup")]
	[SerializeField] private Animator animator;
	[Tooltip("How fast is navmesh speed per walk animation speed, for syncing up animations")]
	[SerializeField] private float walkSpdAnimMult;
	[SerializeField] private float screamTime = 1;
	[SerializeField] private Collider attackBox;

	[Header("WanderState")]
	[SerializeField] private float wanderSpeed;
	[Tooltip("Maximum distance from current position that the enemy can choose to walk to")]
	[SerializeField] private float patrolRadius;
	[Tooltip("The shortest amount of time before the enemy decides to move to a new location")]
	[SerializeField] private float minPatrolTimer;
	[Tooltip("The longest amount of time before the enemy decides to move to a new location")]
	[SerializeField] private float maxPatrolTimer;

	private float patrolTimer;

	[Header("InvestigateState")]
	[SerializeField] private float investigateSpeed;
	[Tooltip("How long does the enemy continue to track the actual object's position while it is not being sensed before it targets its last known location")]
	[SerializeField] private float permanenceTime = 0;
	[Tooltip("How long does the enemy continue to stay in the investigate state while it is not sensing any objects")]
	[SerializeField] private float giveUpTime = 0;

	[Header("RushOverState")]
	[SerializeField] private float rushOverSpeed;
	[SerializeField] private float rushPermanenceTime = 0;
	[SerializeField] private float rushGiveUpTime = 0;
	[SerializeField] private bool screamBeforeRushOver = false;

	[Header("ChaseState")]
	[SerializeField] private float chaseSpeed;
	[SerializeField] private float chasePermanenceTime = 0;
	[SerializeField] private float chaseGiveUpTime = 0;
	[SerializeField] private bool screamBeforeChase = false;
	[SerializeField] private bool onlyScreamOnFirstChase = false;

	private bool aboutToRushScream = true;
	private bool aboutToChaseScream = true;
	private bool finishedScream = true;

	private float investigateTimer;

	[Header("AttackState")]
	[SerializeField] private float attackBufferTime = 0;
	[SerializeField] private float attackSpeed = 0;
	[SerializeField] private float attackTime = 0;
	[SerializeField] private float attackCooldownTime = 0;

	public EnemyState attackRevertState;
	public Transform attackRevertFocus;
	public int attackRevertPriority;

	private bool aboutToAttack = true;
	private bool finishedAttack = false;

	#endregion

	void Start()
	{
		if (defaultFocus == null)
			defaultFocus = Player.PlayerManager.Instance.GetPlayer().transform;
		currentFocus = defaultFocus;
		currentState = defaultState;
		nextState = defaultState;
		lowestPriority = behaviourHierarchy.Count;
		currentStatePriority = lowestPriority;
		agent = GetComponent<NavMeshAgent>();
		patrolTimer = Random.Range(minPatrolTimer, maxPatrolTimer);
	}

	Dictionary<AttractorType, List<Attractor>> DetectedAttractors()
	{
		Dictionary<AttractorType, List<Attractor>> tempAttractorDictionary = new Dictionary<AttractorType, List<Attractor>>();
		foreach (EnemySense sense in senses)
		{
			if (!sense.lightSensitive || Flashlight.Instance.IsFlashlightOn())
			{
				foreach (Attractor attractor in DetectTarget(sense, sense.invertDetection))
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

		if (!(currentState == EnemyState.RushOver))
		{
			aboutToRushScream = true;
		}
		if (!(currentState == EnemyState.Chase) && !onlyScreamOnFirstChase)
		{
			aboutToChaseScream = true;
		}

		Dictionary<AttractorType, List<Attractor>> tempDetectedAttractors = DetectedAttractors();

		bool tempCheck = false;
		int tempPriority = 0;
		foreach (EnemyReactions reaction in behaviourHierarchy)
		{
			if (reaction.stateRestriction.Count < 1 || reaction.stateRestriction.Contains(currentState))
			{
				Transform tempFocus = defaultFocus; ;
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
						nextFocus = defaultFocus;
					}
					else
					{
						nextFocus = tempFocus;
					}

					nextStatePriority = tempPriority;
					nextState = reaction.stateChange;
					if (nextStatePriority < currentStatePriority)
					{
						currentFocus = nextFocus;
						currentState = nextState;
						currentStatePriority = nextStatePriority;
					}

					tempCheck = true;
					break;
				}
			}
			tempPriority++;
		}

		if (!tempCheck)
		{
			nextFocus = defaultFocus;
			nextStatePriority = lowestPriority;
			nextState = defaultState;
		}

		// Check if the agent has reached its destination and is not calculating a new path
		if (currentState == EnemyState.Wander)
		{
			currentFocus = nextFocus;
			currentState = nextState;
			currentStatePriority = nextStatePriority;
			agent.speed = wanderSpeed;
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
			currentFocus = nextFocus;
			currentState = nextState;
			currentStatePriority = nextStatePriority;
			agent.speed = 0;
		}
		else if (currentState == EnemyState.Investigate)
		{
			if (currentState == nextState)
			{
				investigateTimer = 0;
			}
			else
			{
				investigateTimer += Time.deltaTime;
			}

			if (investigateTimer >= giveUpTime)
			{
				currentFocus = nextFocus;
				currentState = nextState;
				currentStatePriority = nextStatePriority;
			}

			if (investigateTimer <= permanenceTime)
			{
				ghostPosition = currentFocus.position;
			}

			
			agent.speed = investigateSpeed;
			if (Vector3.Distance(transform.position, ghostPosition) > 1)
				agent.SetDestination(ghostPosition);
		}
		else if (currentState == EnemyState.RushOver)
		{
			if (screamBeforeRushOver && aboutToRushScream)
			{
				aboutToRushScream = false;
				finishedScream = false;
				StartCoroutine(ScreamRoutine());
			}

			if (currentState == nextState)
			{
				investigateTimer = 0;
			}
			else
			{
				investigateTimer += Time.deltaTime;
			}

			if (investigateTimer >= rushGiveUpTime)
			{
				currentFocus = nextFocus;
				currentState = nextState;
				currentStatePriority = nextStatePriority;
			}

			if (investigateTimer <= rushPermanenceTime)
			{
				ghostPosition = currentFocus.position;
			}

			if (finishedScream)
			{
				agent.speed = rushOverSpeed;
				if (Vector3.Distance(transform.position, ghostPosition) > 1)
					agent.SetDestination(ghostPosition);
			}
		}
		else if (currentState == EnemyState.Chase)
		{
			if (screamBeforeChase && aboutToChaseScream)
			{
				aboutToChaseScream = false;
				finishedScream = false;
				StartCoroutine(ScreamRoutine());
			}

			if (currentState == nextState)
			{
				investigateTimer = 0;
			}
			else
			{
				investigateTimer += Time.deltaTime;
			}

			if (investigateTimer >= chaseGiveUpTime)
			{
				currentFocus = nextFocus;
				currentState = nextState;
				currentStatePriority = nextStatePriority;
			}

			if (investigateTimer <= chasePermanenceTime)
			{
				ghostPosition = currentFocus.position;
			}

			if (finishedScream)
			{
				agent.speed = chaseSpeed;
				if (Vector3.Distance(transform.position, ghostPosition) > 1)
					agent.SetDestination(ghostPosition);
			}
		}
		else if (currentState == EnemyState.Attack)
		{
			// all this unfinished or whatever
			if (aboutToAttack)
			{
				aboutToAttack = false;
				finishedAttack = false;
				StartCoroutine(AttackRoutine());
			}

			if (finishedAttack)
			{
				aboutToAttack = true;
				if (!(nextState == EnemyState.Attack))
				{
					nextFocus = attackRevertFocus == null ? defaultFocus : attackRevertFocus;
					nextState = attackRevertState;
					nextStatePriority = attackRevertPriority;
				}
				currentFocus = nextFocus;
				currentState = nextState;
				currentStatePriority = nextStatePriority;
			}
		}
	}

	IEnumerator ScreamRoutine()
	{
		animator.SetBool("Screaming", true);
		agent.speed = 0;
		yield return new WaitForSeconds(screamTime);
		animator.SetBool("Screaming", false);
		finishedScream = true;
	}

	IEnumerator AttackRoutine()
	{
		attackBox.enabled = true;
		animator.SetBool("Attacking", true);
		agent.speed = 0;
		yield return new WaitForSeconds(attackBufferTime);
		agent.speed = attackSpeed;
		yield return new WaitForSeconds(attackTime);
		agent.speed = 0;
		yield return new WaitForSeconds(attackCooldownTime);
		animator.SetBool("Attacking", false);
		finishedAttack = true;
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
