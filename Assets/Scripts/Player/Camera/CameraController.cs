using System;
using Unity.Cinemachine;
using UnityEngine;

namespace Player.Camera
{
    public class CameraLagController : Singleton<CameraLagController>
    {
        [Header("Camera Lag")]
        [SerializeField] private float panLag = 6f;
        [SerializeField] private float tiltLag = 6f;
        
        [SerializeField] private float tiltMin = -80f;
        [SerializeField] private float tiltMax = 80f;

        [Header("Input")]
        [SerializeField] private float sensitivity = 2f;

        // The "ahead" rotation the flashlight will snap to
        public float TargetPan { get; private set; }
        public float TargetTilt { get; private set; }

        private CinemachineCamera _cinemachineCamera;
        private CinemachinePanTilt _panTilt;

        private float _currentPan;
        private float _currentTilt;

        private void Start()
        {
            _cinemachineCamera = GetComponent<CinemachineCamera>();
            _panTilt = GetComponent<CinemachinePanTilt>();

            _currentPan = _panTilt.PanAxis.Value;
            _currentTilt = _panTilt.TiltAxis.Value;
            TargetPan = _currentPan;
            TargetTilt = _currentTilt;
        }

        private void Update()
        {
            // Read raw input yourself
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            TargetPan += mouseX * sensitivity;
            TargetTilt -= mouseY * sensitivity; // minus because mouse up = tilt down
            TargetTilt = Mathf.Clamp(TargetTilt, tiltMin, tiltMax);
        }

        private void LateUpdate()
        {
            // Lerp the camera's actual pan/tilt to lag behind the target
            _currentPan = Mathf.LerpAngle(_currentPan, TargetPan, Time.deltaTime * panLag);
            _currentTilt = Mathf.LerpAngle(_currentTilt, TargetTilt, Time.deltaTime * tiltLag);

            _panTilt.PanAxis.Value = _currentPan;
            _panTilt.TiltAxis.Value = _currentTilt;
        }
    }
}