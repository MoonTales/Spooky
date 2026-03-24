using System;
using System.Collections;
using Managers;
using Player;
using Unity.Cinemachine;
using UnityEngine;
using Types = System.Types;
using Interaction;

public class PavementSwap : MonoBehaviour
{
    // Expose the material to be set for the swap script
    public Material[] materialArray;


    void Start()
    {
        StartCoroutine(SwapMaterials());
    }


    IEnumerator SwapMaterials()
    {
        if (GameStateManager.Instance.GetCurrentWorldClockHour() == 2)
        {
            // Get ALL mesh renderers (this object + all children)
            MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();

            // Replace material element 0 on all renderers with its transparent material counterpart
            Material[] fadeMats = new Material[renderers.Length];

            for (int i = 0; i < renderers.Length; i++)
            {
                Material[] mats = renderers[i].materials;
                mats[0] = materialArray[i];   // array of transparent materials
                renderers[i].materials = mats;     // apply back

                // store for alpha fading
                fadeMats[i] = renderers[i].materials[0];
            }


            yield return null;            
        }
    }
}
