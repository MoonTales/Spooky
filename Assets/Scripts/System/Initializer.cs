using UnityEngine;

namespace System
{
    /// <summary>
    /// Responsible for initializing persistent game systems and objects before the first scene is loaded.
    /// Ensures that core managers or services remain available throughout the application's lifetime.
    /// </summary>
    public static class Initializer
    {
        /// <summary>
        /// Unity callback that is automatically invoked before the first scene is loaded.
        /// Used to set up persistent systems and objects that should exist globally.
        /// </summary>
        /// <remarks>
        /// - The method is marked with <see cref="RuntimeInitializeOnLoadMethodAttribute"/> to ensure it runs
        ///   prior to scene loading.
        /// - Instantiates and preserves a prefab called "PERSISTANTOBJECTS" from the Resources folder.
        /// - This object is set to <see cref="Object.DontDestroyOnLoad(Object)"/> so it persists across scene changes.
        /// </remarks>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Execute()
        {
            // Log initialization step for debugging and visibility.
            //DebugUtils.Log("Loading persistent systems...");

            // Load the "PERSISTANTOBJECTS" prefab from Resources,
            // instantiate it, and mark it to not be destroyed when changing scenes.
            UnityEngine.Object.DontDestroyOnLoad(
                UnityEngine.Object.Instantiate(
                    UnityEngine.Resources.Load("PERSISTANTOBJECTS")
                )
            );
        }
    }
}