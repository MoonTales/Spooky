using UnityEngine;

public class JustDoAJumpscare : MonoBehaviour
{
	public Animator anim;

	public void Jumpscare()
	{
		anim.SetBool("Scary", true);
	}
}
