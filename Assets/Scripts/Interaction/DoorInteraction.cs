using System;
using System.Collections;
using Managers;
using UnityEngine;

namespace Interaction
{
    public class DoorInteraction : MonoBehaviour, IInteractable
    {

        [SerializeField] private GameObject doorHandle;
        [SerializeField] private float shakeDuration = 0.3f;
        [SerializeField] private float shakeDurationVariance = 0.1f;
        [SerializeField] private float shakeAmount = 5f;
        [SerializeField] private float shakeAmountVariance = 2f;

        // Internal
        private bool _isShaking = false;

        private void Start()
        {
            // we are gonna add some variance to the doors variables to
            // this is just gonna ensure every door feels slightly different
            shakeDuration += UnityEngine.Random.Range(-shakeDurationVariance, shakeDurationVariance);
            shakeAmount += UnityEngine.Random.Range(-shakeAmountVariance, shakeAmountVariance);
        }

        // ---------------------------------
        // IInteractable implementation
        // ---------------------------------
        public TextKey PromptKey { get; }
        public bool CanInteract(Interactor interactor)
        {
            // we can not interact while the handle is shaking
            return !_isShaking;
        }

        public void Interact(Interactor interactor)
        {
            // <> SFX HERE <>
            AudioManager.Instance.PlayUiHoverSfx();
            // --------------
            // "shake" the door handle
            StartCoroutine(ShakeHandle());
        }
        
        
        private IEnumerator ShakeHandle()
        {
            _isShaking = true;
            // store our original rotation so we can reset it after shaking
            Quaternion originalRotation = doorHandle.transform.localRotation;
            float elapsed = 0f;

            while (elapsed < shakeDuration)
            {
                // Determine the angles of the shake (we love sin waves)
                float angle = Mathf.Sin(elapsed * 40f) * shakeAmount * (1f - elapsed / shakeDuration);
                float angleY = Mathf.Sin(elapsed * 40f) * shakeAmount * (1f - elapsed / shakeDuration);
                doorHandle.transform.localRotation = originalRotation * Quaternion.Euler(0f, angle, angleY);
                elapsed += Time.deltaTime;
                yield return null;
            }

            //reset!
            doorHandle.transform.localRotation = originalRotation;
            _isShaking = false;
        }
    }
}
