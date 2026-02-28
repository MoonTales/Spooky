using System;
using Unity.Cinemachine;
using UnityEngine;
using Types = System.Types;

namespace Player.Camera
{
    public class CameraLagController : Singleton<CameraLagController>
    {
        [Header("Camera Lag")]
        [SerializeField] private float panLag = 6f;
        [SerializeField] private float tiltLag = 6f;
        [SerializeField] private float snapLag = 25f; // fast "snap" when flashlight is off

        
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
        
        private bool _shouldUpdate = true;

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
            if (!_shouldUpdate) return;

            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            TargetPan += mouseX * sensitivity;
            TargetTilt -= mouseY * sensitivity;
            TargetTilt = Mathf.Clamp(TargetTilt, tiltMin, tiltMax);
        }

        private bool _isCaughtUp = true;

        private void LateUpdate()
        {
            if (!_shouldUpdate) return;

            float lag = Flashlight.Instance.IsFlashlightOn() ? panLag : snapLag;

            _currentPan = Mathf.LerpAngle(_currentPan, TargetPan, Time.deltaTime * lag);
            _currentTilt = Mathf.LerpAngle(_currentTilt, TargetTilt, Time.deltaTime * lag);

            _panTilt.PanAxis.Value = _currentPan;
            _panTilt.TiltAxis.Value = _currentTilt;
        }
        
        
        protected override void OnGameStateChanged(Types.GameState newState)
        {
            if (newState == Types.GameState.Gameplay)
            {
                // we only want to update during gameplay sections of the game
                _shouldUpdate = true;
            }
            else
            {
                _shouldUpdate = false;
            }
        }
    }
}