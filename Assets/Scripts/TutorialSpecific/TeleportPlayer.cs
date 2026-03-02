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
			// Set the rotation to be the OPPOSITE as this object's rotation (this is hard coded rn, cause I legit can not find 
			// the object this is attached to in the world, so I had to trial and error to find this
			var rotation = transform.rotation;
			// Reverse the rotation by 180 degrees on the Y axis
			rotation = Quaternion.Euler(rotation.eulerAngles.x, rotation.eulerAngles.y + 180, rotation.eulerAngles.z);
			PlayerManager.Instance.TeleportPlayer(realLocation, rotation);
		}
	}
}
