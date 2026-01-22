using System;
using UnityEngine;

public class SceneTeleportInteraction : MonoBehaviour, IInteractable
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    [SerializeField] private string sceneName = "";
    
    public string Prompt { get; }
    public bool CanInteract(Interactor interactor)
    {
        return true;
    }

    public void Interact(Interactor interactor)
    {
        SceneSwapper.Instance.SwapScene(sceneName);
    }

    public AudioClip HoverSfx { get; }
    public AudioClip InteractSfx { get; }
    public AudioClip DeniedSfx { get; }
}
