using System;
using UnityEngine;
using Types = System.Types;

namespace UI
{
    public class PlayerHUDController : Singleton<PlayerHUDController>
    {
    
        // Internal References to the HUD
        private Canvas _hudCanvas;

        
        private void Start()
        {
            _hudCanvas = GetComponent<Canvas>();
        }

        protected override void OnGameStateChanged(Types.GameState newstate)
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
                case Types.GameState.Inspecting:
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


