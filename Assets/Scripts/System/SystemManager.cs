using Managers;

namespace System
{
    /// <summary>
    /// SystemManager is a singleton class that manages core system functionalities, such as initializing subsystems,
    /// initializing Managers, and handling global events.
    /// </summary>
    public class SystemManager : Singleton<SystemManager>
    {
        // Start is called before the first frame update
        private void Start()
        {
            CreateManagers();
            DebugUtils.LogSuccess("All managers built successfully.");
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
        
        }
    }
}
