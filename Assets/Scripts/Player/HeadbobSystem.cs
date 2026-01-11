using UnityEngine;

public class HeadbobSystem : MonoBehaviour
{

    [SerializeField] private float amount = 0.05f;
    [SerializeField] private float frequency = 10f;
    [SerializeField] private float smoothness = 10f;
    
    private void Update()
    {
        //CheckForHeadbobTrigger();
    }

    private void CheckForHeadbobTrigger()
    {
        float inputMagnitude = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")).magnitude;
        if (inputMagnitude > 0)
        {
            // Trigger headbob effect
            StartHeadbob();
        }
    }

    private Vector3 StartHeadbob()
    {
        Vector3 pos = Vector3.zero;
        pos.y += Mathf.Lerp(pos.y, Mathf.Sin(Time.time * frequency) * amount * 1.4f, Time.deltaTime * smoothness);
        pos.x += Mathf.Lerp(pos.x, Mathf.Cos(Time.time * frequency / 2.0f) * amount * 1.6f, Time.deltaTime * smoothness);
        transform.localPosition = pos;

        return pos;
    }
}
