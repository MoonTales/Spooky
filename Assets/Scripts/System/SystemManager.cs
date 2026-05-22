using Managers;
using Player;
using UI;
using UnityEngine;

namespace System
{
    /// <summary>
    /// SystemManager is a singleton class that manages core system functionalities, such as initializing subsystems,
    /// initializing Managers, and handling global events.
    /// </summary>
    public class SystemManager : Singleton<SystemManager>
    {
        [SerializeField] private float _baseVitality = 10f;

        // Start is called before the first frame update
        protected override void Awake()
        {
            base.Awake();
            CreateManagers();
        }

        public void Start()
        {
            // FOR NOW WE WILL DO STATS TESTING HERE
            DebugUtils.Log("Initializing Player Stats...");
            StatsSystem.Instance.InitializeStats();
            
            float vitality    = StatsSystem.Instance.GetStat(PrimaryStatType.Vitality);
            float maxHealth   = StatsSystem.Instance.GetStat(SubStatType.MaxHealth);
            float healthRegen = StatsSystem.Instance.GetStat(SubStatType.HealthRegen);
            DebugUtils.Log($"Player Vitality: {vitality}");
            DebugUtils.Log($"Player Max Health: {maxHealth}");
            DebugUtils.Log($"Player Health Regen: {healthRegen}");
            
            StatsSystem.Instance.UpgradeStat(PrimaryStatType.Vitality, 5f);
            vitality    = StatsSystem.Instance.GetStat(PrimaryStatType.Vitality);
            maxHealth   = StatsSystem.Instance.GetStat(SubStatType.MaxHealth);
            healthRegen = StatsSystem.Instance.GetStat(SubStatType.HealthRegen);
            DebugUtils.Log($"Player Vitality: {vitality}");
            DebugUtils.Log($"Player Max Health: {maxHealth}");
            DebugUtils.Log($"Player Health Regen: {healthRegen}");
            
        }
        

        /// <summary>
        /// Creates all required singleton managers for the game and attaches them to the SystemManager.
        /// These are primary game managers that do not require any references added via the Unity Editor.
        /// </summary>
        private void CreateManagers()
        {
            // New managers should be added here in this method:
            // gameObject.AddComponent<ManagerClassName>();
            gameObject.AddComponent<PlayerManager>();
            gameObject.AddComponent<GameStateManager>();
            gameObject.AddComponent<SceneSwapper>();
            gameObject.AddComponent<PlayerInventory>();
            gameObject.AddComponent<DrawingStateManager>();
            gameObject.AddComponent<NotificationController>();
            gameObject.AddComponent<CutsceneManager>();
            gameObject.AddComponent<LetterManager>();
            gameObject.AddComponent<SpawnAnchorManager>();
            gameObject.AddComponent<ScreenFadeManager>();
            gameObject.AddComponent<SleepTrackerManager>();
            gameObject.AddComponent<SaveSystem>();
            gameObject.AddComponent<UiPopupConfirmation>();
            gameObject.AddComponent<SceneCache>();
            gameObject.AddComponent<SecretCodeManager>();
            gameObject.AddComponent<HorrorEventManager>();
            gameObject.AddComponent<StatsSystem>();
        }
    }
}
