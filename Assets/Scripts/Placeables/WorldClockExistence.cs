using System;
using UnityEngine;

namespace Placeables
{

    public enum WorldClockShowState
    {
        OnValue, // only appear at the specified hour
        BeforeValue, // only appear before the specified hour
        AfterValue // only appear after the specified hour
    }
    public class WorldClockExistence : EventSubscriberBase
    {
        [SerializeField] private int requiredHour = -1; // -1 means no time restriction
        [SerializeField] private WorldClockShowState showState = WorldClockShowState.OnValue;

        
        // internal 
        private MeshRenderer[] _meshRenderers;
        private Collider[] _objColliders;
        
        protected override void OnEnable()
        {
            base.OnEnable();
            
            _meshRenderers = GetComponentsInChildren<MeshRenderer>();
            _objColliders = GetComponentsInChildren<Collider>();
        }
        protected override void OnWorldClockTicked(int newHour)
        {
            
            // if the required hour is -1, then we want to ignore the world clock and just show the object
            if (requiredHour == -1)
            {
                for (int i = 0; i < _meshRenderers.Length; i++)
                {
                    _meshRenderers[i].enabled = true;
                }
                for (int i = 0; i < _objColliders.Length; i++)
                {
                    _objColliders[i].enabled = true;
                }
                return;
            }
            
            if ((showState == WorldClockShowState.OnValue && newHour == requiredHour) ||
                (showState == WorldClockShowState.BeforeValue && newHour < requiredHour) ||
                (showState == WorldClockShowState.AfterValue && newHour > requiredHour))
            {
                for (int i = 0; i < _meshRenderers.Length; i++)
                {
                    _meshRenderers[i].enabled = true;
                }
                for (int i = 0; i < _objColliders.Length; i++)
                {
                    _objColliders[i].enabled = true;
                }
            }
            else
            {
                for (int i = 0; i < _meshRenderers.Length; i++)
                {
                    _meshRenderers[i].enabled = false;
                }

                for (int i = 0; i < _objColliders.Length; i++)
                {
                    _objColliders[i].enabled = false;
                }
            }
        }
    }
}
