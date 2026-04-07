using UnityEngine;
using Managers;

public class JustDoAJumpscare : MonoBehaviour
{
	public Animator anim;
	public AudioClip[] jumpscareSounds;
	public string jumpscareActivateBool = "Scary";
	public bool boolValue = true;

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
	}
}
