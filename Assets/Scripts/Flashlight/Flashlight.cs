using System;
using System.Collections;
using Managers;
using Player;
using Unity.Cinemachine;
using UnityEngine;
using Types = System.Types;


[System.Serializable]
public class FlickerSettings
{
    [Range(0f, 1f)] public float specialFlickerChance = 0.3f;
    public float minOnTimeToTriggerSpecialFlicker = 10f;
    public float maxOnTimeToTriggerSpecialFlicker = 40f;
    public float flickerDuration = 0.5f;
    public float flickerSpeed = 0.1f;
}

public class Flashlight : Singleton<Flashlight>
{
    // Tag used for what should cause the flashlight to Flicker
    [SerializeField] private string flickerTag = "Enemy";
    [Space(10)]
    [Header("Flicker Settings")]
    [SerializeField] private float maxFlickerDistance = 15f;
    [Space(10)]
    [Header("Battery State Flicker Settings")]
    [SerializeField] private FlickerSettings highBatteryFlicker = new FlickerSettings 
    { 
        specialFlickerChance = 0.3f, 
        minOnTimeToTriggerSpecialFlicker = 10f, 
        maxOnTimeToTriggerSpecialFlicker = 40f,
        flickerDuration = 0.5f,
        flickerSpeed = 0.1f
    };

    [SerializeField] private FlickerSettings mediumBatteryFlicker = new FlickerSettings 
    { 
        specialFlickerChance = 0.5f, 
        minOnTimeToTriggerSpecialFlicker = 10f, 
        maxOnTimeToTriggerSpecialFlicker = 30f,
        flickerDuration = 0.4f,
        flickerSpeed = 0.08f
    };

    [SerializeField] private FlickerSettings lowBatteryFlicker = new FlickerSettings 
    { 
        specialFlickerChance = 0.7f, 
        minOnTimeToTriggerSpecialFlicker = 5f, 
        maxOnTimeToTriggerSpecialFlicker = 10f,
        flickerDuration = 0.3f,
        flickerSpeed = 0.05f
    };

    [SerializeField] private FlickerSettings criticalBatteryFlicker = new FlickerSettings 
    { 
        specialFlickerChance = 0.9f, 
        minOnTimeToTriggerSpecialFlicker = 1f, 
        maxOnTimeToTriggerSpecialFlicker = 5f,
        flickerDuration = 0.2f,
        flickerSpeed = 0.03f
    };
    
    [Space(10)]
    [Header("battery Settings")]
    [SerializeField] private float maxBatteryLife = 100f; // Assume this is in seconds for now
    [SerializeField] private float minBatterylife = 20f; // The lowest battery life we will drop to
    [SerializeField] private float batteryDrainRate = 10f; // percentage per minute
    [SerializeField] private float batteryRechargeRate = 0.5f; // percentage per minute
    // threshold values
    [SerializeField] private float highBatteryThreshold = 75f; // percentage
    [SerializeField] private float mediumBatteryThreshold = 50f; // percentage
    [SerializeField] private float lowBatteryThreshold = 20f; // percentage
    [SerializeField] private float criticalBatteryThreshold = 1f; // percentage
    private Types.FlashlightBatteryState _currentBatteryState = Types.FlashlightBatteryState.High;
    private float _batteryLife = 100f; // percentage
    private Coroutine _batteryDrainCoroutine;
    private Coroutine _batteryRechargeCoroutine;
    
    
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
    
    private GameObject _cachedFlickerTarget;
    private GameObject _currentFlickerTarget = null;

    
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
    
        if (Physics.SphereCast(ray, 0.5f, out hit, maxFlickerDistance))
        {
            if (hit.collider.CompareTag(flickerTag))
            {
                GameObject hitEnemy = hit.collider.gameObject;
            
                // Check if this is a new enemy or the same one
                if (_currentFlickerTarget != hitEnemy)
                {
                    // If we were targeting a different enemy, broadcast false for it
                    if (_currentFlickerTarget != null)
                    {
                        EventBroadcaster.Broadcast_OnFlashlightHitEnemy(_currentFlickerTarget, false);
                    }
                
                    // Broadcast true for the new enemy
                    _currentFlickerTarget = hitEnemy;
                    EventBroadcaster.Broadcast_OnFlashlightHitEnemy(_currentFlickerTarget, true);
                
                    _cachedFlickerTarget = hitEnemy;
                    StartCoroutine(FlashlightFlicker());
                }
                return;
            }
        }
    
        // No enemy hit - if we were tracking one, broadcast false
        if (_currentFlickerTarget != null)
        {
            EventBroadcaster.Broadcast_OnFlashlightHitEnemy(_currentFlickerTarget, false);
            _currentFlickerTarget = null;
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
        if(_batteryDrainCoroutine != null)
        {
            StopCoroutine(_batteryDrainCoroutine);
        }
        _batteryDrainCoroutine = StartCoroutine(FlashlightBatteryDrain());
        // Stop battery recharge if it was recharging
        if(_batteryRechargeCoroutine != null)
        {
            StopCoroutine(_batteryRechargeCoroutine);
            _batteryRechargeCoroutine = null;
        }
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
        if(_batteryDrainCoroutine != null)
        {
            StopCoroutine(_batteryDrainCoroutine);
            _batteryDrainCoroutine = null;
        }
        // recharge battery when flashlight is off
        if(_batteryRechargeCoroutine != null)
        {
            StopCoroutine(_batteryRechargeCoroutine);
        }
        _batteryRechargeCoroutine = StartCoroutine(FlashlightBatteryRecharge());
    }


    private void SetBatteryStateBasedOnBatteryLife()
    {
        // Update battery state based on thresholds
        if (_batteryLife >= highBatteryThreshold)
        {
            _currentBatteryState = Types.FlashlightBatteryState.High;
        }
        else if (_batteryLife >= mediumBatteryThreshold)
        {
            _currentBatteryState = Types.FlashlightBatteryState.Medium;
        }
        else if (_batteryLife >= lowBatteryThreshold)
        {
            _currentBatteryState = Types.FlashlightBatteryState.Low;
        }
        else if (_batteryLife >= criticalBatteryThreshold)
        {
            _currentBatteryState = Types.FlashlightBatteryState.Critical;
        }
        else
        {
            _currentBatteryState = Types.FlashlightBatteryState.Dead;
            // Automatically turn off flashlight if battery is dead
        }
    }
    
    private IEnumerator FlashlightBatteryRecharge()
    {
        while (!_isOn && _batteryLife < maxBatteryLife) 
        {
            // Recharge battery based on recharge rate
            _batteryLife += (batteryRechargeRate / 60f) * Time.deltaTime;
            _batteryLife = Mathf.Clamp(_batteryLife, 0f, maxBatteryLife);
            SetBatteryStateBasedOnBatteryLife();
            //DebugUtils.Log("Flashlight Battery Life: " + _batteryLife + "%, State: " + _currentBatteryState);
            yield return null;
        }
    }
    private IEnumerator FlashlightBatteryDrain()
    {
        while (_isOn && _batteryLife > 0)
        {
            // Drain battery based on drain rate
            _batteryLife -= (batteryDrainRate / 60f) * Time.deltaTime;
            _batteryLife = Mathf.Clamp(_batteryLife, 0f, maxBatteryLife);
            SetBatteryStateBasedOnBatteryLife();
        
            // Check if battery died and turn off flashlight
            if (_currentBatteryState == Types.FlashlightBatteryState.Dead)
            {
                DebugUtils.Log("Battery died! Turning off flashlight.");
                ToggleFlashlight();
                yield break; // Exit coroutine immediately
            }
        
            DebugUtils.Log("Flashlight Battery Life: " + _batteryLife + "%, State: " + _currentBatteryState);
            yield return null;
        }
    }
    
    
    private IEnumerator SpecialFlickerTimer()
    {
        while (_isOn)
        {
            FlickerSettings settings = GetCurrentFlickerSettings();
        
            float waitTime = UnityEngine.Random.Range(
                settings.minOnTimeToTriggerSpecialFlicker, 
                settings.maxOnTimeToTriggerSpecialFlicker
            );
            yield return new WaitForSeconds(waitTime);
        
            if (!_isOn) { yield break; }
        
            if (UnityEngine.Random.value <= settings.specialFlickerChance)
            {
                StartCoroutine(FlashlightFlicker(settings.flickerDuration, settings.flickerSpeed));
            }
        }
    }
    
    private FlickerSettings GetCurrentFlickerSettings()
    {
        switch (_currentBatteryState)
        {
            case Types.FlashlightBatteryState.High:
                return highBatteryFlicker;
            case Types.FlashlightBatteryState.Medium:
                return mediumBatteryFlicker;
            case Types.FlashlightBatteryState.Low:
                return lowBatteryFlicker;
            default:
                return criticalBatteryFlicker;
        }
    }
    
    public void ToggleFlashlight()
    {
        _isOn = !_isOn;
        OnFlashlightToggled(_isOn);
    }

    private IEnumerator FlashlightFlicker(float flickerDuration = 0.5f, float flickerSpeed = 0.1f)
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