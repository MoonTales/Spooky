using UnityEngine;
using Managers;
using System;
using System.Collections.Generic;
using System.Collections;

public class JustDoAJumpscare : MonoBehaviour
{
	public Animator anim;
	public AudioClip[] jumpscareSounds;
	public string jumpscareActivateBool = "Scary";
	public bool boolValue = true;
	public float damageDone = 0;
	public float delay = 0;

	public void Jumpscare()
	{
		if (delay > 0)
		{
			StartCoroutine(delayJumpscare());
		}
		else
		{
			if (anim != null)
				anim.SetBool(jumpscareActivateBool, boolValue);

			if (jumpscareSounds != null)
			{
				foreach (AudioClip jumpscare in jumpscareSounds)
				{
					UAudio.Instance.PlayClip(jumpscare);
				}
			}

			if (damageDone > 0)
			{
				EventBroadcaster.Broadcast_OnPlayerDamaged(damageDone);
			}
		}
	}

	IEnumerator delayJumpscare()
	{
		yield return new WaitForSeconds(delay);

		if (anim != null)
			anim.SetBool(jumpscareActivateBool, boolValue);

		if (jumpscareSounds != null)
		{
			foreach (AudioClip jumpscare in jumpscareSounds)
			{
				UAudio.Instance.PlayClip(jumpscare);
			}
		}

		if (damageDone > 0)
		{
			EventBroadcaster.Broadcast_OnPlayerDamaged(damageDone);
		}
	}
}
