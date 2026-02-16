using System;
using UnityEngine;

namespace Interaction.drawings
{
    // Special drawing for the tutorial to help us teleport
    public class TutorialDrawing : Drawing
    {
        [SerializeField] private SceneField sceneToLoad;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        public override void Interact(Interactor interactor)
        {
            SceneSwapper.Instance.SwapScene(sceneToLoad);
        }
    }
}
