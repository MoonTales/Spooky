using UnityEngine;

namespace System
{
    /// <summary>
    /// Singleton[T]
    /// 
    /// This is a generic base class for creating Singleton MonoBehaviour instances in Unity.
    /// It ensures that only one instance of the class exists and provides global access to it through a static Instance property.
    /// 
    /// Features:
    /// - Enforces the Singleton pattern to prevent multiple instances.
    /// - Automatically assigns the instance if one exists in the scene.
    /// - Logs helpful messages if no instance is found or if duplicates exist.
    /// - Makes the Singleton persistent across scenes using DontDestroyOnLoad.
    /// 
    /// How to use:
    /// 1. Inherit from Singleton[T] in your MonoBehaviour class. Replace `T` with the name of your class.
    ///    Example: `public class PlayerManager : Singleton[PlayerManager] { }`
    /// 
    /// 2. Access the singleton instance using `YourClassName.Instance`. Example: `PlayerManager.Instance`.
    /// 
    /// 3. Ensure there is exactly one GameObject in your scene with the Singleton-derived component.
    ///    If multiple instances exist, duplicates will be automatically destroyed.
    /// 
    /// Notes:
    /// - This class only works with MonoBehaviour-derived classes.
    /// - Use `DontDestroyOnLoad(gameObject)` to make the singleton persistent across scenes.
    ///
    /// Created by: MoonTales
    ///  </summary>
    public class Singleton<T> : EventSubscriberBase where T : MonoBehaviour
    {
        // Private static reference to the single instance of the class.
        // This is the actual instance that will be returned when calling the Instance property.
        private static T _instance;

        /// <summary>
        /// Public static property to access the Singleton instance.
        /// If the instance does not already exist, it will try to find it in the scene.
        /// If no instance is found, it will log an error message.
        /// </summary>
        public static T Instance
        {
            get
            {
                // Check if the instance has already been assigned
                if (!_instance)
                {
                    // Attempt to find an instance of the Singleton in the scene
                    _instance = FindAnyObjectByType<T>();

                    // If no instance is found, log an error message
                    if (!_instance)
                    {
                        DebugUtils.LogError($"[Singleton] An instance of {typeof(T)} is needed in the scene but none was found.");
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Unity's Awake method, called when the script instance is being loaded.
        /// This ensures the Singleton is correctly assigned and enforces the Singleton pattern by destroying duplicates.
        /// </summary>
        protected virtual void Awake()
        {
            // Check if there is already an existing instance
            if (_instance != null && _instance != this)
            {
                // Log a warning and destroy the duplicate instance
                DebugUtils.LogWarning($"[Singleton] Duplicate instance of {typeof(T)} found. Destroying this instance.");
                Destroy(gameObject);
                return;
            }

            // Assign this instance as the Singleton
            _instance = this as T;

            // Make the Singleton persistent across scenes
            // only add this if we are currently a root object
            if (transform.parent == null){ DontDestroyOnLoad(gameObject); }
        }
    }
}
