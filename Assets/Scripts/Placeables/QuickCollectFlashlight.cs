using System;
using UnityEngine;

public class QuickCollectFlashlight : MonoBehaviour
{
	
	
	private void OnTriggerEnter(Collider other)
	{
		if (other.tag == "Player")
		{
			gameObject.SetActive(false);
			// once we have picked up the flashlight, we want to broadcast to the player that they have picked up the flashlight
			Flashlight.Instance.SetDoWePossessTheFlashlight(true);
		}
	}
}
