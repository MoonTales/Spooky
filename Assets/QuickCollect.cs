using UnityEngine;

public class QuickCollect : MonoBehaviour
{
	private void OnTriggerEnter(Collider other)
	{
		if (other.tag == "Player")
		{
			gameObject.SetActive(false);
		}
	}
}
