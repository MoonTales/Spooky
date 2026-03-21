using UnityEngine;

public class WindSway : MonoBehaviour
{

    // Whatever object this is attached to, will have a gentle sway motion
    // this is for a HANGING object, meaning this rotation pivot is at the top of the object
    [SerializeField] private float swayAmount = 5f; // how far the object sways in degrees
    [SerializeField] private float swaySpeed = 1f; // how fast the object sways
    
    private Quaternion _initialRotation;
    
    
    private void Start()
    {
        _initialRotation = transform.localRotation;
    }
    private void Update()
    {
        float swayX = Mathf.Sin(Time.time * swaySpeed) * swayAmount;
        float swayZ = Mathf.Cos(Time.time * swaySpeed) * swayAmount;
        Quaternion swayRotation = Quaternion.Euler(swayX, 0f, swayZ);
        transform.localRotation = _initialRotation * swayRotation;
    }
}
