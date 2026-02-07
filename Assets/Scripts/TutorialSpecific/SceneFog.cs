using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public class SceneFog : MonoBehaviour
{
    public MirzaBeig.VolumetricFogLite.VolumetricFogRendererFeatureLite theRenderer;

    public Material fogMaterial;

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Switch asset based on scene name
        theRenderer.settings.fogMaterial = fogMaterial;
    }
}
