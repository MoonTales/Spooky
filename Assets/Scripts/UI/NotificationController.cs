using System;
using System.Collections;
using TMPro;
using UnityEngine;
using Types = System.Types;

namespace UI
{
    /// <summary>
    /// This class will handle and connect to the HUD, allowing any class in the game
    /// to create "pop up notifications" on the screen for the player to see.
    /// </summary>

    public class NotificationController : Singleton<NotificationController>
    {

        // Internal Variables:
        // we are gonna steal this off the hud, and manually control the text
        private TMP_Text _notificationText;
        private float _fadeInTimer = 1f;  // Duration of fade in
        private float _fadeOutTimer = 1f; // Duration of fade out
        private bool _isFading = false;
        
        protected override void RegisterSubscriptions()
        {
            base.RegisterSubscriptions();
            // Listen for the notification broadcast
            TrackSubscription(() => EventBroadcaster.OnNotificationSent += OnNotificationSent,
                () => EventBroadcaster.OnNotificationSent -= OnNotificationSent);
        }

        private void OnNotificationSent(Types.NotificationData notificationData)
        {
            // get access to the hud notification text
            if (!_notificationText) { _notificationText = PlayerHUDController.Instance.GetNotificationText(); }
            // if its still null we have a problem
            if (!_notificationText){DebugUtils.LogError("NotificationController could not find the notification text on the HUD!"); return;}
            
            // Stop any existing notification coroutine
            StopAllCoroutines();
            
            // now we can handle the popups of the Notifications on the screen
            DebugUtils.Log($"NotificationController received notification: {notificationData.ToString()}");
            
            // enable the notification text
            _notificationText.gameObject.SetActive(true);
            
            // set the text to the notification message
            _notificationText.text = notificationData.messageOverride;
            
            // Start the full notification sequence
            StartCoroutine(ShowNotificationSequence(notificationData.duration));
        }

        private IEnumerator ShowNotificationSequence(float displayDuration)
        {
            // Fade in
            yield return StartCoroutine(FadeInText());
            
            // Wait for display duration
            yield return new WaitForSeconds(displayDuration);
            
            // Fade out
            yield return StartCoroutine(FadeOutText());
            
            // Hide the notification text
            if (_notificationText)
            {
                _notificationText.gameObject.SetActive(false);
            }
        }

        private IEnumerator FadeInText()
        {
            _isFading = true;
            float elapsedTime = 0f;
            Color textColor = _notificationText.color;
            
            // Start from fully transparent
            textColor.a = 0f;
            _notificationText.color = textColor;
            
            while (elapsedTime < _fadeInTimer)
            {
                elapsedTime += Time.deltaTime;
                textColor.a = Mathf.Lerp(0f, 1f, elapsedTime / _fadeInTimer);
                _notificationText.color = textColor;
                yield return null;
            }
            
            // Ensure we end at full opacity
            textColor.a = 1f;
            _notificationText.color = textColor;
            _isFading = false;
        }

        private IEnumerator FadeOutText()
        {
            _isFading = true;
            float elapsedTime = 0f;
            Color textColor = _notificationText.color;
            
            while (elapsedTime < _fadeOutTimer)
            {
                elapsedTime += Time.deltaTime;
                textColor.a = Mathf.Lerp(1f, 0f, elapsedTime / _fadeOutTimer);
                _notificationText.color = textColor;
                yield return null;
            }
            
            // Ensure we end at full transparency
            textColor.a = 0f;
            _notificationText.color = textColor;
            _isFading = false;
        }
    }
}