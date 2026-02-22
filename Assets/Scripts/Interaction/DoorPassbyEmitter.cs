using System.Collections;
using FMOD.Studio;
using FMODUnity;
using Managers;
using Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using Types = System.Types;

[DisallowMultipleComponent]
public class DoorPassbyEmitter : MonoBehaviour
{
    [Header("FMOD")]
    [SerializeField] private EventReference passbyEvent;

    [Header("Scene Gate")]
    [SerializeField] private string allowedSceneName = "Bedroom";
    [SerializeField] private bool requireGameplayState = true;

    [Header("Random Trigger Timing")]
    [SerializeField] private Vector2 intervalRange = new Vector2(8f, 20f);
    [SerializeField] private int maxPlays = -1; // -1 = unlimited plays.

    [Header("Motion")]
    [SerializeField] private float duration = 4.5f;
    [SerializeField] private float sideDistance = 1.5f;
    [SerializeField] private float pathHalfWidth = 2.5f;
    [SerializeField] private float height = 1.7f;

    [Header("Optional Overrides")]
    [SerializeField] private Transform listenerOverride;

    private Coroutine _loopCoroutine;
    private Coroutine _passbyCoroutine;
    private EventInstance _activeInstance;
    private bool _isPlaying;
    private int _playsStarted;
    private AudioManager _audioManager;

    private void OnEnable()
    {
        _loopCoroutine = StartCoroutine(Loop());
    }

    private void OnDisable()
    {
        if (_loopCoroutine != null)
        {
            StopCoroutine(_loopCoroutine);
            _loopCoroutine = null;
        }

        if (_passbyCoroutine != null)
        {
            StopCoroutine(_passbyCoroutine);
            _passbyCoroutine = null;
        }

        StopActiveInstanceImmediate();
        _isPlaying = false;
    }

    private IEnumerator Loop()
    {
        while (enabled)
        {
            float min = Mathf.Min(intervalRange.x, intervalRange.y);
            float max = Mathf.Max(intervalRange.x, intervalRange.y);
            yield return new WaitForSeconds(Random.Range(min, max));

            if (_isPlaying || !CanTriggerNow())
            {
                continue;
            }

            if (maxPlays >= 0 && _playsStarted >= maxPlays)
            {
                yield break;
            }

            _passbyCoroutine = StartCoroutine(PlayPassby());
        }
    }

    private IEnumerator PlayPassby()
    {
        _isPlaying = true;

        if (!TryGetAudioManager(out AudioManager audioManager) || passbyEvent.IsNull)
        {
            _isPlaying = false;
            _passbyCoroutine = null;
            yield break;
        }

        Vector3 listenerPosition = GetListenerPosition();

        float listenerDot = Vector3.Dot(transform.forward, listenerPosition - transform.position);
        float listenerSide = listenerDot >= 0f ? 1f : -1f;
        float emitterSide = -listenerSide;

        bool leftToRight = Random.value < 0.5f;
        float startOffset = leftToRight ? -pathHalfWidth : pathHalfWidth;
        float endOffset = -startOffset;

        Vector3 basePosition = transform.position
                               + transform.forward * (emitterSide * sideDistance)
                               + Vector3.up * height;
        Vector3 start = basePosition + transform.right * startOffset;
        Vector3 end = basePosition + transform.right * endOffset;

        if (!audioManager.TryStartSfxEventInstance(passbyEvent, start, out _activeInstance))
        {
            _isPlaying = false;
            _passbyCoroutine = null;
            yield break;
        }

        _playsStarted++;

        float elapsed = 0f;
        float clampedDuration = Mathf.Max(0.01f, duration);
        while (elapsed < clampedDuration)
        {
            if (!CanContinuePlayback())
            {
                StopActiveInstanceImmediate();
                _isPlaying = false;
                _passbyCoroutine = null;
                yield break;
            }

            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsed / clampedDuration);
            Vector3 currentPosition = Vector3.Lerp(start, end, alpha);
            audioManager.UpdateEventInstancePosition(_activeInstance, currentPosition);
            yield return null;
        }

        audioManager.StopAndReleaseEventInstance(ref _activeInstance, immediate: false);
        _isPlaying = false;
        _passbyCoroutine = null;
    }

    private bool CanContinuePlayback()
    {
        return CanRunInAllowedScene() && (!requireGameplayState || IsGameplayState());
    }

    private bool CanTriggerNow()
    {
        if (!CanRunInAllowedScene())
        {
            return false;
        }

        if (passbyEvent.IsNull || !TryGetAudioManager(out _))
        {
            return false;
        }

        if (!requireGameplayState)
        {
            return true;
        }

        return IsGameplayState();
    }

    private bool CanRunInAllowedScene()
    {
        if (string.IsNullOrWhiteSpace(allowedSceneName))
        {
            return true;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        return activeScene.IsValid()
               && activeScene.isLoaded
               && string.Equals(activeScene.name, allowedSceneName, System.StringComparison.Ordinal);
    }

    private static bool IsGameplayState()
    {
        return GameStateManager.Instance != null
               && GameStateManager.Instance.GetCurrentGameState() == Types.GameState.Gameplay;
    }

    private Vector3 GetListenerPosition()
    {
        if (listenerOverride != null)
        {
            return listenerOverride.position;
        }

        if (PlayerController.Instance != null)
        {
            return PlayerController.Instance.transform.position;
        }

        if (Camera.main != null)
        {
            return Camera.main.transform.position;
        }

        return transform.position;
    }

    private void StopActiveInstanceImmediate()
    {
        if (TryGetAudioManager(out AudioManager audioManager))
        {
            audioManager.StopAndReleaseEventInstance(ref _activeInstance, immediate: true);
            return;
        }

        if (_activeInstance.isValid())
        {
            _activeInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            _activeInstance.release();
            _activeInstance = default;
        }
    }

    public void ResetPlayCount()
    {
        _playsStarted = 0;
    }

    private bool TryGetAudioManager(out AudioManager audioManager)
    {
        if (_audioManager != null)
        {
            audioManager = _audioManager;
            return true;
        }

        // Avoid Singleton.Instance here to prevent unload-time warning spam.
        _audioManager = FindAnyObjectByType<AudioManager>();
        audioManager = _audioManager;
        return audioManager != null;
    }
}
