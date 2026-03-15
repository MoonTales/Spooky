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
        [Tooltip("Set this to true if you just want this game object to become active, instead of all the meshes and colliders of its children")]
        [SerializeField] private bool gameObjectAlternative = false; // Need this for my nightmare realms stuff because I need the gamobject to toggle,
                                                                     // not all meshes and colliders  -Brayden
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
                if (gameObjectAlternative)
                {
                    gameObject.SetActive(true);
                }
                else
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
                return;
            }
            
            if ((showState == WorldClockShowState.OnValue && newHour == requiredHour) ||
                (showState == WorldClockShowState.BeforeValue && newHour < requiredHour) ||
                (showState == WorldClockShowState.AfterValue && newHour > requiredHour))
            {
                if (gameObjectAlternative)
                {
                    gameObject.SetActive(true);
                }
                else
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
            }
            else
            {
                if (gameObjectAlternative)
                {
                    gameObject.SetActive(false);
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
}
