using UnityEngine;

public class StartMusicScript : MonoBehaviour
{
	
	private AudioSource audioSource;
	private void Start()
	{
		audioSource = GetComponent<AudioSource>();
		//TODO: bandaid
		audioSource.Play();
		audioSource.Stop();
	}
	private void OnTriggerEnter(Collider other)
	{
		if (other.tag == "Player")
		{
			audioSource.Play();
			
			foreach (BoxCollider col in GetComponents<BoxCollider>())
			{
				col.enabled = false;
			}
		}
	}
}
