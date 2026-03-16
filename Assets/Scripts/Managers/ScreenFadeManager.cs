using System;
using System.Collections;
using System.Collections.Generic;
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
        public static bool IsFadeInProgress { get; private set; }
        
        // this will load in a "canvas" from the resources folder, which will be used to fade the screen in and out
        private GameObject _screenFadeCanvas;
        private Image _fadeImage;
        private bool _isFading = false;
        
        // ICONS
        private Image _ICON_Save_Image;
        private Image _ICON_Load_Image;
        
        // FIX: this is a queue to avoid the case of multiple screenfades called at the same time
        private readonly Queue<Types.ScreenFadeData> _fadeQueue = new Queue<Types.ScreenFadeData>();
        
        public void DisplaySaveIconForDuration(float duration)
        {
            Debug.Log($"Displaying save icon for duration: {duration}");
            // enable the save icon, and then disable it after the duration
            if (_ICON_Save_Image == null)
            {
                Debug.LogWarning("Save icon image reference is null. Cannot display save icon.");
                return;
            }
            StartCoroutine(DisplayIconForDuration(_ICON_Save_Image, duration));
        }
        public void DisplayLoadIconForDuration(float duration)
        {
            // enable the load icon, and then disable it after the duration
            if (_ICON_Load_Image == null) { return;}
            StartCoroutine(DisplayIconForDuration(_ICON_Load_Image, duration));
        }
        
        private IEnumerator DisplayIconForDuration(Image iconImage, float duration = 3, float fadeDuration = 0.5f)
        {
            // Fade in
            iconImage.enabled = true;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Clamp01(elapsed / fadeDuration);
                iconImage.color = new Color(iconImage.color.r, iconImage.color.g, iconImage.color.b, alpha);
                yield return null;
            }

            // Hold at full opacity
            yield return new WaitForSeconds(duration);

            // Fade out
            elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Clamp01(1f - (elapsed / fadeDuration));
                iconImage.color = new Color(iconImage.color.r, iconImage.color.g, iconImage.color.b, alpha);
                yield return null;
            }

            iconImage.enabled = false;
        }

        
        private void Start()
        {
            // load in the screen fade canvas from the resources folder
            _screenFadeCanvas = Instantiate(Resources.Load<GameObject>("UI/ScreenFadeCanvas"), transform, true);

            // Get the Image component from the canvas
            _fadeImage = _screenFadeCanvas.GetComponentInChildren<Image>();
            // loop through all the children of the canvas to find the icons
            Image[] allImages = _screenFadeCanvas.GetComponentsInChildren<Image>();
            foreach (Image image in allImages)
            {
                if (image.name == "ICON_Save")
                {
                    _ICON_Save_Image = image;
                    _ICON_Save_Image.enabled = false;
                }
                else if (image.name == "ICON_Load")
                {
                    _ICON_Load_Image = image;
                    _ICON_Load_Image.enabled = false;
                }
            }
            
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
            TrackSubscription(() => EventBroadcaster.OnRequestScreenFadeScreenSwap += OnRequestScreenFadeScreenSwap,
                () => EventBroadcaster.OnRequestScreenFadeScreenSwap -= OnRequestScreenFadeScreenSwap);
        }

        private void OnRequestScreenFadeScreenSwap(Types.ScreenFadeSceneTransitionData screenfadedata)
        {
            // this will
            //1. Fade out (call the OnFadeOutComplete event once the fade out is complete)
            //2. Load the new scene (call the OnSceneLoadComplete event once the new scene is loaded)
            //3. Fade in (call the OnFadeInComplete event once the fade in is complete)
            if (_fadeImage == null) { return;}
            if (_isFading)
            {
                // Disregard any new screen swap requests
                Debug.LogWarning("Screen fade already in progress. New screen swap request disregarded.");
            }
            else
            {
                _isFading = true;
                IsFadeInProgress = true;
                StartCoroutine(FadeScreenSwapSequence(screenfadedata));
            }
            
            
        }

        private IEnumerator FadeScreenSwapSequence(Types.ScreenFadeSceneTransitionData screenfadedata)
        {
            
            // Fade to Black
            Debug.Log("Starting fade to black for screen swap...");
            yield return StartCoroutine(FadeToBlack(screenfadedata.GetFadeOutDuration()));
            OnScreenFadeOutComplete(screenfadedata.GetOnFadeOutComplete());
            
            if (_ICON_Load_Image != null) { _ICON_Load_Image.enabled = true; }
            // Load in the new scene, and pause untill we are done loading
            Debug.Log("Loading new scene: " + screenfadedata.GetSceneToTransitionTo());
            yield return StartCoroutine(SceneSwapper.Instance.LoadSceneAsync(screenfadedata.GetSceneToTransitionTo()));
            OnScreenFadeDurationComplete(screenfadedata.GetOnSceneLoaded());
            yield return new WaitForSeconds(1); // slight buffer to account for any potential loading hiccups
            if (_ICON_Load_Image != null) { _ICON_Load_Image.enabled = false; }
            // Fade to clear
            Debug.Log("Starting fade to clear for screen swap...");
            yield return StartCoroutine(FadeToClear(screenfadedata.GetFadeInDuration()));
            _isFading = false;
            IsFadeInProgress = false;
            Debug.Log("Screen swap fade sequence complete.");
        }

        private void OnRequestScreenFade(Types.ScreenFadeData screenFadeData)
        {

            if (_fadeImage == null) { return;}
            
            if (_isFading)
            {
                _fadeQueue.Enqueue(screenFadeData);
            }
            else
            {
                _isFading = true;
                IsFadeInProgress = true;
                StartCoroutine(FadeSequence(screenFadeData));
            }
        }

        private IEnumerator FadeSequence(Types.ScreenFadeData fadeData)
        {
            // Fade to black (Fade Out)
            yield return StartCoroutine(FadeToBlack(fadeData.GetFadeOutDuration()));
            OnScreenFadeOutComplete(fadeData.GetOnFadeOutComplete());

            // Pause for a set time
            yield return new WaitForSeconds(fadeData.GetFadeDuration());
            OnScreenFadeDurationComplete(fadeData.GetOnFadeDurationComplete());
            
            // Fade to clear (Fade Out)
            yield return StartCoroutine(FadeToClear(fadeData.GetFadeInDuration()));
            OnScreenFadeInComplete(fadeData.GetOnFadeInComplete());

            _isFading = false;
            IsFadeInProgress = false;
            // Check the queue
            if (_fadeQueue.Count > 0)
            {
                StartCoroutine(FadeSequence(_fadeQueue.Dequeue()));
            }
        }

        protected override void OnDestroy()
        {
            IsFadeInProgress = false;
            _fadeQueue.Clear();
            base.OnDestroy();
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

        private void OnScreenFadeInComplete(Action onComplete) { onComplete?.Invoke(); }
        private void OnScreenFadeOutComplete(Action onComplete) { onComplete?.Invoke(); }
        private void OnScreenFadeDurationComplete(Action onComplete) { onComplete?.Invoke(); }

    }
}
