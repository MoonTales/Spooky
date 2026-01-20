using UnityEngine;
using System;
using Types = System.Types;

public class MainMenu : MonoBehaviour
{
    public void Play()
    {
        EventBroadcaster.Broadcast_GameStateChanged(Types.GameState.Gameplay);
        SceneSwapper.Instance.SwapScene("Bedroom");
    }

    public void Settings()
    {
        
    }

    public void Quit()
    {
        Application.Quit();
    }
}
