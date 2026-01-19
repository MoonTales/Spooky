using System;
using UnityEngine;
using Types = System.Types;

namespace Cutscenes
{
    public class CutsceneController : MonoBehaviour
    {

        [SerializeField] private GameObject _cutsceneToPlay;

        
        private void Start()
        {
            PlayCutscene();
        }
        
        

        private void PlayCutscene()
        {
            EventBroadcaster.Broadcast_GameStateChanged(Types.GameState.Cutscene);
            _cutsceneToPlay.SetActive(true);
        
        }
    
        public void EndCutscene()
        {
            EventBroadcaster.Broadcast_GameStateChanged(Types.GameState.Gameplay);
            _cutsceneToPlay.SetActive(false);
        }
    }
}
