using System;
using Player;
using TMPro;
using UnityEngine;
using Types = System.Types;

namespace UI
{
    public class PlayerHUDController : Singleton<PlayerHUDController>
    {
    
        // Internal References to the HUD
        private Canvas _hudCanvas;
        // textmeshpro Text ui
        private TMP_Text _hudSanityStateText;
        private TMP_Text _hudSanityValueText;

        
        private void Start()
        {
            _hudCanvas = GetComponent<Canvas>();
            _hudSanityStateText = transform.Find("SanityState").GetComponent<TMP_Text>();
            _hudSanityValueText = transform.Find("SanityValue").GetComponent<TMP_Text>();
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

        private void Update()
        {
            if (_hudSanityStateText)
            {
                _hudSanityStateText.text = PlayerStats.Instance.GetPlayerStats().GetPlayerMentalState().ToString();
            }
            if (_hudSanityValueText)
            {
                _hudSanityValueText.text = Mathf.RoundToInt(PlayerStats.Instance.GetPlayerStats().GetCurrentMentalHealth()).ToString();
            }
        }
    }
}


