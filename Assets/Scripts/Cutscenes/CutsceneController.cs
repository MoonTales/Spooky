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
        [SerializeField] private bool  playOnlyOnce = true;

        private PlayableDirector playableDirector;
        
        private void Start()
        {
            playableDirector = cutsceneToPlay.GetComponent<PlayableDirector>();
            playableDirector.playOnAwake = false;
            if (cutsceneType == CutsceneType.PlayOnStart){PlayCutscene();}
        }

        protected override void OnGameStarted()
        {
            if (cutsceneType == CutsceneType.PlayOnGameStart){PlayCutscene();}
        }
        
        

        private void PlayCutscene()
        {
            CutsceneManager.Instance.OnRequestStartCutscene(this);
        }
        


        public string Prompt { get; }
        public bool CanInteract(Interactor interactor)
        {
            // only allowed to interact during gameplay
            return GameStateManager.Instance.GetCurrentGameState() == Types.GameState.Gameplay && cutsceneType == CutsceneType.PlayOnInteraction;
        }

        public void Interact(Interactor interactor)
        {
            if (cutsceneType == CutsceneType.PlayOnInteraction) { PlayCutscene(); }
        }

        public void CutsceneEnded()
        {
            gameObject.SetActive(false);
            EventBroadcaster.Broadcast_GameStateChanged(Types.GameState.Gameplay);
        }
        
    }
}
