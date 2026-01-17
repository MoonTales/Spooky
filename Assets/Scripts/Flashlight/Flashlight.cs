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
    [SerializeField] private float minOnTimeToTriggerSpecialFlicker_High = 10f;
    [SerializeField] private float maxOnTimeToTriggerSpecialFlicker_High = 40f;
    [SerializeField] [Range(0f, 1f)] private float specialFlickerChance_High = 0.3f;
    [SerializeField] private float minOnTimeToTriggerSpecialFlicker_Medium = 10f;
    [SerializeField] private float maxOnTimeToTriggerSpecialFlicker_Medium = 30f;
    [SerializeField] [Range(0f, 1f)] private float specialFlickerChance_Medium = 0.5f;
    [SerializeField] private float minOnTimeToTriggerSpecialFlicker_Low = 10f;
    [SerializeField] private float maxOnTimeToTriggerSpecialFlicker_Low = 20f;
    [SerializeField] [Range(0f, 1f)] private float specialFlickerChance_Low = 0.7f;
    [SerializeField] private float minOnTimeToTriggerSpecialFlicker_Critical = 5f;
    [SerializeField] private float maxOnTimeToTriggerSpecialFlicker_Critcal = 10f;
    [SerializeField] [Range(0f, 1f)] private float specialFlickerChance_Critcal = 0.9f;
    
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
            DebugUtils.Log("Flashlight Battery Life: " + _batteryLife + "%, State: " + _currentBatteryState);
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
            // Wait a random time between min and max
            float minOnTimeToTriggerSpecialFlicker;
            if (_currentBatteryState == Types.FlashlightBatteryState.High){minOnTimeToTriggerSpecialFlicker = minOnTimeToTriggerSpecialFlicker_High;}
            else if (_currentBatteryState == Types.FlashlightBatteryState.Medium){minOnTimeToTriggerSpecialFlicker = minOnTimeToTriggerSpecialFlicker_Medium;}
            else if (_currentBatteryState == Types.FlashlightBatteryState.Low){minOnTimeToTriggerSpecialFlicker = minOnTimeToTriggerSpecialFlicker_Low;}
            else {minOnTimeToTriggerSpecialFlicker = minOnTimeToTriggerSpecialFlicker_Critical;}
            float maxOnTimeToTriggerSpecialFlicker;
            if (_currentBatteryState == Types.FlashlightBatteryState.High){maxOnTimeToTriggerSpecialFlicker = maxOnTimeToTriggerSpecialFlicker_High;}
            else if (_currentBatteryState == Types.FlashlightBatteryState.Medium){maxOnTimeToTriggerSpecialFlicker = maxOnTimeToTriggerSpecialFlicker_Medium;}
            else if (_currentBatteryState == Types.FlashlightBatteryState.Low){maxOnTimeToTriggerSpecialFlicker = maxOnTimeToTriggerSpecialFlicker_Low;}
            else {maxOnTimeToTriggerSpecialFlicker = maxOnTimeToTriggerSpecialFlicker_Critcal;}
            float waitTime = UnityEngine.Random.Range(minOnTimeToTriggerSpecialFlicker, maxOnTimeToTriggerSpecialFlicker);
            yield return new WaitForSeconds(waitTime);
            
            // double check again to make sure flashlight is still on
            if (!_isOn) { yield break; }
            
            float specialFlickerChance;
            if (_currentBatteryState == Types.FlashlightBatteryState.High){specialFlickerChance = specialFlickerChance_High;}
            else if (_currentBatteryState == Types.FlashlightBatteryState.Medium){specialFlickerChance = specialFlickerChance_Medium;}
            else if (_currentBatteryState == Types.FlashlightBatteryState.Low){specialFlickerChance = specialFlickerChance_Low;}
            else {specialFlickerChance = specialFlickerChance_Critcal;}
            
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