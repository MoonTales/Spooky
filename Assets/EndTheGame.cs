using UnityEngine;
using System;
using Types = System.Types;

public class EndTheGame : MonoBehaviour
{
	private void Awake()
	{
		//System.SceneSwapper.Instance.SwapScene("Credits");
		Types.ScreenFadeSceneTransitionData data = new Types.ScreenFadeSceneTransitionData(3f, 3f, "Credits", null, null, null);
		data.Send();
	}
}
