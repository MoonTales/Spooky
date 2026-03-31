using System;
using Managers;
using Player;
using UnityEngine;
using Types = System.Types;

public class QuickCollectFlashlight : MonoBehaviour
{
	
	// this is a weird class that exists only for 1 object in the tutorial, so ima add an edge case check to it for the flashlight

	public float _minYDistanceToPlayer = 8f;
	private GameObject _flashlightLight;
	
	private void OnTriggerEnter(Collider other)
	{
		if (other.tag == "Player")
		{
			gameObject.SetActive(false);
			// once we have picked up the flashlight, we want to broadcast to the player that they have picked up the flashlight
			Flashlight.Instance.SetDoWePossessTheFlashlight(true);
		}
	}
	
	private void Start()
	{
		// get the child with name "YellowLight"
		_flashlightLight = transform.Find("YellowLight").gameObject;
	}

	private void FixedUpdate()
	{
		if (PlayerManager.Instance.CanPlayerSeeThis(gameObject.transform) || PlayerManager.Instance.GetDistance(gameObject.transform.position) <= 5)
		{
			_flashlightLight.SetActive(true);
		} else
		{
			_flashlightLight.SetActive(false);
		}
	}
}