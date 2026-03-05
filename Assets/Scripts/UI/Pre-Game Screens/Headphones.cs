using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Types = System.Types;

public class Headphones : MonoBehaviour
{
    [SerializeField] private GameObject headphonesCanvas;

    [Header("flow")]
    [SerializeField] private float revealDuration = 2f;
    [SerializeField] private float exitToBlackDuration = 1f;
    [SerializeField] private float headphonesAnim = 2f;

    private bool _canContinue;
    private bool _leaving;

    // this is for transparency for the fade
    float alpha = 1;

    private void Start()
    {
        headphonesCanvas.SetActive(true);

        // reveal scene from black
        new Types.ScreenFadeData(
            fadeInDuration: revealDuration,
            fadeDuration: 0f,
            fadeOutDuration: 0f
        ).Send();
    }

    // ill set this up later
    private IEnumerator ShowHeadphonesAfterDelay()
    {
        yield return new WaitForSeconds(headphonesAnim);
        _canContinue = true;
    }

    private void Update()
    {
        if (!_canContinue || _leaving) return;

         _leaving = true;
         _canContinue = false;

         // fade to black, then load game warnings when black is reached
         new Types.ScreenFadeData(
             fadeInDuration: 0.5f,
             fadeDuration: 0.1f,
             fadeOutDuration: exitToBlackDuration,
             onFadeOutComplete: LoadGameWarnings
          ).Send();
        
    }

    private void LoadGameWarnings()
    {
        SceneSwapper.Instance.SwapScene("GameWarnings");
    }
}
