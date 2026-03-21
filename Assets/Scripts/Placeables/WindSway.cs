using UnityEngine;

public class WindSway : MonoBehaviour
{

    // Whatever object this is attached to, will have a gentle sway motion
    // this is for a HANGING object, meaning this rotation pivot is at the top of the object
    [SerializeField] private float swayAmount = 5f; // how far the object sways in degrees
    [SerializeField] private float swaySpeed = 1f; // how fast the object sways
    
    private Quaternion _initialRotation;
    private GameObject[] _childObjects;
    
    private void Start()
    {
        _initialRotation = transform.localRotation;
        foreach (Transform child in transform)
        {
            _childObjects = new GameObject[transform.childCount];
            for (int i = 0; i < transform.childCount; i++)
            {
                _childObjects[i] = transform.GetChild(i).gameObject;
            }
        }
    }
    private void Update()
    {
        foreach (GameObject child in _childObjects)
        {
            if (child == null)
            {
                return;
            }
            float swayX = Mathf.Sin(Time.time * swaySpeed) * swayAmount;
            float swayZ = Mathf.Cos(Time.time * swaySpeed) * swayAmount;
            Quaternion swayRotation = Quaternion.Euler(swayX, 0f, swayZ);
            child.transform.localRotation = _initialRotation * swayRotation;
        }

    }
}
