using System;
using UnityEngine;

public class SettingsController : Singleton<SettingsController> 
{
    [SerializeField] private GameObject SettingsCanvas;
    
    public void OpenSettings()
    {
        SettingsCanvas.SetActive(true);
    }
    public void CloseSettings()
    {
        SettingsCanvas.SetActive(false);
    }
}
