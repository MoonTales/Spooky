using UnityEngine;
using Player;

public class AttachToPlayer : MonoBehaviour
{
    [SerializeField] private PlayerAttached attacher;
    [SerializeField] private bool detach = false;

	private void OnTriggerEnter(Collider other)
	{
		if (other.tag == "Player")
		{
			attacher.attached = !detach;
		}
	}
}
