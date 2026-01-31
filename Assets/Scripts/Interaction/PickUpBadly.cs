using System.Collections;
using Player;
using UnityEngine;

public class PickUpBadly : MonoBehaviour
{
	private Vector3 startPosition;
    private float elapsedTime;
    private bool grabbed = false;
    private bool grabbing = false;

    private void Start()
	{
		startPosition = transform.position;
	}

	private void Update()
	{
		if (grabbing || Vector3.Distance(PlayerManager.Instance.GetPlayer().transform.position, transform.position) < 1)
		{
			if (!grabbing)
			{
                StartCoroutine(MoveRoutine());
			}

            if (grabbed)
			{
                transform.position = PlayerManager.Instance.GetPlayerHandTransform().position;
            }
		}
	}

    IEnumerator MoveRoutine()
    {
        grabbing = true;
        elapsedTime = 0f;

        // Continue looping as long as the elapsed time is less than the duration
        while (elapsedTime < 1)
        {
            // Calculate the interpolation percentage (0 to 1)
            // This ensures linear movement over the specified time
            float t = elapsedTime / 1;

            // Update the object's position using Vector3.Lerp
            transform.position = Vector3.Lerp(startPosition, PlayerManager.Instance.GetPlayerHandTransform().position, t);

            // Increment the elapsed time by the time passed since the last frame
            elapsedTime += Time.deltaTime;

            // Wait until the next frame before continuing the loop
            yield return null;
        }

        // Ensure the object reaches the exact end position at the end of the coroutine
        grabbed = true;
    }
}
