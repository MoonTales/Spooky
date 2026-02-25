using Managers;
using UnityEngine;

public class TriggerDoorClose : MonoBehaviour
{
	[SerializeField] private bool playDoorCloseSfx = false;
	[SerializeField] private AudioManager.SfxId doorCloseSfxId = AudioManager.SfxId.TutorialDoorSlide;
	[SerializeField] private bool triggerOnce = true;

	private bool _hasTriggered;
	private Animator _animator;

	private void Awake()
	{
		_animator = GetComponent<Animator>();
	}

	private void OnTriggerEnter(Collider other)
	{
		if (!other.CompareTag("Player"))
		{
			return;
		}

		if (triggerOnce && _hasTriggered)
		{
			return;
		}

		_hasTriggered = true;
		if (_animator != null)
		{
			_animator.SetTrigger("Close");
		}
		PlayDoorCloseSfx();

		if (triggerOnce)
		{
			Collider triggerCollider = GetComponent<Collider>();
			if (triggerCollider != null)
			{
				triggerCollider.enabled = false;
			}
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
