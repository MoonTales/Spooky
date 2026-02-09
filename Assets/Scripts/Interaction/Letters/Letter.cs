using UnityEngine;
using System;
using Inspection;
using Managers;
using Types = System.Types;

namespace Interaction.Letters
{
    
    public class Letter : InspectableObject, IInteractable
    {
        
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        
        private Types.LetterType _letterType; public void SetLetterType(Types.LetterType letterType) { _letterType = letterType; } public Types.LetterType GetLetterType() { return _letterType; }
        
        
        

        private void Start()
        {
            // when the letter spawns in, we are gonna check a few things, and set the information about the letter accordingly;
            int currentHour = GameStateManager.Instance.GetCurrentWorldClockHour();
            // the only times will be either Act (1), Act (2), or Act (3) (for now)
            switch (currentHour)
            {
                case 1:
                    HandleActOneLetter();
                    break;
                case 2:
                    HandleActTwoLetter();
                    break;
                case 3:
                    HandleActThreeLetter();
                    break;
                default:
                    DebugUtils.LogWarning("Letter spawned at an unexpected hour!");
                    break;
            }
        }

        
        // these could also just stay as seperate things
        private void HandleActOneLetter()
        {
            // depending on if this is a friend letter or a researcher letter, we will set the letter type and the prompt key accordingly
            if (_letterType == Types.LetterType.Researcher)
            {
                // Now we can set the promtkey and the rowKey. this also needs to be hooked up to take in a world clock hour
                promptKey = new TextKey { place = "Bedroom", id = "ResearcherLetterPrompt" };
                rowKey = new TextKey { place = "Bedroom", id = "ResearcherLetterRow" };
            }
            else if (_letterType == Types.LetterType.Friend)
            {
                promptKey = new TextKey { place = "Bedroom", id = "FriendLetterPrompt" };
                rowKey = new TextKey { place = "Bedroom", id = "FriendLetterRow" };
            }
        }

        private void HandleActTwoLetter()
        {
            // depending on if this is a friend letter or a researcher letter, we will set the letter type and the prompt key accordingly
            if (_letterType == Types.LetterType.Researcher)
            {
                // Now we can set the promtkey and the rowKey. this also needs to be hooked up to take in a world clock hour
                promptKey = new TextKey { place = "Bedroom", id = "ResearcherLetterPrompt" };
                rowKey = new TextKey { place = "Bedroom", id = "ResearcherLetterRow" };
            }
            else if (_letterType == Types.LetterType.Friend)
            {
                promptKey = new TextKey { place = "Bedroom", id = "FriendLetterPrompt" };
                rowKey = new TextKey { place = "Bedroom", id = "FriendLetterRow" };
            }
        }
        
        private void HandleActThreeLetter()
        {
            // depending on if this is a friend letter or a researcher letter, we will set the letter type and the prompt key accordingly
            if (_letterType == Types.LetterType.Researcher)
            {
                // Now we can set the promtkey and the rowKey. this also needs to be hooked up to take in a world clock hour
                promptKey = new TextKey { place = "Bedroom", id = "ResearcherLetterPrompt" };
                rowKey = new TextKey { place = "Bedroom", id = "ResearcherLetterRow" };
            }
            else if (_letterType == Types.LetterType.Friend)
            {
                promptKey = new TextKey { place = "Bedroom", id = "FriendLetterPrompt" };
                rowKey = new TextKey { place = "Bedroom", id = "FriendLetterRow" };
            }
        }



        public void Interact(Interactor interactor)
        {
            if (_letterType == Types.LetterType.Researcher)
            {
                Types.NotificationData data = new(
                    duration: 3.0f, 
                    messageKey: new TextKey { place = "Letters", id = "LetterContent1"},
                    messageOverride: "This is a researcher's letter!"
                );
                data.Send();
            }
            else if (_letterType == Types.LetterType.Friend)
            {
                Types.NotificationData data = new(
                    duration: 3.0f, 
                    messageKey: new TextKey { place = "Letters", id = "LetterContent2"},
                    messageOverride: "This is a friend's letter!"
                );
                data.Send();
            }
            InspectionSystem.Instance.StartInspection(gameObject);
        }

        public override void OnInspectionFinished()
        {
            // Custom logic that can run once the inspection has been completed fully
            DebugUtils.Log($"Finished inspecting letter: !!");
        }
    }
}
