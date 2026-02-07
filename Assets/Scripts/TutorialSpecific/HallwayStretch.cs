using UnityEngine;
using Player;

public class HallwayStretch : MonoBehaviour
{
    private Animator anim;
    private bool longed = false;
	private bool shorted = false;
	public float maxRunTime = 3;
	private float currentRunTime = 0;

	private void Start()
	{
		anim = gameObject.GetComponent<Animator>();
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.gameObject.layer == 8)
		{
			anim.SetTrigger("Longer");
			longed = true;
		}
	}

	private void OnTriggerStay(Collider other)
	{
		if (other.gameObject.layer == 8 && longed && !shorted)
		{
			if (other.GetComponent<Attractor>().intensity > 6)
			{
				currentRunTime += Time.deltaTime;
				if (currentRunTime >= maxRunTime)
				{
					anim.SetTrigger("Shorter");
					shorted = true;
				}
			}
			else
			{
				currentRunTime = 0;
			}
		}
	}
}
