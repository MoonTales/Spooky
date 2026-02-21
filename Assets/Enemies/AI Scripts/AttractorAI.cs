using System.Linq;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System.Reflection;
using Unity.AI.Navigation;
using UnityEngine.AI;

public class AttractorAI : MonoBehaviour
{
	#region InitialSetup
	private NavMeshAgent agent;

	[Tooltip("Between 0-100")]
	public float currentDangerLevel = 0;
	[SerializeField] private List<string> currentStatuses = new List<string>();
	#endregion

	#region States
	public enum EnemyState
	{
		Stand,
		Wander,
		Investigate,
		RushOver,
		Chase,
		Attack,
		Inspect,
		Search,
		Flee
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
		self,
		NONE
	}

	public enum FunctionType
	{
		TestFunction_floatF_boolB,
		ReprogramReaction_REPROGRAM,
		DeleteFocus,
		ChangeStats_STATS,
		AddStatuses_LISTstringStatuses_boolRemove
	}

	[System.Serializable]
	public class EnemyStatusListWrapper
	{
		public List<string> statusRestrictions;
	}

	[System.Serializable]
	public class EnemyStateListWrapper
	{
		public List<EnemyState> stateRestriction;
	}

	[System.Serializable]
	public class FunctionPickerListWrapper
	{
		public List<FunctionPicker> functionExecutions;
	}

	[System.Serializable]
	public class EnemyReactionReprogram
	{
		public AttractorType[] possibleAttractorTypeChanges;
		public float attractorTypeLowerBoundDangerRange = 100;
		public float attractorTypeUpperBoundDangerRange = 100;

		[Tooltip("Inclusve")]
		public float[] possibleMinIntensityChanges;
		public float minIntensityLowerBoundDangerRange = 100;
		public float minIntensityUpperBoundDangerRange = 100;

		[Tooltip("Non-inclusve")]
		public float[] possibleMaxIntensityChanges;
		public float maxIntensityLowerBoundDangerRange = 100;
		public float maxIntensityUpperBoundDangerRange = 100;

		[SerializeField] public EnemyStatusListWrapper[] possibleStatusRestrictionsChanges;
		public float statusRestrictionsLowerBoundDangerRange = 100;
		public float statusRestrictionsUpperBoundDangerRange = 100;

		public bool[] possibleAllStatusesRequiredChanges;
		public float allStatusesRequiredLowerBoundDangerRange = 100;
		public float allStatusesRequiredUpperBoundDangerRange = 100;

		[SerializeField] public EnemyStateListWrapper[] possibleStateRestrictionChanges;
		public float stateRestrictionLowerBoundDangerRange = 100;
		public float stateRestrictionUpperBoundDangerRange = 100;

		[SerializeField] public FunctionPickerListWrapper[] possibleFunctionExecutionsChanges;
		public float functionExecutionsLowerBoundDangerRange = 100;
		public float functionExecutionsUpperBoundDangerRange = 100;

		public EnemyState[] possibleStateChangeChanges;
		public float stateChangeLowerBoundDangerRange = 100;
		public float stateChangeUpperBoundDangerRange = 100;

		[Tooltip("Set to true whenever the stateChange is a state that requires a target to focus on" +
			"and you want the enemy to focus on the relevant detected target. If this is false and the state requires a target," +
			"it will automatically target the defaultFocus/Player")]
		public bool[] possibleTargetDetectedObjectChanges;
		public float targetDetectedObjectLowerBoundDangerRange = 100;
		public float targetDetectedObjectUpperBoundDangerRange = 100;

		[Tooltip("When choosing an Attractor to focus on, the enemy will choose the Attractor nearest to it," +
			"instead of the Attractor with the highest intensity")]
		public bool[] possiblePrioritizeDistanceInsteadOfIntensityChanges;
		public float prioritizeDistanceInsteadOfIntensityLowerBoundDangerRange = 100;
		public float prioritizeDistanceInsteadOfIntensityUpperBoundDangerRange = 100;

		[Tooltip("Enemy will focus on farthest Attractor or the Attractor with the lowest intensity")]
		public bool[] possibleInvertPriorityChanges;
		public float invertPriorityLowerBoundDangerRange = 100;
		public float invertPriorityUpperBoundDangerRange = 100;
	}

	[System.Serializable]
	public class FunctionPicker
	{
		public FunctionType function;
		[Header("Note: use spaces to seperate items in LISTs")]
		public List<string> arguments;
		[Tooltip("This is only for functions that reprogram reactions in the behaviour hierarchy. The array contains possible reactions to reprogram  based on" +
			"their index in the list")]
		public int[] possiblePriorityReprograms;
		[Tooltip("Only useful for functions that reprogram a reaction")]
		public EnemyReactionReprogram reprogramParameters;
		[Header("StatChanges")]
		public Stats[] statsToChange;
		public Alteration changeStatsBy;
		public float statsChangeAmount;
	}

	public void TestFunction(List<string> arguments)
	{
		float F = float.Parse(arguments[0]);
		bool B = bool.Parse(arguments[1]);

		if (B)
		{
			Debug.Log($"Oh wow {F}");
		}
	}
	public void DeleteFocus()
	{
		if (nextFocus != Player.PlayerManager.Instance.GetPlayer().transform)
		{
			nextFocus.gameObject.SetActive(false);
		}
	}

	public void AddStatuses(List<string> arguments)
	{
		List<string> Statuses = arguments[0].Split(' ', System.StringSplitOptions.RemoveEmptyEntries).ToList();
		bool Remove = bool.Parse(arguments[1]);

		if (Remove)
		{
			HashSet<string> valuesToRemove = new HashSet<string>(Statuses);
			currentStatuses.RemoveAll(item => valuesToRemove.Contains(item));
		}
		else
		{
			IEnumerable<string> itemsToAdd = Statuses.Except(currentStatuses);
			currentStatuses.AddRange(itemsToAdd);
		}
	}

	public enum Stats
	{
		dangerLevel,
		screamTime,

		//Wander
		wander_speed,
		wander_patrolRadius,
		wander_minPatrolTimer,
		wander_maxPatrolTimer,

		//Investigate
		investigate_speed,
		investigate_permanenceTime,
		investigate_giveUpTime,

		//Rush Over
		rushOver_speed,
		rushOver_permanenceTime,
		rushOver_giveUpTime,

		//Chase
		chase_speed,
		chase_permanenceTime,
		chase_giveUpTime,

		//Attack
		attack_bufferTime,
		attack_speed,
		attack_time,
		attack_cooldownTime,

		//Inspect
		inspect_time,

		//Search
		search_speed,
		search_radius,
		search_minTimer,
		search_maxTimer,
		search_hideRadius,
		search_hideTargetAvoidanceRange,

		//Flee
		flee_speed,
		flee_minDistance,
		flee_maxDistance,
		flee_minTime,
		flee_maxTime,
		flee_targetAvoidanceRange
	}
	public enum Alteration
	{
		plus,
		minus,
		times,
		dividedBy,
		equals
	}
	public void ChangeStats(Stats[] statsToChange, Alteration changeBy, float changeAmount)
	{
		foreach (Stats stat in statsToChange)
		{
			switch (stat)
			{
				case Stats.dangerLevel:
					currentDangerLevel = DoAlterationCalculation(currentDangerLevel, changeBy, changeAmount);
					break;
				case Stats.screamTime:
					screamTime = DoAlterationCalculation(screamTime, changeBy, changeAmount);
					break;

				//Wander
				case Stats.wander_speed:
					wanderSpeed = DoAlterationCalculation(wanderSpeed, changeBy, changeAmount);
					break;
				case Stats.wander_patrolRadius:
					patrolRadius = DoAlterationCalculation(patrolRadius, changeBy, changeAmount);
					break;
				case Stats.wander_minPatrolTimer:
					minPatrolTimer = DoAlterationCalculation(minPatrolTimer, changeBy, changeAmount);
					break;
				case Stats.wander_maxPatrolTimer:
					maxPatrolTimer = DoAlterationCalculation(maxPatrolTimer, changeBy, changeAmount);
					break;

				//Investigate
				case Stats.investigate_speed:
					investigateSpeed = DoAlterationCalculation(investigateSpeed, changeBy, changeAmount);
					break;
				case Stats.investigate_permanenceTime:
					permanenceTime = DoAlterationCalculation(permanenceTime, changeBy, changeAmount);
					break;
				case Stats.investigate_giveUpTime:
					giveUpTime = DoAlterationCalculation(giveUpTime, changeBy, changeAmount);
					break;

				//Rush Over
				case Stats.rushOver_speed:
					rushOverSpeed = DoAlterationCalculation(rushOverSpeed, changeBy, changeAmount);
					break;
				case Stats.rushOver_permanenceTime:
					rushPermanenceTime = DoAlterationCalculation(rushPermanenceTime, changeBy, changeAmount);
					break;
				case Stats.rushOver_giveUpTime:
					rushGiveUpTime = DoAlterationCalculation(rushGiveUpTime, changeBy, changeAmount);
					break;

				//Chase
				case Stats.chase_speed:
					chaseSpeed = DoAlterationCalculation(chaseSpeed, changeBy, changeAmount);
					break;
				case Stats.chase_permanenceTime:
					chasePermanenceTime = DoAlterationCalculation(chasePermanenceTime, changeBy, changeAmount);
					break;
				case Stats.chase_giveUpTime:
					chaseGiveUpTime = DoAlterationCalculation(chaseGiveUpTime, changeBy, changeAmount);
					break;

				//Attack
				case Stats.attack_bufferTime:
					attackBufferTime = DoAlterationCalculation(attackBufferTime, changeBy, changeAmount);
					break;
				case Stats.attack_speed:
					attackSpeed = DoAlterationCalculation(attackSpeed, changeBy, changeAmount);
					break;
				case Stats.attack_time:
					attackTime = DoAlterationCalculation(attackTime, changeBy, changeAmount);
					break;
				case Stats.attack_cooldownTime:
					attackCooldownTime = DoAlterationCalculation(attackCooldownTime, changeBy, changeAmount);
					break;

				//Inspect
				case Stats.inspect_time:
					inspectTime = DoAlterationCalculation(inspectTime, changeBy, changeAmount);
					break;

				//Search
				case Stats.search_speed:
					searchSpeed = DoAlterationCalculation(searchSpeed, changeBy, changeAmount);
					break;
				case Stats.search_radius:
					searchRadius = DoAlterationCalculation(searchRadius, changeBy, changeAmount);
					break;
				case Stats.search_minTimer:
					minSearchTimer = DoAlterationCalculation(minSearchTimer, changeBy, changeAmount);
					break;
				case Stats.search_maxTimer:
					maxSearchTimer = DoAlterationCalculation(maxSearchTimer, changeBy, changeAmount);
					break;
				case Stats.search_hideRadius:
					hideRadius = DoAlterationCalculation(hideRadius, changeBy, changeAmount);
					break;
				case Stats.search_hideTargetAvoidanceRange:
					hideTargetAvoidanceRange = DoAlterationCalculation(hideTargetAvoidanceRange, changeBy, changeAmount);
					break;

				//Flee
				case Stats.flee_speed:
					fleeSpeed = DoAlterationCalculation(fleeSpeed, changeBy, changeAmount);
					break;
				case Stats.flee_minDistance:
					minFleeDistance = DoAlterationCalculation(minFleeDistance, changeBy, changeAmount);
					break;
				case Stats.flee_maxDistance:
					maxFleeDistance = DoAlterationCalculation(maxFleeDistance, changeBy, changeAmount);
					break;
				case Stats.flee_minTime:
					minFleeTime = DoAlterationCalculation(minFleeTime, changeBy, changeAmount);
					break;
				case Stats.flee_maxTime:
					maxFleeTime = DoAlterationCalculation(maxFleeTime, changeBy, changeAmount);
					break;
				case Stats.flee_targetAvoidanceRange:
					fleeTargetAvoidanceRange = DoAlterationCalculation(fleeTargetAvoidanceRange, changeBy, changeAmount);
					break;
			}
		}
	}
	public float DoAlterationCalculation(float valueToChange, Alteration changeBy, float changeAmount)
	{
		switch (changeBy)
		{
			case Alteration.plus:
				return Mathf.Max(valueToChange + changeAmount, 0);
			case Alteration.minus:
				return Mathf.Max(valueToChange - changeAmount, 0);
			case Alteration.times:
				return Mathf.Max(valueToChange * changeAmount, 0);
			case Alteration.dividedBy:
				return changeAmount == 0 ? 0 : Mathf.Max(valueToChange / changeAmount, 0);
			case Alteration.equals:
				return Mathf.Max(changeAmount, 0);
			default:
				return valueToChange;
		}
	}

	[System.Serializable]
	public class BoolCondition
	{
		public string boolName;
		public bool boolValue;
	}
	[System.Serializable]
	public class FloatCondition
	{
		public string floatName;
		public float floatValue;
	}
	[System.Serializable]
	public class IntCondition
	{
		public string intName;
		public int intValue;
	}
	[System.Serializable]
	public class ReactConditions
	{
		public List<BoolCondition> boolConditions = new List<BoolCondition>();
		public List<FloatCondition> floatConditions = new List<FloatCondition>();
		public List<IntCondition> intConditions = new List<IntCondition>();
	}

	public ReactConditions currentConditions;

	[System.Serializable]
	public class EnemyReactions
	{
		public AttractorType attractorType;
		[Tooltip("Inclusve")]
		public float minIntensity;
		[Tooltip("Non-inclusve")]
		public float maxIntensity;
		public ReactConditions reactionConditions;
		public bool allConditionsRequired = false;
		[Tooltip("This behavior will only be activated if any of the enemy's current statuses match up with any in this list. If this list is empty, then this" +
			"behavior can be activated regardless of the enemy's current statuses.")]
		public List<string> statusRestrictions = new List<string>();
		[Tooltip("If this is set to true, the above rule changes from any of the listed status to all of the listed statuses being required.")]
		public bool allStatusesRequired = false;
		[Tooltip("This behavior will only be activated if the current state of the enemy is one of these states. If this list is empty, then this behavior can" +
			"be activated regardless of the enemy's current state.")]
		public List<EnemyState> stateRestriction = new List<EnemyState>();
		[SerializeField] public List<FunctionPicker> functionExecutions = new List<FunctionPicker>();
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

	public class ThoughtProcess
	{
		public AttractorType attractorType;
		[Tooltip("Inclusve")]
		public float minIntensity;
		[Tooltip("Non-inclusve")]
		public float maxIntensity;
		public ReactConditions reactionConditions;
		public bool allConditionsRequired = false;
		[Tooltip("This behavior will only be activated if any of the enemy's current statuses match up with any in this list. If this list is empty, then this" +
			"behavior can be activated regardless of the enemy's current statuses.")]
		public List<string> statusRestrictions = new List<string>();
		[Tooltip("If this is set to true, the above rule changes from any of the listed status to all of the listed statuses being required.")]
		public bool allStatusesRequired = false;
		[Tooltip("This behavior will only be activated if the current state of the enemy is one of these states. If this list is empty, then this behavior can" +
			"be activated regardless of the enemy's current state.")]
		public List<EnemyState> stateRestriction = new List<EnemyState>();
		[SerializeField] public List<FunctionPicker> functionExecutions = new List<FunctionPicker>();
	}

	public List<EnemyReactions> behaviourHierarchy;
	public List<ThoughtProcess> Thoughts;
	// <RR> REFACTOR REMOVED
	// private bool forceCurrentStateBuffer = false;
	// private bool awaitingStateWithForcedBuffer = false;
	// private bool forceSkipCurrentStateBuffer = false;
	// private bool awaitingStateWithSkippedBuffer = false;
	// <RR> REFACTOR REMOVED END

	private Transform currentFocus;
	private Transform nextFocus;
	private Vector3 ghostPosition;
	private int currentStatePriority;
	private int nextStatePriority;
	private int lowestPriority;
	
	public void ReprogramReaction(int[] priority, EnemyReactionReprogram reactionEdits)
	{
		if (priority.Length <= 0)
		{
			return;
		}

		int chosenPriority = priority[Random.Range(0, priority.Length)];

		Debug.Log("Priority length is " + priority.Length + " and behavior hierarchy count is " + behaviourHierarchy.Count);

		if (priority.Length <= behaviourHierarchy.Count)
		{
			EnemyReactions chosenReaction = behaviourHierarchy[chosenPriority];

			float tempLowerBound = 0;
			float tempUpperBound = 100;

			if (reactionEdits.possibleAttractorTypeChanges.Length > 0)
			{
				tempLowerBound = Mathf.Clamp(currentDangerLevel - reactionEdits.attractorTypeLowerBoundDangerRange, 0, 100) / 100;
				tempUpperBound = Mathf.Clamp(currentDangerLevel + reactionEdits.attractorTypeUpperBoundDangerRange, 0, 100) / 100;

				chosenReaction.attractorType = reactionEdits.possibleAttractorTypeChanges[Random.Range((int)(reactionEdits.possibleAttractorTypeChanges.Length *
					tempLowerBound), Mathf.CeilToInt(reactionEdits.possibleAttractorTypeChanges.Length * tempUpperBound))];
			}
			if (reactionEdits.possibleMinIntensityChanges.Length > 0)
			{
				tempLowerBound = Mathf.Clamp(currentDangerLevel - reactionEdits.minIntensityLowerBoundDangerRange, 0, 100) / 100;
				tempUpperBound = Mathf.Clamp(currentDangerLevel + reactionEdits.minIntensityUpperBoundDangerRange, 0, 100) / 100;

				chosenReaction.minIntensity = reactionEdits.possibleMinIntensityChanges[Random.Range((int)(reactionEdits.possibleMinIntensityChanges.Length *
					tempLowerBound), Mathf.CeilToInt(reactionEdits.possibleMinIntensityChanges.Length * tempUpperBound))];
			}
			if (reactionEdits.possibleMaxIntensityChanges.Length > 0)
			{
				tempLowerBound = Mathf.Clamp(currentDangerLevel - reactionEdits.maxIntensityLowerBoundDangerRange, 0, 100) / 100;
				tempUpperBound = Mathf.Clamp(currentDangerLevel + reactionEdits.maxIntensityUpperBoundDangerRange, 0, 100) / 100;

				chosenReaction.maxIntensity = reactionEdits.possibleMaxIntensityChanges[Random.Range((int)(reactionEdits.possibleMaxIntensityChanges.Length *
					tempLowerBound), Mathf.CeilToInt(reactionEdits.possibleMaxIntensityChanges.Length * tempUpperBound))];
			}
			if (reactionEdits.possibleStatusRestrictionsChanges.Length > 0)
			{
				tempLowerBound = Mathf.Clamp(currentDangerLevel - reactionEdits.statusRestrictionsLowerBoundDangerRange, 0, 100) / 100;
				tempUpperBound = Mathf.Clamp(currentDangerLevel + reactionEdits.statusRestrictionsUpperBoundDangerRange, 0, 100) / 100;

				chosenReaction.statusRestrictions = reactionEdits.possibleStatusRestrictionsChanges[Random.Range((int)(
					reactionEdits.possibleStatusRestrictionsChanges.Length * tempLowerBound), Mathf.CeilToInt(
						reactionEdits.possibleStatusRestrictionsChanges.Length * tempUpperBound))].statusRestrictions;
			}
			if (reactionEdits.possibleAllStatusesRequiredChanges.Length > 0)
			{
				tempLowerBound = Mathf.Clamp(currentDangerLevel - reactionEdits.allStatusesRequiredLowerBoundDangerRange, 0, 100) / 100;
				tempUpperBound = Mathf.Clamp(currentDangerLevel + reactionEdits.allStatusesRequiredUpperBoundDangerRange, 0, 100) / 100;

				chosenReaction.allStatusesRequired = reactionEdits.possibleAllStatusesRequiredChanges[Random.Range((int)(
					reactionEdits.possibleAllStatusesRequiredChanges.Length * tempLowerBound), Mathf.CeilToInt(
						reactionEdits.possibleAllStatusesRequiredChanges.Length * tempUpperBound))];
			}
			if (reactionEdits.possibleStateRestrictionChanges.Length > 0)
			{
				tempLowerBound = Mathf.Clamp(currentDangerLevel - reactionEdits.stateRestrictionLowerBoundDangerRange, 0, 100) / 100;
				tempUpperBound = Mathf.Clamp(currentDangerLevel + reactionEdits.stateRestrictionUpperBoundDangerRange, 0, 100) / 100;

				chosenReaction.stateRestriction = reactionEdits.possibleStateRestrictionChanges[Random.Range((int)(
					reactionEdits.possibleStateRestrictionChanges.Length * tempLowerBound), Mathf.CeilToInt(reactionEdits.possibleStateRestrictionChanges.Length *
					tempUpperBound))].stateRestriction;
			}
			if (reactionEdits.possibleFunctionExecutionsChanges.Length > 0)
			{
				tempLowerBound = Mathf.Clamp(currentDangerLevel - reactionEdits.functionExecutionsLowerBoundDangerRange, 0, 100) / 100;
				tempUpperBound = Mathf.Clamp(currentDangerLevel + reactionEdits.functionExecutionsUpperBoundDangerRange, 0, 100) / 100;

				chosenReaction.functionExecutions = reactionEdits.possibleFunctionExecutionsChanges[Random.Range((int)(
					reactionEdits.possibleFunctionExecutionsChanges.Length * tempLowerBound), Mathf.CeilToInt(
						reactionEdits.possibleFunctionExecutionsChanges.Length * tempUpperBound))].functionExecutions;
			}
			if (reactionEdits.possibleStateChangeChanges.Length > 0)
			{
				tempLowerBound = Mathf.Clamp(currentDangerLevel - reactionEdits.stateChangeLowerBoundDangerRange, 0, 100) / 100;
				tempUpperBound = Mathf.Clamp(currentDangerLevel + reactionEdits.stateChangeUpperBoundDangerRange, 0, 100) / 100;

				chosenReaction.stateChange = reactionEdits.possibleStateChangeChanges[Random.Range((int)(reactionEdits.possibleStateChangeChanges.Length *
					tempLowerBound), Mathf.CeilToInt(reactionEdits.possibleStateChangeChanges.Length * tempUpperBound))];
			}
			if (reactionEdits.possibleTargetDetectedObjectChanges.Length > 0)
			{
				tempLowerBound = Mathf.Clamp(currentDangerLevel - reactionEdits.targetDetectedObjectLowerBoundDangerRange, 0, 100) / 100;
				tempUpperBound = Mathf.Clamp(currentDangerLevel + reactionEdits.targetDetectedObjectUpperBoundDangerRange, 0, 100) / 100;

				chosenReaction.targetDetectedObject = reactionEdits.possibleTargetDetectedObjectChanges[Random.Range((int)(
					reactionEdits.possibleTargetDetectedObjectChanges.Length * tempLowerBound), Mathf.CeilToInt(
						reactionEdits.possibleTargetDetectedObjectChanges.Length * tempUpperBound))];
			}
			if (reactionEdits.possiblePrioritizeDistanceInsteadOfIntensityChanges.Length > 0)
			{
				tempLowerBound = Mathf.Clamp(currentDangerLevel - reactionEdits.prioritizeDistanceInsteadOfIntensityLowerBoundDangerRange, 0, 100) / 100;
				tempUpperBound = Mathf.Clamp(currentDangerLevel + reactionEdits.prioritizeDistanceInsteadOfIntensityUpperBoundDangerRange, 0, 100) / 100;

				chosenReaction.prioritizeDistanceInsteadOfIntensity = reactionEdits.possiblePrioritizeDistanceInsteadOfIntensityChanges[Random.Range((int)(
					reactionEdits.possiblePrioritizeDistanceInsteadOfIntensityChanges.Length * tempLowerBound), Mathf.CeilToInt(
						reactionEdits.possiblePrioritizeDistanceInsteadOfIntensityChanges.Length * tempUpperBound))];
			}
			if (reactionEdits.possibleInvertPriorityChanges.Length > 0)
			{
				tempLowerBound = Mathf.Clamp(currentDangerLevel - reactionEdits.invertPriorityLowerBoundDangerRange, 0, 100) / 100;
				tempUpperBound = Mathf.Clamp(currentDangerLevel + reactionEdits.invertPriorityUpperBoundDangerRange, 0, 100) / 100;

				chosenReaction.invertPriority = reactionEdits.possibleInvertPriorityChanges[Random.Range((int)(reactionEdits.possibleInvertPriorityChanges.Length *
					tempLowerBound), Mathf.CeilToInt(reactionEdits.possibleInvertPriorityChanges.Length * tempUpperBound))];
			}
		}
	}

	[System.Serializable]
	public class EnemySense
	{
		public float detectionRadius = 10f;
		public float detectionAngle = 360f;
		public LayerMask targetLayer;
		public LayerMask obstacleLayer;
		public Transform[] senseOrgans;
		public bool addPlayerCameraAsSenseOrgan = false;
		[Tooltip("Attractors detected by this sense will instead be clasified as not detected, " +
			"while every Attractor in the targetLayer NOT detected by this sense is considered detected (as of right now, this does nothing)")]
		public bool invertDetection = false;  // finish this later
		[Tooltip("Setting this to true makes this sense only work when in a space that is lit up, or if a light is nearby")]
		public bool lightSensitive = false;
	}

	private Collider[] hitColliders;

	public EnemySense[] senses;

	[Header("Animation Setup")]
	[SerializeField] private Animator animator;
	[Tooltip("How fast is navmesh speed per walk animation speed, for syncing up animations")]
	[SerializeField] private float walkSpdAnimMult;
	[SerializeField] private float screamTime = 1;
	[SerializeField] private Collider attackBox;

	[Header("Wander State")]
	[SerializeField] private float wanderSpeed;
	[Tooltip("Maximum distance from current position that the enemy can choose to walk to")]
	[SerializeField] private float patrolRadius;
	[Tooltip("The shortest amount of time before the enemy decides to move to a new location")]
	[SerializeField] private float minPatrolTimer;
	[Tooltip("The longest amount of time before the enemy decides to move to a new location")]
	[SerializeField] private float maxPatrolTimer;

	private float patrolTimer;

	[Header("Investigate State")]
	[SerializeField] private float investigateSpeed;
	[Tooltip("How long does the enemy continue to track the actual object's position while it is not being sensed before it targets its last known location")]
	[SerializeField] private float permanenceTime = 0;
	[Tooltip("How long does the enemy continue to stay in the investigate state while it is not sensing any objects")]
	[SerializeField] private float giveUpTime = 0;

	[Header("Rush Over State")]
	[SerializeField] private float rushOverSpeed;
	[SerializeField] private float rushPermanenceTime = 0;
	[SerializeField] private float rushGiveUpTime = 0;
	[SerializeField] private bool screamBeforeRushOver = false;

	[Header("Chase State")]
	[SerializeField] private float chaseSpeed;
	[SerializeField] private float chasePermanenceTime = 0;
	[SerializeField] private float chaseGiveUpTime = 0;
	[SerializeField] private bool screamBeforeChase = false;
	[SerializeField] private bool onlyScreamOnFirstChase = false;

	private bool aboutToRushScream = true;
	private bool aboutToChaseScream = true;
	private bool finishedScream = true;

	private float investigateTimer;

	[Header("Attack State")]
	[SerializeField] private float attackBufferTime = 0;
	[SerializeField] private float attackSpeed = 0;
	[SerializeField] private float attackTime = 0;
	[SerializeField] private float attackCooldownTime = 0;

	public EnemyState attackRevertState;
	public Transform attackRevertFocus;
	public int attackRevertPriority;

	private bool aboutToAttack = true;
	private bool finishedAttack = false;

	[Header("Inspect State")]
	[SerializeField] private float inspectTime = 0;
	private float currentInspect = 0;

	[Header("Search State")]
	[SerializeField] private float searchSpeed;
	[Tooltip("Maximum distance from current position that the enemy can choose to search")]
	[SerializeField] private float searchRadius;
	[Tooltip("The shortest amount of time before the enemy decides to move to stop searching")]
	[SerializeField] private float minSearchTimer;
	[Tooltip("The longest amount of time before the enemy decides to move to a new location")]
	[SerializeField] private float maxSearchTimer;
	[Tooltip("The minimum amount of places the enemy could search")]
	[SerializeField] private int minSearchAmount;
	[Tooltip("The maximum amount of places the enemy could search")]
	[SerializeField] private int maxSearchAmount;
	[Tooltip("The enemy will only search areas that cannot be detected by the sense at this index in Enemy Senses")]
	[SerializeField] private int searchSenseIndex;
	[SerializeField] private bool screamInsteadOfLookAround = false;
	[SerializeField] private bool dontScreamOrLookAround = false;
	[SerializeField] private bool hide = false;
	[SerializeField] private float hideRadius;
	[SerializeField] private bool avoidHideTarget = false;
	[SerializeField] private float hideTargetAvoidanceRange;

	private float searchTimer;
	private int searchAmount;
	private List<Vector3> searchLocations = new List<Vector3>();
	private bool searching = false;
	private bool searchingSpot = false;
	private bool hiding = false;
	private bool hiddenStationary = false;

	[Header("Flee State")]
	[SerializeField] private float fleeSpeed;
	[SerializeField] private float minFleeDistance;
	[SerializeField] private float maxFleeDistance;
	[SerializeField] private float minFleeTime;
	[SerializeField] private float maxFleeTime;
	[SerializeField] private bool avoidTarget = false;
	[SerializeField] private float fleeTargetAvoidanceRange;

	private float fleeTime;
	private bool fleeing = false;
	private NavMeshObstacle currentAvoidedTarget;

	#endregion

	public void AddCameraSenses()
	{
		foreach (EnemySense sense in senses)
		{
			if (sense.addPlayerCameraAsSenseOrgan)
			{
				sense.senseOrgans.Append(Flashlight.Instance.GetComponentInParent<Transform>());
			}
		}
	}

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

		AddCameraSenses();
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

			if (CheckConeVisibility(target.position, currentSense))
			{
				tempAttractorList.Add(target.GetComponent<Attractor>());
			}
		}
		return tempAttractorList;
	}

	bool CheckConeVisibility(Vector3 targetPosition, EnemySense currentSense)
	{
		foreach (Transform organ in currentSense.senseOrgans)
		{
			Vector3 directionToTarget = (targetPosition - organ.position).normalized;

			if (Vector3.Angle(organ.forward, directionToTarget) < currentSense.detectionAngle)
			{
				float distanceToTarget = Vector3.Distance(organ.position, targetPosition);
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

	void HandleFunctionCalling(FunctionPicker function)
	{
		switch (function.function)
		{
			case FunctionType.TestFunction_floatF_boolB:
				TestFunction(function.arguments);
				break;
			case FunctionType.ReprogramReaction_REPROGRAM:
				ReprogramReaction(function.possiblePriorityReprograms, function.reprogramParameters);
				break;
			case FunctionType.DeleteFocus:
				DeleteFocus();
				break;
			case FunctionType.ChangeStats_STATS:
				ChangeStats(function.statsToChange, function.changeStatsBy, function.statsChangeAmount);
				break;
			case FunctionType.AddStatuses_LISTstringStatuses_boolRemove:
				AddStatuses(function.arguments);
				break;
		}
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
		if (!(currentState == EnemyState.Inspect))
		{
			animator.SetBool("Inspecting", false);
			currentInspect = 0;
		}
		if (!(currentState == EnemyState.Search))
		{
			animator.SetBool("LookingAround", false);
			searchTimer = 0;
			if (searchLocations.Count > 1)
				searchLocations.Clear();
			searching = false;
			hiding = false;
			hiddenStationary = false;
		}
		if (!(currentState == EnemyState.Flee))
		{
			fleeTime = 0;
			fleeing = false;
		}
		if (!(currentState == EnemyState.Search || currentState == EnemyState.Flee))
		{
			if (currentAvoidedTarget != null)
				currentAvoidedTarget.enabled = false;
		}

		Dictionary<AttractorType, List<Attractor>> tempDetectedAttractors = DetectedAttractors();

		bool tempCheck = false;
		int tempPriority = 0;
		foreach (EnemyReactions reaction in behaviourHierarchy)
		{
			if ((reaction.stateRestriction.Count < 1 || reaction.stateRestriction.Contains(currentState)) && (reaction.statusRestrictions.Count < 1 ||
				reaction.allStatusesRequired ? currentStatuses.Intersect(reaction.statusRestrictions).Count() == currentStatuses.Count() :
				reaction.statusRestrictions.Intersect(currentStatuses).Any()))
			{
				Transform tempFocus = defaultFocus;
				float tempValue = -1;
				List<Attractor> tempAttractors = new List<Attractor>();
				if (reaction.attractorType != AttractorType.NONE && tempDetectedAttractors.ContainsKey(reaction.attractorType))
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

				if (reaction.attractorType == AttractorType.NONE || tempAttractors.Count > 0)
				{
					bool conditionsMet = true;
					if (reaction.allConditionsRequired)
					{

						//YOU NEED TO FIX THIS!!!!!!!!!!!!!!!!! RIGHT NOW IT ONLY CHECKS IF THE FLOAT AND INT VALUES ARE EXACTLY THE SAME!! MAKE IT SO GREATER THAN
						//AND LESS THAN STATEMENTS ARE POSSIBLE!!!!!!!!!!
						if (reaction.reactionConditions.boolConditions.Count > 0 &&
							!(currentConditions.boolConditions.Intersect(reaction.reactionConditions.boolConditions).Count() ==
							currentConditions.boolConditions.Count()))
						{
							conditionsMet = false;
						}
						else if (reaction.reactionConditions.floatConditions.Count > 0 &&
							!(currentConditions.floatConditions.Intersect(reaction.reactionConditions.floatConditions).Count() ==
							currentConditions.floatConditions.Count()))
						{
							conditionsMet = false;
						}
						else if (reaction.reactionConditions.intConditions.Count > 0 &&
							!(currentConditions.intConditions.Intersect(reaction.reactionConditions.intConditions).Count() ==
							currentConditions.intConditions.Count()))
						{
							conditionsMet = false;
						}
					}

					if (conditionsMet)
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
							foreach (FunctionPicker function in reaction.functionExecutions)
							{
								HandleFunctionCalling(function);
							}
							currentStatePriority = nextStatePriority;
						}

						tempCheck = true;
						break;
					}
				}
				else if (!reaction.allConditionsRequired)
				{
					if ((reaction.reactionConditions.boolConditions.Count > 0 &&
						reaction.reactionConditions.boolConditions.Intersect(currentConditions.boolConditions).Any()) ||
						(reaction.reactionConditions.floatConditions.Count > 0 &&
						reaction.reactionConditions.floatConditions.Intersect(currentConditions.floatConditions).Any()) ||
						(reaction.reactionConditions.intConditions.Count > 0 &&
						reaction.reactionConditions.intConditions.Intersect(currentConditions.intConditions).Any()))
					{
						nextFocus = defaultFocus;

						nextStatePriority = tempPriority;
						nextState = reaction.stateChange;
						if (nextStatePriority < currentStatePriority)
						{
							currentFocus = nextFocus;
							currentState = nextState;
							foreach (FunctionPicker function in reaction.functionExecutions)
							{
								HandleFunctionCalling(function);
							}
							currentStatePriority = nextStatePriority;
						}

						tempCheck = true;
						break;
					}
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
		else if (currentState == EnemyState.Inspect)
		{
			agent.speed = 0;
			animator.SetBool("Inspecting", true);
			currentInspect += Time.deltaTime;
			if (currentInspect >= inspectTime)
			{
				currentInspect = 0;
				animator.SetBool("Inspecting", false);
				currentFocus = nextFocus;
				currentState = nextState;
				currentStatePriority = nextStatePriority;
			}
		}
		else if (currentState == EnemyState.Search)
		{
			agent.speed = searchSpeed;

			if (!searching)
			{
				if (searchLocations.Count > 0)
					searchLocations.Clear();
				searchTimer = Random.Range(minSearchTimer, maxSearchTimer);
				searchAmount = Random.Range(minSearchAmount, maxSearchAmount + 1);

				for (int spots = 0; spots < searchAmount; spots++)
				{
					Vector3 searchSpot = FindSearchSpot(searchRadius);
					if (searchSpot != Vector3.zero)
					{
						searchLocations.Add(searchSpot);
						if (hide)
							break;
					}
					else
						break;
				}

				searchingSpot = false;
				searching = true;

				if (avoidHideTarget && currentFocus.TryGetComponent(out NavMeshObstacle obstacle))
				{
					obstacle.enabled = true;
					obstacle.radius = hideTargetAvoidanceRange;
					currentAvoidedTarget = obstacle;
				}
			}

			else
			{
				searchTimer -= Time.deltaTime;

				if (!searchingSpot)
				{
					if (searchLocations.Count > 0)
					{
						int lastIndex = searchLocations.Count - 1;
						agent.SetDestination(searchLocations[lastIndex]);
						searchLocations.RemoveAt(lastIndex);
						searchingSpot = true;
					}
					else
					{
						animator.SetBool("LookingAround", false);
						searchTimer = 0;
						searching = false;
						currentAvoidedTarget.enabled = false;
						hiddenStationary = false;
						hiding = false;
						currentFocus = nextFocus;
						currentState = nextState;
						currentStatePriority = nextStatePriority;
					}
				}
				else if (hiddenStationary || agent.remainingDistance < agent.stoppingDistance)
				{
					agent.ResetPath();
					if (hide)
					{
						hiddenStationary = true;
						if (CheckConeVisibility(transform.position, senses[searchSenseIndex]))
						{
							hiding = false;
							Vector3 searchSpot = FindSearchSpot(hideRadius);
							if (searchSpot != Vector3.zero)
							{
								agent.SetDestination(searchSpot);
							}
							else
							{
								searchingSpot = false;
								hiddenStationary = false;
							}
						}
						else
						{
							hiding = true;
						}
					}
					else
						searchingSpot = false;
				}
				
				if (searchTimer <= 0)
				{
					animator.SetBool("LookingAround", false);
					searchTimer = 0;
					searching = false;
					hiddenStationary = false;
					hiding = false;
					currentAvoidedTarget.enabled = false;
					currentFocus = nextFocus;
					currentState = nextState;
					currentStatePriority = nextStatePriority;
				}
			}
		}
		else if (currentState == EnemyState.Flee)
		{
			agent.speed = fleeSpeed;

			if (!fleeing)
			{
				fleeTime = Random.Range(minFleeTime, maxFleeTime);
				Vector3 directionToFocus = transform.position - currentFocus.position;

				// Normalize the direction to ensure consistent movement speed
				Vector3 fleeDirection = directionToFocus.normalized;

				float fleeDistance = Random.Range(minFleeDistance, maxFleeDistance);

				Vector3 targetDestination = transform.position + fleeDirection * fleeDistance;

				if (avoidTarget && currentFocus.TryGetComponent(out NavMeshObstacle obstacle))
				{
					obstacle.enabled = true;
					obstacle.radius = fleeTargetAvoidanceRange;
					currentAvoidedTarget = obstacle;
				}

				agent.SetDestination(targetDestination);
				fleeing = true;
			}
			else
			{
				fleeTime -= Time.deltaTime;

				if (fleeTime <= 0 || agent.remainingDistance < 0.5f)
				{
					fleeTime = 0;
					fleeing = false;
					currentAvoidedTarget.enabled = false;
					currentFocus = nextFocus;
					currentState = nextState;
					currentStatePriority = nextStatePriority;
				}
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
		Vector3 randomPoint = FindFirstObjectByType<NavMeshSurface>().transform.position + Random.insideUnitSphere * patrolRadius;
		NavMeshHit hit;

		// Sample the NavMesh to find the closest valid point within the specified range
		if (NavMesh.SamplePosition(randomPoint, out hit, patrolRadius, NavMesh.AllAreas))
		{
			agent.SetDestination(hit.position);
		}
	}

	private Vector3 FindSearchSpot(float theRadius)
	{
		Vector3 newPos = Vector3.zero;
		bool foundValidDestination = false;
		int attempts = 0;
		int maxAttempts = 10;

		while (!foundValidDestination && attempts < maxAttempts)
		{
			Vector3 randomPoint = transform.position + Random.insideUnitSphere * theRadius;
			NavMeshHit hit;

			if (NavMesh.SamplePosition(randomPoint, out hit, theRadius, NavMesh.AllAreas))
			{
				if (!CheckConeVisibility(hit.position, senses[searchSenseIndex]))
				{
					newPos = hit.position;
					foundValidDestination = true;
				}
			}
			attempts++;
		}

		return newPos;
	}
}
