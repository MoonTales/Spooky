using UnityEngine;

public class StopThePlayerZone : MonoBehaviour
{
	private void OnTriggerStay(Collider other)
	{
		if (other.tag == "Player")
		{
			Player.PlayerController.Instance.LockInput();

			if (Flashlight.Instance.IsFlashlightOn())
			{
				Flashlight.Instance.ToggleFlashlight();
			}
		}
	}
}
