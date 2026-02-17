using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Types = System.Types;

namespace Managers
{
    /// <summary>
    /// Class used to manage screen fading functionality, such as fading to black, fading from black
    ///
    /// We will be able to request a screen fade, along with subscribing to an event to be called once the fade IN is complete, and when the fade OUT is complete.
    /// </summary>
    
    
    public class ScreenFadeManager : Singleton<ScreenFadeManager>
    {
        
        // this will load in a "canvas" from the resources folder, which will be used to fade the screen in and out
        private GameObject _screenFadeCanvas;
        private Image _fadeImage;
        private bool _isFading = false;

        
        private void Start()
        {
            // load in the screen fade canvas from the resources folder
            _screenFadeCanvas = Instantiate(Resources.Load<GameObject>("UI/ScreenFadeCanvas"), transform, true);

            // Get the Image component from the canvas
            _fadeImage = _screenFadeCanvas.GetComponentInChildren<Image>();
            
            // Start with transparent image
            Color color = _fadeImage.color;
            _fadeImage.color = new Color(color.r, color.g, color.b, 0f);
            _fadeImage.raycastTarget = false;
            _screenFadeCanvas.SetActive(true);
        }
        
        protected override void RegisterSubscriptions()
        {
            base.RegisterSubscriptions();
            TrackSubscription(() => EventBroadcaster.OnRequestScreenFade += OnRequestScreenFade,
                () => EventBroadcaster.OnRequestScreenFade -= OnRequestScreenFade);
        }

        private void OnRequestScreenFade(Types.ScreenFadeData screenFadeData)
        {
            if (!_isFading && _fadeImage != null)
            {
                StartCoroutine(FadeSequence(screenFadeData));
            }
        }

        private IEnumerator FadeSequence(Types.ScreenFadeData fadeData)
        {
            _isFading = true;

            // Fade to black (Fade Out)
            yield return StartCoroutine(FadeToBlack(fadeData.GetFadeOutDuration()));
            OnScreenFadeOutComplete(fadeData.GetOnFadeOutComplete());

            // Pause for a set time
            yield return new WaitForSeconds(fadeData.GetFadeDuration());
            
            // Fade to clear (Fade Out)
            yield return StartCoroutine(FadeToClear(fadeData.GetFadeInDuration()));
            OnScreenFadeInComplete(fadeData.GetOnFadeInComplete());

            _isFading = false;
        }

        private IEnumerator FadeToBlack(float duration)
        {
            float elapsed = 0f;
            Color color = _fadeImage.color;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Clamp01(elapsed / duration);
                _fadeImage.color = new Color(color.r, color.g, color.b, alpha);
                yield return null;
            }

            // Ensure fully solid
            _fadeImage.color = new Color(color.r, color.g, color.b, 1f);
        }

        private IEnumerator FadeToClear(float duration)
        {
            float elapsed = 0f;
            Color color = _fadeImage.color;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = 1f - Mathf.Clamp01(elapsed / duration);
                _fadeImage.color = new Color(color.r, color.g, color.b, alpha);
                yield return null;
            }

            // Ensure fully transparent
            _fadeImage.color = new Color(color.r, color.g, color.b, 0f);
        }

        private void OnScreenFadeInComplete(Action onComplete)
        {
            onComplete?.Invoke();
        }

        private void OnScreenFadeOutComplete(Action onComplete)
        {
            onComplete?.Invoke();
        }

    }
}