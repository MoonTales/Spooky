using System;
using Managers;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Serialization;
using Types = System.Types;

namespace Cutscenes
{
    [Serializable]
    enum CutsceneType
    {
        PlayOnStart, // this is anytime Start() is called
        PlayOnGameStart, // this is anything OnGameStarted() is called
        PlayOnInteraction, // this is anytime the player interacts with this object
    }
    public class CutsceneController : EventSubscriberBase, IInteractable
    {
        public TextKey PromptKey => default;
        
        
        [SerializeField] private GameObject cutsceneToPlay;
        [SerializeField] private CutsceneType cutsceneType;
        //[SerializeField] private bool  playOnlyOnce = true;
        
        private PlayableDirector _playableDirector;
        private CutsceneSignalReceiver _cutsceneSignalReceiver;
        
        
        private void Start()
        {
            if (cutsceneType == CutsceneType.PlayOnStart){CutsceneManager.Instance.OnRequestStartCutscene(this);}

            _playableDirector = GetComponentInChildren<PlayableDirector>(true);
    
            if (_playableDirector != null)
            {
                _cutsceneSignalReceiver = _playableDirector.gameObject.GetComponent<CutsceneSignalReceiver>();
                if (_cutsceneSignalReceiver == null)
                {
                    _cutsceneSignalReceiver = _playableDirector.gameObject.AddComponent<CutsceneSignalReceiver>();
                }
                
                _playableDirector.SetGenericBinding(_cutsceneSignalReceiver, _cutsceneSignalReceiver);
            }
        }
        
        protected override void OnGameStarted()
        {
            if (cutsceneType == CutsceneType.PlayOnGameStart){CutsceneManager.Instance.OnRequestStartCutscene(this);}
        }
        
        
        public bool CanInteract(Interactor interactor)
        {
            // only allowed to interact during gameplay
            return GameStateManager.Instance.GetCurrentGameState() == Types.GameState.Gameplay && cutsceneType == CutsceneType.PlayOnInteraction;
        }

        public void Interact(Interactor interactor)
        {
            if (cutsceneType == CutsceneType.PlayOnInteraction) { CutsceneManager.Instance.OnRequestStartCutscene(this); }
        }

        public void CutsceneEnded()
        {
            gameObject.SetActive(false);
            EventBroadcaster.Broadcast_GameStateChanged(Types.GameState.Gameplay);
        }
    }
}
