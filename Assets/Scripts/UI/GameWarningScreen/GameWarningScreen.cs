using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Types = System.Types;

public class GameWarningScreen : MonoBehaviour
{
    [SerializeField] private GameObject warningsCanvas;
    [SerializeField] private TMP_Text pressKey;

    [Header("flow")]
    [SerializeField] private float revealDuration = 2f;
    [SerializeField] private float delayBeforePrompt = 5f;
    [SerializeField] private float exitToBlackDuration = 1f;

    private bool _canContinue;
    private bool _leaving;
    float alpha = 1;

    private Color _pressKeyBaseColor;

    private void Start()
    {
        warningsCanvas.SetActive(true);

        _pressKeyBaseColor = pressKey.color;

        // hide prompt at start
        pressKey.gameObject.SetActive(false);

        // reveal scene from black
        new Types.ScreenFadeData(
            fadeInDuration: revealDuration,
            fadeDuration: 0f,
            fadeOutDuration: 0f
        ).Send();

        StartCoroutine(ShowPromptAfterDelay());
    }

    private IEnumerator ShowPromptAfterDelay()
    {
        yield return new WaitForSeconds(delayBeforePrompt);
        pressKey.gameObject.SetActive(true);
        _canContinue = true;
    }

    private void Update()
    {
        if (!_canContinue || _leaving) return;

        if (Input.anyKeyDown)
        {
            _leaving = true;
            _canContinue = false;

            // fade to black, then load main menu when black is reached
            new Types.ScreenFadeData(
                fadeInDuration: 0f,
                fadeDuration: 0.1f,
                fadeOutDuration: exitToBlackDuration,
                onFadeOutComplete: LoadMainMenu
            ).Send();
        }
    }

    private void LoadMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}