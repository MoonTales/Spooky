using System;
using Managers;
using UnityEngine;

public class ShowOnCondition : EventSubscriberBase
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (GameStateManager.Instance.GetIsPhotoInRoom())
        {
            gameObject.SetActive(true);
        }
        else
        {
            gameObject.SetActive(false);
        }
        
    }
    
}
