using System;
using Managers;
using UnityEngine;
using Types = System.Types;

namespace Interaction.drawings
{
    // Special drawing for the tutorial to help us teleport
    public class TutorialDrawing : Drawing
    {
        [SerializeField] private SceneField sceneToLoad;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        public override void Interact(Interactor interactor)
        {
            // we are gonna treat this the EXACT same as a good wakeup, from the nightmare
            // Disable the collider so that we cant interact with this again while the fadeout is happening
            GetComponent<Collider>().enabled = false;
            const int timeToFadeOut = 5;
            // Mark as good wakeup before turning the tracker on so the correct variant starts immediately.
            SleepTrackerManager.Instance.SetIsGoodWakeup(true);
            AudioManager.Instance.BeginGoodWakeupAlarmTransition();
            SleepTrackerManager.Instance.TurnSleepTrackerOn();
            Types.ScreenFadeData data = new Types.ScreenFadeData(fadeInDuration:1, 2, fadeOutDuration:timeToFadeOut, null, FadeOutCompleted);
            data.Send();
            // set the prompt to be empty, since we are teleporting away and dont want the player to see the old prompt after they interact
            PromptKey = new TextKey();

        }

        private void FadeOutCompleted()
        {
            SleepTrackerManager.Instance.SetIsGoodWakeup(true);
            SceneSwapper.Instance.SwapScene(sceneToLoad);
            GameStateManager.Instance.SetCurrentZoneId(-1);
        }
    }
}
