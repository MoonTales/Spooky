using UnityEngine;
using Player;

public class TeleportPlayer : MonoBehaviour
{
	[SerializeField] private Vector3 teleportLocation;
	[SerializeField] private bool keepPlayerX = false;
	[SerializeField] private bool keepPlayerY = false;
	[SerializeField] private bool keepPlayerZ = false;
	private Transform playerReference;

	private void Start()
	{
		playerReference = PlayerManager.Instance.GetPlayer().transform;
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.tag == "Player")
		{
			Vector3 realLocation = new Vector3(keepPlayerX ? playerReference.position.x : teleportLocation.x, keepPlayerY ? playerReference.position.y :
				teleportLocation.y, keepPlayerZ ? playerReference.position.z : teleportLocation.z);
			PlayerManager.Instance.TeleportPlayer(realLocation);
		}
	}
}
