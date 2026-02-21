using UnityEngine;
using System.Collections;

public class fadeTest : MonoBehaviour
{

    // Expose the material to be set for the fade script
    public Material transparentLetterBase;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(FadeOut());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private IEnumerator FadeOut()
    {

    // Set the material to its transparent shader graph counterpart
    Renderer renderer = GetComponent<MeshRenderer>();
    Material[] mats = renderer.materials;
    mats[0] = transparentLetterBase;

    renderer.materials = mats;

    // Initialize variables to be altered
    Renderer objectRenderer = GetComponent<Renderer>();
    Color color = objectRenderer.material.GetColor("_Base_Color");
    Material mat = objectRenderer.material;

    float elapsedTime = 0f;
    float duration = 10f;
    float startAlpha = color.a;
    float targetAlpha = 0f;
    
    // Calculate a smoothed descending alpha value using lerp and replace the old one
    while (elapsedTime < duration)
    {
        elapsedTime += Time.deltaTime;
        float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / duration);
        color.a = newAlpha;
        mat.SetColor("_Base_Color", color);
        yield return null;
    }
    //Ensure we are at 0 alpha by the end of the script
    color.a = targetAlpha;
    mat.SetColor("_Base_Color", color);

    //Finally, destroy the game object
    Destroy(gameObject);
    }
}



