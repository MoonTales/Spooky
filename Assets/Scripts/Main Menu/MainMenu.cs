using UnityEngine;

public class MainMenu : MonoBehaviour
{
    public void Play()
    {
        // i'm blanking on how to give the script access to sceneswapper cause rn its causing an error
        //SceneSwapper.Instance.SwapScene("Cohen");
    }

    public void Quit()
    {
        Application.Quit();
    }
}
