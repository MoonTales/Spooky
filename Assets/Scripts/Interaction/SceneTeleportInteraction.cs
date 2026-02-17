using System;
using Managers;
using UnityEngine;

public class SceneTeleportInteraction : MonoBehaviour, IInteractable
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    [SerializeField] private string sceneName = "";
    public TextKey PromptKey => default;

    public string Prompt { get; }
    public bool CanInteract(Interactor interactor)
    {
        return true;
    }

    public void Interact(Interactor interactor)
    {
        SceneSwapper.Instance.SwapScene(sceneName);
        GameStateManager.Instance.SetCurrentZoneId(-1);
    }

    public AudioClip HoverSfx { get; }
    public AudioClip InteractSfx { get; }
    public AudioClip DeniedSfx { get; }
}
