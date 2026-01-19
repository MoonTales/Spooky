using UnityEngine;

public class CutsceneController : MonoBehaviour
{

    [SerializeField] private GameObject _cutsceneToPlay;

    void Update()
    {
        // Soon hookup to soething else
        if (Input.GetKeyDown(KeyCode.T))
        {
            PlayCutscene();
        }
        
    }

    private void PlayCutscene()
    {
        _cutsceneToPlay.SetActive(true);
        
    }
    
    public void EndCutscene()
    {
        _cutsceneToPlay.SetActive(false);
    }
}
