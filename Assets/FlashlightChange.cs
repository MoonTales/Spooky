using UnityEngine;

public class FlashlightChange : MonoBehaviour
{
    [SerializeField] private bool increaseFlashlight = false;

	private void OnTriggerEnter(Collider other)
	{
		if (other.tag == "Player")
		{
			Flashlight.Instance.GetComponent<Animator>().SetBool("Increase", increaseFlashlight);
		}
	}
}
