using UnityEngine;
using Player;

public class TeleportPlayer : MonoBehaviour
{
	[SerializeField] private Vector3 teleportLocation;

	private void OnTriggerEnter(Collider other)
	{
		if (other.tag == "Player")
		{
			PlayerManager.Instance.TeleportPlayer(teleportLocation);
		}
	}
}
