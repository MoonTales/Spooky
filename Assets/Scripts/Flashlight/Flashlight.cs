using System;
using System.Collections;
using Managers;
using Player;
using Unity.Cinemachine;
using UnityEngine;
using Types = System.Types;

public class Flashlight : Singleton<Flashlight>
{
    // Tag used for what should cause the flashlight to Flicker
    [SerializeField] private string flickerTag = "Enemy";
    
    [Header("Flicker Settings")]
    [SerializeField] private float flickerDuration = 0.5f;
    [SerializeField] private float flickerSpeed = 0.1f;
    [SerializeField] private float maxFlickerDistance = 15f;
    [Space(10)]
    [Header("Special Flicker Settings")]
    // every random on time between the min and max, chance for a FORCED flicker to happen (as long as the flashlight is on)
    [SerializeField] private float minOnTimeToTriggerSpecialFlicker = 10f;
    [SerializeField] private float maxOnTimeToTriggerSpecialFlicker = 30f;
    [SerializeField] [Range(0f, 1f)] private float specialFlickerChance = 0.3f;
    [Space(10)]
    [Header("battery Settings")]
    [SerializeField] private float maxBatteryLife = 100f; // Assume this is in seconds for now
    [SerializeField] private float minBatterylife = 20f; // The lowest battery life we will drop to
    [SerializeField] private float batteryDrainRate = 1f; // percentage per minute
    [SerializeField] private float batteryRechargeRate = 0.5f; // percentage per minute
    // threshold values
    [SerializeField] private float highBatteryThreshold = 75f; // percentage
    [SerializeField] private float mediumBatteryThreshold = 50f; // percentage
    [SerializeField] private float lowBatteryThreshold = 20f; // percentage
    [SerializeField] private float criticalBatteryThreshold = 5f; // percentage
    private Types.FlashlightBatteryState _currentBatteryState = Types.FlashlightBatteryState.High;
    private float _batteryLife = 100f; // percentage
    
    
    // Internal Variables
    private bool _isOn = false;
    private bool _isFlickering = false;
    // Since a flashlight may have multiple "Light" components (for different effects), we can store them in an array
    // this will allow us to directly control all light components of the flashlight
    private Light[] _lightComponents;
    private Camera _playerCamera;
    // Special flicker timer
    private Coroutine _specialFlickerCoroutine;
    
    
    private CinemachineCamera CinemaCamera;
    private CinemachinePanTilt panTilt;
    
    
    [SerializeField] private float panDrag = 8f;
    [SerializeField] private float tiltDrag = 8f;

    private float _currentPan;
    private float _currentTilt;

    
    private void Start()
    {
        // Get all Light components attached to this GameObject and its children
        _lightComponents = GetComponentsInChildren<Light>();
        
        // Get camera if not assigned
        if (_playerCamera == null) {_playerCamera = Camera.main;}
        
        // Initialize flashlight state
        OnFlashlightToggled(_isOn);
        
        CinemaCamera = PlayerManager.Instance.GetCinemachineCamera();
        panTilt = CinemaCamera.GetComponent<CinemachinePanTilt>();
        // Initialize current values to match starting rotation
        if (panTilt != null)
        {
            _currentPan = panTilt.PanAxis.Value;
            _currentTilt = panTilt.TiltAxis.Value;
        }
    }
    
    private void Update()
    {
        if (_isOn && !_isFlickering)
        {
            CheckForEnemy();
        }
        DebugUtils.Log("current pan: " + _currentPan + " current tilt: " + _currentTilt);
    }

    private void LateUpdate()
    {
        if (CinemaCamera == null)
        {
            CinemaCamera = PlayerManager.Instance.GetCinemachineCamera();
            if (CinemaCamera == null) return;
        }

        // Get target rotation - try WORLD rotation instead of local
        Vector3 targetEuler = CinemaCamera.transform.eulerAngles; // Changed from localEulerAngles
        float targetPan = targetEuler.y;
        float targetTilt = targetEuler.x;

        // Use LerpAngle to handle wrapping
        _currentPan = Mathf.LerpAngle(_currentPan, targetPan, Time.deltaTime * panDrag);
        _currentTilt = Mathf.LerpAngle(_currentTilt, targetTilt, Time.deltaTime * tiltDrag);

        // IMPORTANT: Normalize the angles to prevent infinite growth
        _currentPan = Mathf.Repeat(_currentPan, 360f);
        _currentTilt = Mathf.Repeat(_currentTilt, 360f);

        // Apply to flashlight - you might need WORLD rotation here too
        transform.rotation = Quaternion.Euler(_currentTilt, _currentPan, 0f); // Changed from localRotation
    }


    private void CheckForEnemy()
    {
        Ray ray = new Ray(_playerCamera.transform.position, _playerCamera.transform.forward);
        RaycastHit hit;
        
        // this it to "narrow", we want a "wider" cone of detection for the flashlight
        /*
        if (Physics.Raycast(ray, out hit, maxFlickerDistance))
        {
            if (hit.collider.CompareTag(flickerTag))
            {
                StartCoroutine(FlashlightFlicker());
            }
        }
        */
        if (Physics.SphereCast(ray, 0.5f, out hit, maxFlickerDistance))
        {
            if (hit.collider.CompareTag(flickerTag))
            {
                StartCoroutine(FlashlightFlicker());
            }
        }
        // draw a debug ray
        Debug.DrawRay(ray.origin, ray.direction * maxFlickerDistance, Color.yellow);
    }
    
    private void OnFlashlightToggled(bool isOn)
    {
        // Handle flashlight toggle event
        if (isOn)
        {
            HandleFlashlightOn();
        }
        else
        {
            HandleFlashlightOff();
        }
    }

    private void HandleFlashlightOn()
    {
        EventBroadcaster.Broadcast_OnFlashlightToggled(true);
        // play SFX
        AudioManager.Instance.PlayFlashlightOn();
        // turn on all light components
        SetAllLights(true);
        // Start special flicker timer
        if (_specialFlickerCoroutine != null)
        {
            StopCoroutine(_specialFlickerCoroutine);
        }
        _specialFlickerCoroutine = StartCoroutine(SpecialFlickerTimer());
    }
    
    private void HandleFlashlightOff()
    {
        EventBroadcaster.Broadcast_OnFlashlightToggled(false);
        // play SFX
        AudioManager.Instance.PlayFlashlightOff();
        // turn off all light components
        SetAllLights(false);
        // Stop special flicker timer
        if (_specialFlickerCoroutine != null)
        {
            StopCoroutine(_specialFlickerCoroutine);
            _specialFlickerCoroutine = null;
        }
    }
    
    private IEnumerator SpecialFlickerTimer()
    {
        while (_isOn)
        {
            // Wait a random time between min and max
            float waitTime = UnityEngine.Random.Range(minOnTimeToTriggerSpecialFlicker, maxOnTimeToTriggerSpecialFlicker);
            yield return new WaitForSeconds(waitTime);
            
            // double check again to make sure flashlight is still on
            if (!_isOn) { yield break; }
            
            // Roll for chance to trigger special flicker
            if (UnityEngine.Random.value <= specialFlickerChance)
            {
                StartCoroutine(FlashlightFlicker());
            }
        }
    }
    
    public void ToggleFlashlight()
    {
        _isOn = !_isOn;
        OnFlashlightToggled(_isOn);
    }

    private IEnumerator FlashlightFlicker()
    {
        _isFlickering = true;
        float elapsed = 0f;
        
        while (elapsed < flickerDuration)
        {
            // Randomly flicker each light component
            foreach (var light in _lightComponents)
            {
                // we dont want to flicker the point light (since thats not really a flashlight lol)
                if (light.type == LightType.Point){ continue;}
                // 50/50 chance to enable or disable the light
                light.enabled = UnityEngine.Random.value > 0.5f;
            }
            
            yield return new WaitForSeconds(flickerSpeed);
            elapsed += flickerSpeed;
        }
        
        // Ensure all lights are back on after flickering
        // if the flashlight is still on, return them to enabled state
        SetAllLights(_isOn);


        _isFlickering = false;
    }
    
    private void SetAllLights(bool state)
    {
        foreach (Light light in _lightComponents)
        {
            light.enabled = state;
        }
    }
    
}