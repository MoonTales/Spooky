using UnityEngine;
using System.Runtime.CompilerServices;

namespace System
{
    /// <summary>
    /// A utility class for enhanced debugging in Unity. 
    /// Provides methods for custom color-coded logging, assertions, and additional debug utilities.
    /// Automatically prefixes log messages with the caller's class and method name for improved traceability.
    /// Created by: MoonTales
    /// </summary>
    public static class DebugUtils
    {
        // ReSharper disable Unity.PerformanceAnalysis
        /// <summary>
        /// Logs a custom message with a specified color. Default color is white.
        /// Automatically prefixes the message with the caller's class and method.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="color">The color of the message. Default is "white".</param>
        /// <param name="filePath">Automatically provided caller file path.</param>
        /// <param name="memberName">Automatically provided caller member (method) name.</param>
        public static void Log(string message, string color = "white",
            [CallerFilePath] string filePath = "",
            [CallerMemberName] string memberName = "")
        {
            Debug.Log(FormatMessage(AddContext(message, filePath, memberName), color));
        }

        // ReSharper disable Unity.PerformanceAnalysis
        /// <summary>
        /// Logs a warning message with a specified color. Default color is orange.
        /// Automatically prefixes the message with the caller's class and method.
        /// </summary>
        /// <param name="message">The warning message to log.</param>
        /// <param name="color">The color of the message. Default is "orange".</param>
        /// <param name="filePath">Automatically provided caller file path.</param>
        /// <param name="memberName">Automatically provided caller member (method) name.</param>
        public static void LogWarning(string message, string color = "orange",
            [CallerFilePath] string filePath = "",
            [CallerMemberName] string memberName = "")
        {
            Debug.LogWarning(FormatMessage(AddContext(message, filePath, memberName), color));
        }

        // ReSharper disable Unity.PerformanceAnalysis
        /// <summary>
        /// Logs an error message with a specified color. Default color is red.
        /// Automatically prefixes the message with the caller's class and method.
        /// </summary>
        /// <param name="message">The error message to log.</param>
        /// <param name="color">The color of the message. Default is "red".</param>
        /// <param name="filePath">Automatically provided caller file path.</param>
        /// <param name="memberName">Automatically provided caller member (method) name.</param>
        public static void LogError(string message, string color = "red",
            [CallerFilePath] string filePath = "",
            [CallerMemberName] string memberName = "")
        {
            Debug.LogError(FormatMessage(AddContext(message, filePath, memberName), color));
        }

        // ReSharper disable Unity.PerformanceAnalysis
        /// <summary>
        /// Logs a success message with a predefined green color.
        /// Automatically prefixes the message with the caller's class and method.
        /// </summary>
        /// <param name="message">The success message to log.</param>
        /// <param name="filePath">Automatically provided caller file path.</param>
        /// <param name="memberName">Automatically provided caller member (method) name.</param>
        public static void LogSuccess(string message,
            [CallerFilePath] string filePath = "",
            [CallerMemberName] string memberName = "")
        {
            Log(message, "green", filePath, memberName);
        }

        // ReSharper disable Unity.PerformanceAnalysis
        /// <summary>
        /// Logs an assertion message and throws an exception if the condition is false.
        /// Automatically prefixes the message with the caller's class and method.
        /// </summary>
        /// <param name="condition">The condition to assert. If false, an exception is thrown.</param>
        /// <param name="message">The assertion message to log.</param>
        /// <param name="filePath">Automatically provided caller file path.</param>
        /// <param name="memberName">Automatically provided caller member (method) name.</param>
        public static void Assert(bool condition, string message,
            [CallerFilePath] string filePath = "",
            [CallerMemberName] string memberName = "")
        {
            if (!condition)
            {
                LogError("Assertion Failed: " + message, "red", filePath, memberName);
                throw new global::System.Exception("Assertion Failed: " + message);
            }
        }

        // ReSharper disable Unity.PerformanceAnalysis
        /// <summary>
        /// Clears all logs in the Unity Console.
        /// Note: Only works in the Unity Editor.
        /// </summary>
        public static void ClearConsole()
        {
#if UNITY_EDITOR
            var assembly = global::System.Reflection.Assembly.GetAssembly(typeof(UnityEditor.Editor));
            var logEntries = assembly.GetType("UnityEditor.LogEntries");
            var clearMethod = logEntries.GetMethod("Clear", global::System.Reflection.BindingFlags.Static | global::System.Reflection.BindingFlags.Public);
            if (clearMethod != null) clearMethod.Invoke(null, null);
#endif
        }

        // ReSharper disable Unity.PerformanceAnalysis
        /// <summary>
        /// Formats a message with a specified color using Unity's rich text tags.
        /// </summary>
        /// <param name="message">The message to format.</param>
        /// <param name="color">The color to apply to the message.</param>
        /// <returns>The formatted message.</returns>
        private static string FormatMessage(string message, string color)
        {
            return $"<color={color}>{message}</color>";
        }

        // ReSharper disable Unity.PerformanceAnalysis
        /// <summary>
        /// Adds contextual information (class name and method name) to a log message.
        /// Example: [DungeonGeneratorManager.Start] Message here
        /// </summary>
        /// <param name="message">The original message.</param>
        /// <param name="filePath">The caller's file path (used to extract class name).</param>
        /// <param name="memberName">The caller's method name.</param>
        /// <returns>The contextualized message.</returns>
        private static string AddContext(string message, string filePath, string memberName)
        {
            var className = System.IO.Path.GetFileNameWithoutExtension(filePath);
            return $"[{className}.{memberName}] {message}";
        }
    }
}
