using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;

[System.Serializable]
public class DialogueMarker : Marker, INotification
{
    public TextKey dialogueKey;
    public float displayDuration = 3f;
    
    // INotification implementation
    public PropertyName id { get; }
}