using UnityEngine;

public class HeadScript : MonoBehaviour
{
    [SerializeField] private Transform cinemachineCamera;

    void LateUpdate()
    {
        if (cinemachineCamera == null){ return;}

        transform.SetPositionAndRotation(
            cinemachineCamera.position,
            cinemachineCamera.rotation
        );
    }
}