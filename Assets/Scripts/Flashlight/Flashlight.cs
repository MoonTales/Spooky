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
    [SerializeField] private float maxFlickerDistance = 10f;
    
    private bool _isOn = false; 
    public bool IsOn() { return _isOn; }
    
    private bool _isFlickering = false;
    
    // Since a flashlight may have multiple "Light" components (for different effects), we can store them in an array
    // this will allow us to directly control all light components of the flashlight
    private Light[] _lightComponents;
    
    // Cache for raycast
    [SerializeField] private Camera playerCamera;
    
    private void Start()
    {
        // Get all Light components attached to this GameObject and its children
        _lightComponents = GetComponentsInChildren<Light>();
        
        // Get camera if not assigned
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
        
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
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
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
        // play SFX
        AudioManager.Instance.PlayFlashlightOn();
        // turn on all light components
        foreach (var light in _lightComponents)
        {
            light.enabled = true;
        }
    }
    
    private void HandleFlashlightOff()
    {
        // play SFX
        AudioManager.Instance.PlayFlashlightOff();
        // turn off all light components
        foreach (var light in _lightComponents)
        {
            light.enabled = false;
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
                // we dont want to flicker the point light
                if (light.type == LightType.Point){ continue;}
                light.enabled = UnityEngine.Random.value > 0.5f;
            }
            
            yield return new WaitForSeconds(flickerSpeed);
            elapsed += flickerSpeed;
        }
        
        // Ensure all lights are back on after flickering
        foreach (var light in _lightComponents)
        {
            light.enabled = true;
        }
        
        _isFlickering = false;
    }
}