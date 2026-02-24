using Managers;
using UnityEngine;

public class TriggerDoorClose : MonoBehaviour
{
	[SerializeField] private bool playDoorCloseSfx = false;
	[SerializeField] private AudioManager.SfxId doorCloseSfxId = AudioManager.SfxId.TutorialDoorSlide;

	private void OnTriggerEnter(Collider other)
	{
		if (other.tag == "Player")
		{
			gameObject.GetComponent<Animator>().SetTrigger("Close");
			PlayDoorCloseSfx();
		}
	}

	private void PlayDoorCloseSfx()
	{
		if (!playDoorCloseSfx)
		{
			return;
		}

		AudioManager audioManager = Object.FindAnyObjectByType<AudioManager>();
		if (audioManager != null)
		{
			audioManager.PlaySfx(doorCloseSfxId, transform);
		}
	}
}
