using System;
using UnityEngine;

namespace Managers
{
    public class GameStateManager : Singleton<GameStateManager>
    {

        // Game state manager can send broadcats for when the game starts, pauses, resumes, and ends.


        protected void Update()
        {
            // small update to show how this would work
            if(Input.GetKeyDown(KeyCode.P))
            {
                EventBroadcaster.Broadcast_OnPlayerDamaged(10.0f);
            }
        }
    }
}
