using UnityEngine;

public class EndTheGame : MonoBehaviour
{
	private void Awake()
	{
		System.SceneSwapper.Instance.SwapScene("Credits");
	}
}
