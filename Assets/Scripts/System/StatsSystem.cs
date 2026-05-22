//————————————————————————————————————————————————————————————————
// The following code is written and maintained by MoonTales Studio,
// under the creative direction of Cohen Calvert. 
// You are not allowed to use, alter, modify, or re-distribute this
// code without explicit permission from MoonTales Studio.
//————————————————————————————————————————————————————————————————

using System.Collections.Generic;


namespace System
{
    /// <summary>
    /// Singleton system that manages all player stats.
    /// Structure:
    ///   Character (Level and Experience)
    ///   - PrimaryStat
    ///     - SubStat (calculated from its parent PrimaryStat)
    /// </summary>
    public class StatsSystem : Singleton<StatsSystem>
    {
        //————— Internal Variables —————//
        private readonly Dictionary<PrimaryStatType, PrimaryStat> _primaryStats = new Dictionary<PrimaryStatType, PrimaryStat>();
        private readonly Dictionary<SubStatType, PrimaryStatType> _subStatLookup = new Dictionary<SubStatType, PrimaryStatType>();
        //————— Public API —————//
        
        /// <summary>
        /// Initializes the stats at with their default values
        /// </summary>
        public void InitializeStats()
        {
            Register(PrimaryStatType.Vitality, 10f);
            RegisterSubStat(PrimaryStatType.Vitality, SubStatType.MaxHealth,   v => v * 2f);
            RegisterSubStat(PrimaryStatType.Vitality, SubStatType.HealthRegen, v => v * 0.5f);
        }
        
        /// <summary>
        /// Registers a PrimaryStat under a given PrimaryStatType key.
        /// </summary>
        public void Register(PrimaryStatType type, float baseValue)
        {
            if (_primaryStats.ContainsKey(type))
            {
                UnityEngine.Debug.LogWarning($"[StatsSystem] Stat '{type}' is already registered.");
                return;
            }

            _primaryStats[type] = new PrimaryStat(baseValue);
        }
        public void RegisterSubStat(PrimaryStatType primaryType, SubStatType subType, Func<float, float> calculationFunc)
        {
            PrimaryStat stat = GetPrimaryStat(primaryType);
            if (stat == null)
            {
                UnityEngine.Debug.LogError($"[StatsSystem] Cannot register SubStat '{subType}', PrimaryStat '{primaryType}' not found.");
                return;
            }

            stat.AddSubStat(subType, calculationFunc);
            _subStatLookup[subType] = primaryType;
        }

        public float GetStat(PrimaryStatType type)
        {
            return GetPrimaryStat(type)?.BaseValue ?? 0f;
        }

        public float GetStat(SubStatType subType)
        {
            if (!_subStatLookup.TryGetValue(subType, out PrimaryStatType primaryType))
            {
                UnityEngine.Debug.LogError($"[StatsSystem] SubStat '{subType}' has no registered parent.");
                return 0f;
            }

            return GetSubStatValue(primaryType, subType);
        }

        /// <summary>
        /// Retrieves the PrimaryStat associated with the given type.
        /// Returns null if not registered.
        /// </summary>
        private PrimaryStat GetPrimaryStat(PrimaryStatType type)
        {
            _primaryStats.TryGetValue(type, out PrimaryStat stat);
            return stat;
        }

        /// <summary>
        /// Gets the computed value of a SubStat by its SubStatType.
        /// </summary>
        private float GetSubStatValue(PrimaryStatType primaryType, SubStatType subType)
        {
            PrimaryStat stat = GetPrimaryStat(primaryType);
            if (stat == null)
            {
                UnityEngine.Debug.LogError($"[StatsSystem] PrimaryStat '{primaryType}' not found.");
                return 0f;
            }

            return stat.GetSubStatValue(subType);
        }
        
        /// <summary>
        /// Public call to update the current value of a stat
        /// Provide the stat and what you would like to add to that value
        /// </summary>
        /// <param name="type"></param>
        /// <param name="amount"></param>
        public void UpgradeStat(PrimaryStatType type, float amount)
        {
            PrimaryStat stat = GetPrimaryStat(type);
            if (stat == null)
            {
                UnityEngine.Debug.LogError($"[StatsSystem] Cannot upgrade, PrimaryStat '{type}' not found.");
                return;
            }

            stat.AddToStatValue(amount);
        }

        /// <summary>
        /// Resets and clears all stats from the system
        /// </summary>
        public void ResetAllStats()
        {
            _primaryStats.Clear();
            _subStatLookup.Clear();
        }

        /// <summary>
        /// Debug prints all of the stats to the console
        /// </summary>
        public void DebugPrintStats()
        {
            
        }
    }


    public enum PrimaryStatType
    {
        Vitality,
        Strength,
        Endurance,
        Intelligence,
        Willpower,
        Wisdom,
    }

    public enum SubStatType
    {
        // Vitality SubStats
        MaxHealth,
        HealthRegen,
        // end for now
    }


    /// <summary>
    /// Represents a primary stat with a base value and a collection of derived sub-stats.
    /// </summary>
    public class PrimaryStat
    {
        //————— Internal Variables —————//
        private float _baseValue;
        private Dictionary<SubStatType, SubStat> _subStats = new Dictionary<SubStatType, SubStat>();

        //————— Constructor —————//
        public PrimaryStat(float baseValue)
        {
            _baseValue = baseValue;
        }

        //————— Public API —————//
        public float BaseValue
        {
            get => _baseValue;
            set { _baseValue = value; }
        }

        /// <summary>
        /// Registers a SubStat of a given type, derived from this PrimaryStat.
        /// </summary>
        public void AddSubStat(SubStatType type, Func<float, float> calculationFunc)
        {
            if (_subStats.ContainsKey(type))
            {
                UnityEngine.Debug.LogWarning($"[PrimaryStat] SubStat '{type}' is already registered.");
                return;
            }

            _subStats[type] = new SubStat(this, calculationFunc);
        }

        /// <summary>
        /// Returns the computed value of a SubStat by its SubStatType.
        /// </summary>
        public float GetSubStatValue(SubStatType type)
        {
            if (!_subStats.TryGetValue(type, out SubStat subStat))
            {
                UnityEngine.Debug.LogError($"[PrimaryStat] SubStat '{type}' not found.");
                return 0f;
            }

            return subStat.GetValue();
        }
        
        /// <summary>
        /// Adds a value to the base state (for upgrades or downgrades)
        /// </summary>
        /// <param name="newValue"></param>
        public void AddToStatValue(float newValue)
        {
            BaseValue += newValue;
        }
        
    }


    /// <summary>
    /// A stat derived from a PrimaryStat using a calculation function.
    /// </summary>
    public class SubStat
    {
        //————— Internal Variables —————//
        private PrimaryStat _primaryStat;
        private Func<float, float> _calculationFunc;

        //————— Constructor —————//
        public SubStat(PrimaryStat primaryStat, Func<float, float> calculationFunc)
        {
            _primaryStat = primaryStat;
            _calculationFunc = calculationFunc;
        }

        //————— Public API —————//

        /// <summary>
        /// Returns the computed value of this SubStat, based on its parent's base value.
        /// </summary>
        public float GetValue()
        {
            return _calculationFunc(_primaryStat.BaseValue);
        }
    }
}