using UnityEngine;
using Types = System.Types;
namespace Doors
{
    /// <summary>
    /// Class used to represent a key that can unlock a door.
    ///
    /// For the most part, this class will just be placed on a key object in the scene.
    /// </summary>
    public class Key : MonoBehaviour
    {
        [Header("Key Settings")]
        [SerializeField] private Types.FKeyData _keyData;
    }
}
