using UnityEngine;

public class StartMusicScript : MonoBehaviour
{
	private void OnTriggerEnter(Collider other)
	{
		if (other.tag == "Player")
		{
			GetComponent<AudioSource>().Play();
			foreach (BoxCollider col in GetComponents<BoxCollider>())
			{
				col.enabled = false;
			}
		}
	}
}
