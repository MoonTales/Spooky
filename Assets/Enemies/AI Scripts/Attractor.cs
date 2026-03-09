using UnityEngine;

public class Attractor : MonoBehaviour
{
    public AttractorAI.AttractorType attractorType;
    [Tooltip("If the Attractor Type of this Attractor is the custom type, you must define the attractor with a string ID")]
    public string customAttractorID;
    public float intensity;
}
