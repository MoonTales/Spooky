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
        public Material[] materialArray;

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
            // Tell the letter system that we have been read
            var id = (_letterType == Types.LetterType.Researcher ? "res_letter_" : "fren_letter_") + GameStateManager.Instance.GetCurrentWorldClockHour();            
            LetterManager.Instance.HandleLetterRead(id);
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
            // disable the collider so we cant interact again
            GetComponent<Collider>().enabled = false;
            StartCoroutine(LetterManager.Instance.ReverseSlideNote(gameObject));
        }



        private void HandleFinishedFriendLetter()
        {
            // due to this being a "fake" letter, we want to have it "vanish"
            GetComponent<Collider>().enabled = false;
            StartCoroutine(FadeOut());
        }


    private IEnumerator FadeOut()
    {
        // Get ALL mesh renderers (this object + all children)
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
        Debug.Log(renderers.Length);
        Debug.Log(materialArray.Length);

        // Replace material element 0 on all renderers with its transparent material counterpart
        Material[] fadeMats = new Material[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] mats = renderers[i].materials;
            mats[0] = materialArray[i];   // array of transparent materials
            renderers[i].materials = mats;     // apply back

            // store for alpha fading
            fadeMats[i] = renderers[i].materials[0];
        }

        // Prepare fade variables
        Color color = fadeMats[0].GetColor("_Base_Color");
        float duration = 2f;
        float elapsedTime = 0f;
        float startAlpha = color.a;
        float targetAlpha = 0f;

        // Fade all materials smoothly using Lerp to interpolate
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, t);

            for (int i = 0; i < fadeMats.Length; i++)
            {
                Color c = fadeMats[i].GetColor("_Base_Color");
                c.a = newAlpha;
                fadeMats[i].SetColor("_Base_Color", c);
            }

            yield return null;
        }

        // Ensure final alpha = 0 so that its smooth
        for (int i = 0; i < fadeMats.Length; i++)
        {
            Color c = fadeMats[i].GetColor("_Base_Color");
            c.a = 0f;
            fadeMats[i].SetColor("_Base_Color", c);
        }

        // Finally, destroy the whole object
        Destroy(gameObject);
    }

    }
}
