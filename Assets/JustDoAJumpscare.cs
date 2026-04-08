using UnityEngine;
using Managers;
using System;

public class JustDoAJumpscare : MonoBehaviour
{
	public Animator anim;
	public AudioClip[] jumpscareSounds;
	public string jumpscareActivateBool = "Scary";
	public bool boolValue = true;
	public float damageDone = 0;

	public void Jumpscare()
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
