using UnityEngine;
using System;
using System.Collections;
using Inspection;
using Managers;
using Types = System.Types;

namespace Interaction.Letters
{
    
    public class Letter : InspectableObject, IInteractable
    {
        
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        
        private Types.LetterType _letterType; public void SetLetterType(Types.LetterType letterType) { _letterType = letterType; } public Types.LetterType GetLetterType() { return _letterType; }
        private bool _hasBeenWrittenOn = false; public void SetHasBeenWrittenOn(bool value) { _hasBeenWrittenOn = value; } public bool GetHasBeenWrittenOn() { return _hasBeenWrittenOn; }
        
        // Expose the material to be set for the fade script
        public Material transparentLetterBase;

        private void Start()
        {
            // the only times will be either Act (1), Act (2), or Act (3) (for now)
            if (_letterType == Types.LetterType.Researcher)
            {
                // Now we can set the promtkey and the rowKey. this also needs to be hooked up to take in a world clock hour
                promptKey = new TextKey { place = "prompt", id = "res_letter" };
                rowKey = new TextKey { place = "bedroom", id = "res_letter" };
            }
            else if (_letterType == Types.LetterType.Friend)
            {
                promptKey = new TextKey { place = "prompt", id = "fren_letter" };
                rowKey = new TextKey { place = "bedroom", id = "fren_letter" };
            }
        }
        
        public void SetResponseTextKey()
        {
            rowKey = new TextKey { place = "bedroom", id = "respond_letter" };
        }

        
        public new void Interact(Interactor interactor)
        {
            InspectionSystem.Instance.StartInspection(gameObject);
        }
        
        public override void OnReturnedToOriginalPosition()
        {
            if (_letterType == Types.LetterType.Researcher)
            {
                HandleFinishedResearcherLetter();
            }
            else if (_letterType == Types.LetterType.Friend)
            {
                HandleFinishedFriendLetter();
            }
        }

        private void HandleFinishedResearcherLetter()
        {
            // this will actually be the opposite effect, where it will slide back to the original position it slid in from, and then destroy itself
            StartCoroutine(LetterManager.Instance.ReverseSlideNote(gameObject));
        }



        private void HandleFinishedFriendLetter()
        {
            // due to this being a "fake" letter, we want to have it "vanish"

            StartCoroutine(FadeOut());


        }


        private IEnumerator FadeOut()
        {

        // Set the material to its transparent shader graph counterpart
        Renderer renderer = GetComponent<MeshRenderer>();
        Material[] mats = renderer.materials;
        mats[0] = transparentLetterBase;

        renderer.materials = mats;

        // Initialize variables to be altered
        Renderer objectRenderer = GetComponent<Renderer>();
        Color color = objectRenderer.material.GetColor("_Base_Color");
        Material mat = objectRenderer.material;

        float elapsedTime = 0f;
        float duration = 10f;
        float startAlpha = color.a;
        float targetAlpha = 0f;

        // Calculate a smoothed descending alpha value using lerp and replace the old one
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / duration);
            color.a = newAlpha;
            mat.SetColor("_Base_Color", color);
            yield return null;
        }
        //Ensure we are at 0 alpha by the end of the script
        color.a = targetAlpha;
        mat.SetColor("_Base_Color", color);

        //Finally, destroy the game object
        Destroy(gameObject);
        }

    }
}
