using UnityEngine;

public class Tutorial_WallSlide : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created


    public void SlideWall()
    {
        // we will slide the wall in the4 negatize Z direction by 5 units over 2 second
        Vector3 targetPosition = transform.position + new Vector3(0, 0, -10);
        StartCoroutine(LerpToPositionCoroutine(targetPosition, 2f));
    }
    
    private System.Collections.IEnumerator LerpToPositionCoroutine(Vector3 targetPosition, float duration)
    {
        Vector3 startPosition = transform.position;
        float elapsedTime = 0;

        while (elapsedTime < duration)
        {
            transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.position = targetPosition; // Ensure it ends at the exact target position
    }
}
