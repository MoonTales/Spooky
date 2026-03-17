using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// This will be attached to a UI Sprite Image element, and will loop through all of the sprites within a list
    /// in a frame independant manner, to emulate an animated UI element
    /// </summary>
    public class UiSpriteAnimator : MonoBehaviour
    {

        // the list of sprites to loop through, which will be set in the inspector
        [SerializeField] private Sprite[] sprites;
        [SerializeField] private float animationSpeed = 1f; // the speed at which to loop through the sprites, in frames per second
        
        // internal
        private Image _image;
        
        
        public void OnEnable()
        {
            _image = GetComponent<Image>();
            StartCoroutine(AnimateSprite());
            
        }
        
        public void OnDisable()
        {
            StopAllCoroutines();
        }

        private IEnumerator AnimateSprite()
        {
            int currentSpriteIndex = 0;
            float timer = 0f;
            float timePerFrame = 1f / animationSpeed;

            while (true)
            {
                timer += Time.unscaledDeltaTime;

                if (timer >= timePerFrame)
                {
                    timer -= timePerFrame;

                    _image.sprite = sprites[currentSpriteIndex];
                    currentSpriteIndex = (currentSpriteIndex + 1) % sprites.Length;
                }

                yield return null; // run every frame
            }
        }
    }
}
