using UnityEngine;

public class FlashlightChange : MonoBehaviour
{
    [SerializeField] private bool increaseFlashlight = false;
    [SerializeField] private bool slightIncrease = false;

	private void OnTriggerEnter(Collider other)
	{
		if (other.tag == "Player")
		{
			Flashlight.Instance.GetComponent<Animator>().SetBool("SlightIncrease", slightIncrease);
			Flashlight.Instance.GetComponent<Animator>().SetBool("Increase", increaseFlashlight);
		}
	}
}
