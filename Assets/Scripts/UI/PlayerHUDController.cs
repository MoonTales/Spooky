using System;
using UnityEngine;
using Types = System.Types;

namespace UI
{
    public class PlayerHUDController : Singleton<PlayerHUDController>
    {
    
        private Canvas _hudCanvas;

        protected override void RegisterSubscriptions()
        {
            base.RegisterSubscriptions();
            TrackSubscription(() => EventBroadcaster.OnGameStateChanged += OnGameStateChanged,
                () => EventBroadcaster.OnGameStateChanged -= OnGameStateChanged);
        }
    
        private void Start()
        {
            _hudCanvas = GetComponent<Canvas>();
        }

        private void OnGameStateChanged(Types.GameState newstate)
        {
            switch (newstate)
            {
                case Types.GameState.Gameplay:
                    ShowHUD(true);
                    break;
                case Types.GameState.Cutscene:
                    ShowHUD(false);
                    break;
                case Types.GameState.MainMenu:
                    ShowHUD(false);
                    break;
            }
        }
    
        private void ShowHUD(bool show)
        {
            if (_hudCanvas != null){_hudCanvas.enabled = show;}
        }
    }
}
