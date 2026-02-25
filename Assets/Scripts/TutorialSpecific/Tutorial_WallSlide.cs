using Managers;
using UnityEngine;

public class Tutorial_WallSlide : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private AudioManager.SfxId doorCloseSfxId = AudioManager.SfxId.TutorialDoorSlide;

    public void SlideWall()
    {
        Vector3 targetPosition = transform.position + new Vector3(0, 0, 15);
        StartCoroutine(LerpToPositionCoroutine(targetPosition, 5f));
        
        // play some SFX for now to make it feel a bit nicer
        AudioManager audioManager = AudioManager.Instance;
        // this will never be null, but im mostly keeping the coding style being used
        if (audioManager != null)
        {
            audioManager.PlaySfx(doorCloseSfxId, transform);
        }
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
