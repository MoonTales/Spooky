using UnityEngine;
using System.Collections;
using System;

public class fadeTest2 : MonoBehaviour
{

    // Expose the material to be set for the fade script
    public Material transparentLetterBase;
    public Material transparentLetterTop;

    public Material[] materialArray;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //materialArray = 
        StartCoroutine(FadeOut());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private IEnumerator FadeOut()
    {
        // Get ALL mesh renderers (this object + all children)
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
        Debug.Log(renderers.Length);
        Debug.Log(materialArray.Length);

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

        // Prepare fade variables
        Color color = fadeMats[0].GetColor("_Base_Color");
        float duration = 10f;
        float elapsedTime = 0f;
        float startAlpha = color.a;
        float targetAlpha = 0f;

        // Fade all materials smoothly using Lerp to interpolate
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, t);

            for (int i = 0; i < fadeMats.Length; i++)
            {
                Color c = fadeMats[i].GetColor("_Base_Color");
                c.a = newAlpha;
                fadeMats[i].SetColor("_Base_Color", c);
            }

            yield return null;
        }

        // Ensure final alpha = 0 so that its smooth
        for (int i = 0; i < fadeMats.Length; i++)
        {
            Color c = fadeMats[i].GetColor("_Base_Color");
            c.a = 0f;
            fadeMats[i].SetColor("_Base_Color", c);
        }

        // Finally, destroy the whole object
        Destroy(gameObject);
    }
}



