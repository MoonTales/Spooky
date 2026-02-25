using FMOD.Studio;
using Managers;
using System;
using UnityEngine;
using Types = System.Types;

namespace Interaction.drawings
{
    // Special drawing for the tutorial to help us teleport
    public class TutorialDrawing : Drawing
    {
        [SerializeField] private SceneField sceneToLoad;
        [SerializeField] private float retryStartIntervalSeconds = 0.25f;

        private EventInstance _staticLoopInstance;
        private AudioManager _audioManager;
        private bool _shouldTryStartStaticLoop;
        private float _nextStartRetryTime;

        private void OnEnable()
        {
            _shouldTryStartStaticLoop = true;
            _nextStartRetryTime = 0f;
            StartDrawingStaticLoop();
        }

        private void OnDisable()
        {
            _shouldTryStartStaticLoop = false;
            StopDrawingStaticLoopImmediate();
        }

        private void OnDestroy()
        {
            _shouldTryStartStaticLoop = false;
            StopDrawingStaticLoopImmediate();
        }

        private void LateUpdate()
        {
            if (!_staticLoopInstance.isValid())
            {
                TryStartDrawingStaticLoopWithRetry();
                return;
            }

            if (TryGetAudioManager(out AudioManager audioManager))
            {
                audioManager.UpdateEventInstanceTransform(_staticLoopInstance, transform);
            }
        }

        public override void Interact(Interactor interactor)
        {
            StopRelatedTutorialDrawingLoops();
            if (TryGetAudioManager(out AudioManager audioManager))
            {
                audioManager.StopTutorialDrawingStaticLoopsImmediate();
            }

            // Match good-wakeup flow so alarm transition and fade timing stay in sync with other wakeup paths.
            Collider collider = GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }

            const int timeToFadeOut = 5;
            SleepTrackerManager.Instance.SetIsGoodWakeup(true);
            AudioManager.Instance.BeginGoodWakeupAlarmTransition();
            SleepTrackerManager.Instance.TurnSleepTrackerOn();

            Types.ScreenFadeData data = new Types.ScreenFadeData(
                fadeInDuration: 1,
                2,
                fadeOutDuration: timeToFadeOut,
                null,
                FadeOutCompleted);
            data.Send();
        }

        private void FadeOutCompleted()
        {
            SleepTrackerManager.Instance.SetIsGoodWakeup(true);
            SceneSwapper.Instance.SwapScene(sceneToLoad);
            GameStateManager.Instance.SetCurrentZoneId(-1);
        }

        private void StartDrawingStaticLoop()
        {
            StopDrawingStaticLoopImmediate();

            if (TryGetAudioManager(out AudioManager audioManager)
                && audioManager.TryStartTutorialDrawingStaticLoop(transform, out EventInstance instance))
            {
                _staticLoopInstance = instance;
                _shouldTryStartStaticLoop = false;
            }
        }

        private void TryStartDrawingStaticLoopWithRetry()
        {
            if (!_shouldTryStartStaticLoop || Time.time < _nextStartRetryTime)
            {
                return;
            }

            _nextStartRetryTime = Time.time + Mathf.Max(0.01f, retryStartIntervalSeconds);
            StartDrawingStaticLoop();
        }

        private void StopDrawingStaticLoopImmediate()
        {
            AudioManager audioManager = _audioManager != null
                ? _audioManager
                : UnityEngine.Object.FindAnyObjectByType<AudioManager>();

            if (audioManager != null)
            {
                audioManager.StopAndReleaseEventInstance(ref _staticLoopInstance, immediate: true);
            }
            else if (_staticLoopInstance.isValid())
            {
                _staticLoopInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                _staticLoopInstance.release();
                _staticLoopInstance = default;
            }
        }

        private void StopRelatedTutorialDrawingLoops()
        {
            TutorialDrawing primary = GetPrimaryTutorialDrawing();
            if (primary != null)
            {
                primary.StopLoopAndDisableRetry();

                TutorialDrawing[] related = primary.GetComponentsInChildren<TutorialDrawing>(true);
                for (int i = 0; i < related.Length; i++)
                {
                    if (related[i] == null || related[i] == primary)
                    {
                        continue;
                    }

                    related[i].StopLoopAndDisableRetry();
                }
            }
            else
            {
                StopLoopAndDisableRetry();
            }
        }

        private TutorialDrawing GetPrimaryTutorialDrawing()
        {
            TutorialDrawing primary = this;
            Transform current = transform.parent;
            while (current != null)
            {
                TutorialDrawing parentDrawing = current.GetComponent<TutorialDrawing>();
                if (parentDrawing != null)
                {
                    primary = parentDrawing;
                }

                current = current.parent;
            }

            return primary;
        }

        private void StopLoopAndDisableRetry()
        {
            _shouldTryStartStaticLoop = false;
            StopDrawingStaticLoopImmediate();
        }

        private bool TryGetAudioManager(out AudioManager audioManager)
        {
            if (_audioManager != null)
            {
                audioManager = _audioManager;
                return true;
            }

            _audioManager = FindAnyObjectByType<AudioManager>();
            audioManager = _audioManager;
            return audioManager != null;
        }
    }
}
