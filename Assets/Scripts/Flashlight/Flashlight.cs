using System;
using System.Collections;
using Managers;
using UnityEngine;

public class Flashlight : Singleton<Flashlight>
{
    // Tag used for what should cause the flashlight to Flicker
    [SerializeField] private string flickerTag = "Enemy";
    
    [Header("Flicker Settings")]
    [SerializeField] private float flickerDuration = 0.5f;
    [SerializeField] private float flickerSpeed = 0.1f;
    [SerializeField] private float maxFlickerDistance = 15f;
    [Header("Special Flicker Settings")]
    // every random on time between the min and max, chance for a FORCED flicker to happen (as long as the flashlight is on)
    [SerializeField] private float minOnTimeToTriggerSpecialFlicker = 10f;
    [SerializeField] private float maxOnTimeToTriggerSpecialFlicker = 30f;
    [SerializeField] [Range(0f, 1f)] private float specialFlickerChance = 0.3f;
    
    // Internal Variables
    private bool _isOn = false;
    private bool _isFlickering = false;
    // Since a flashlight may have multiple "Light" components (for different effects), we can store them in an array
    // this will allow us to directly control all light components of the flashlight
    private Light[] _lightComponents;
    private Camera _playerCamera;
    // Special flicker timer
    private Coroutine _specialFlickerCoroutine;
    
    private void Start()
    {
        // Get all Light components attached to this GameObject and its children
        _lightComponents = GetComponentsInChildren<Light>();
        
        // Get camera if not assigned
        if (_playerCamera == null) { _playerCamera = Camera.main; }
        
        // Initialize flashlight state
        OnFlashlightToggled(_isOn);
    }
    
    private void Update()
    {
        if (_isOn && !_isFlickering)
        {
            CheckForEnemy();
        }
    }
    
    private void CheckForEnemy()
    {
        Ray ray = new Ray(_playerCamera.transform.position, _playerCamera.transform.forward);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, maxFlickerDistance))
        {
            if (hit.collider.CompareTag(flickerTag))
            {
                StartCoroutine(FlashlightFlicker());
            }
        }
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