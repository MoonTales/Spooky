using UnityEngine;
using Player;

public class PlayerAttached : MonoBehaviour
{
    public bool attached = false;
	private Transform playerReference;

	private void Start()
	{
		playerReference = PlayerManager.Instance.GetPlayer().transform;
	}

	void Update()
    {
        if (attached)
		{
            transform.position = playerReference.position;
		}
    }
}
