using System;
using Cutscenes;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;
using Types = System.Types;

namespace Managers
{
    /// <summary>
    /// Class used to primary handle logic relating to cutscenes within the game, such as playing cutscenes, skipping cutscenes, and transitioning between cutscenes and gameplay.
    /// all prefabs within the world that wish to play cutscenes, will interact with this.
    /// </summary>
    public class CutsceneManager : Singleton<CutsceneManager>
    {
        
        // this is the current cutscene that is playing, if any. this is used to prevent multiple cutscenes from playing at once, and to allow for skipping cutscenes.
        private CutsceneController _currentCutscene;
        private PlayableDirector _playableDirector;
        

        
        public void OnRequestStartCutscene(CutsceneController cutsceneController)
        {
            // ensure we dont already have a cutscene playing
            if (_currentCutscene) { return; }
            
            // set our internal references for the cutscene
            _currentCutscene = cutsceneController;
            // the playable director will be in a child object of the cutscene controller, so we need to get it from there
            
            _playableDirector = _currentCutscene.GetComponentInChildren<PlayableDirector>(true);
            
            if (!_playableDirector) { return; }
            //--

            // Subscribe to events relating to the cutscene
            _playableDirector.stopped += OnCutsceneEnd;
            
            // FORCE SNAP for bedroom cutscene
            var brain = Camera.main.GetComponent<CinemachineBrain>();
            brain.DefaultBlend = new CinemachineBlendDefinition(
                CinemachineBlendDefinition.Styles.Cut, 0f
            );
            
            // we can start the cutscene now,
            _playableDirector.gameObject.SetActive(true);
            _playableDirector.Play();
            // Edge case for the credits Scene
            if (_currentCutscene.UseGameStateOverride)
            {
                EventBroadcaster.Broadcast_GameStateChanged(_currentCutscene.GameStateOverride);
            }
            else
            {
                EventBroadcaster.Broadcast_GameStateChanged(Types.GameState.Cutscene);
            }
            
        }
        
        public void OnRequestSkipCutscene()
        {
            if (!_currentCutscene || !_playableDirector) { return; }
            _currentCutscene.CutsceneEnded();
        }

        private void OnCutsceneEnd(PlayableDirector obj)
        {
            // un-subscribe from this
            _playableDirector.stopped -= OnCutsceneEnd;
            
            // Restore the blend settings to default
            var brain = Camera.main.GetComponent<CinemachineBrain>();
            brain.DefaultBlend = new CinemachineBlendDefinition(
                CinemachineBlendDefinition.Styles.EaseInOut, 1f
            );
            
            // ensure that the current GameState isnt main menu, otherwise this is a false trigger
            if (GameStateManager.Instance && GameStateManager.Instance.GetCurrentGameState() == Types.GameState.MainMenu) { return; }
            _currentCutscene.CutsceneEnded();
            _playableDirector.Stop();
            _currentCutscene = null;
            //EventBroadcaster.Broadcast_GameStateChanged(Types.GameState.Gameplay);
        }
    }
}
