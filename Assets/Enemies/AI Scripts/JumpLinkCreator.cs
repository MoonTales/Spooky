using UnityEngine;
using Unity.AI.Navigation;
using UnityEngine.ProBuilder;

public class JumpLinkCreator : MonoBehaviour
{
    public enum LinkDirection
	{
        Left,
        Right,
        Front,
        Back
	}
    [SerializeField] private LinkDirection linkGenerationDirection;
    [SerializeField] private Transform linkedSurface;
    [SerializeField] private Renderer objectToJumpOn;
    [SerializeField] private float agentRadius = 3;

    [ContextMenu("Generate Links")]
    public void GenerateJumpLinks()
	{
        Renderer linkedObject = linkedSurface.GetComponent<Renderer>();
        Transform mainObjectTransform = objectToJumpOn.transform;

        if (linkGenerationDirection == LinkDirection.Left)
		{
            Transform upChild = transform.Find("LeftLinkUp");
            if (upChild == null)
			{
                upChild = new GameObject("LeftLinkUp").transform;
                upChild.SetParent(this.transform);

                // Optionally reset local position/rotation
                upChild.localPosition = Vector3.zero;
            }

            NavMeshLink upLink = upChild.GetComponent<NavMeshLink>();
            if (upLink == null)
			{
                upLink = upChild.gameObject.AddComponent<NavMeshLink>();
			}

            float upHeight = linkedSurface.position.y - mainObjectTransform.position.y + (linkedObject.bounds.size.y / 2);
            float upLength = ((objectToJumpOn.bounds.size.x / 2) + (3 * agentRadius) + 0.05f) * -1;
            float upLengthReverse = ((objectToJumpOn.bounds.size.x / 2) - (3 * agentRadius) + 0.45f) * -1;

            upLink.startPoint = new Vector3(0, upHeight, upLength);
            upLink.endPoint = new Vector3(0, objectToJumpOn.bounds.size.y / 2, upLengthReverse);
            upLink.width = objectToJumpOn.bounds.size.z - (3 * agentRadius);

            Transform downChild = transform.Find("LeftLinkDown");
            if (downChild == null)
            {
                downChild = new GameObject("LeftLinkDown").transform;
                downChild.SetParent(this.transform);

                // Optionally reset local position/rotation
                downChild.localPosition = Vector3.zero;
            }

            NavMeshLink downLink = downChild.GetComponent<NavMeshLink>();
            if (downLink == null)
            {
                downLink = downChild.gameObject.AddComponent<NavMeshLink>();
            }

            downLink.startPoint = new Vector3(0, objectToJumpOn.bounds.size.y / 2, upLengthReverse);
            downLink.endPoint = new Vector3(0, upHeight, upLength - 1.5f);
            downLink.width = objectToJumpOn.bounds.size.z - (3 * agentRadius);
        }

        else if (linkGenerationDirection == LinkDirection.Right)
        {
            Transform upChild = transform.Find("RightLinkUp");
            if (upChild == null)
            {
                upChild = new GameObject("RightLinkUp").transform;
                upChild.SetParent(this.transform);

                // Optionally reset local position/rotation
                upChild.localPosition = Vector3.zero;
            }

            NavMeshLink upLink = upChild.GetComponent<NavMeshLink>();
            if (upLink == null)
            {
                upLink = upChild.gameObject.AddComponent<NavMeshLink>();
            }

            float upHeight = linkedSurface.position.y - mainObjectTransform.position.y + (linkedObject.bounds.size.y / 2);
            float upLength = (objectToJumpOn.bounds.size.x / 2) + (3 * agentRadius) + 0.05f;
            float upLengthReverse = (objectToJumpOn.bounds.size.x / 2) - (3 * agentRadius) + 0.45f;

            upLink.startPoint = new Vector3(0, upHeight, upLength);
            upLink.endPoint = new Vector3(0, objectToJumpOn.bounds.size.y / 2, upLengthReverse);
            upLink.width = objectToJumpOn.bounds.size.z - (3 * agentRadius);

            Transform downChild = transform.Find("RightLinkDown");
            if (downChild == null)
            {
                downChild = new GameObject("RightLinkDown").transform;
                downChild.SetParent(this.transform);

                // Optionally reset local position/rotation
                downChild.localPosition = Vector3.zero;
            }

            NavMeshLink downLink = downChild.GetComponent<NavMeshLink>();
            if (downLink == null)
            {
                downLink = downChild.gameObject.AddComponent<NavMeshLink>();
            }

            downLink.startPoint = new Vector3(0, objectToJumpOn.bounds.size.y / 2, upLengthReverse);
            downLink.endPoint = new Vector3(0, upHeight, upLength + 1.5f);
            downLink.width = objectToJumpOn.bounds.size.z - (3 * agentRadius);
        }

        else if (linkGenerationDirection == LinkDirection.Front)
        {
            Transform upChild = transform.Find("FrontLinkUp");
            if (upChild == null)
            {
                upChild = new GameObject("FrontLinkUp").transform;
                upChild.SetParent(this.transform);

                // Optionally reset local position/rotation
                upChild.localPosition = Vector3.zero;
            }

            NavMeshLink upLink = upChild.GetComponent<NavMeshLink>();
            if (upLink == null)
            {
                upLink = upChild.gameObject.AddComponent<NavMeshLink>();
            }

            float upHeight = linkedSurface.position.y - mainObjectTransform.position.y + (linkedObject.bounds.size.y / 2);
            float upLength = ((objectToJumpOn.bounds.size.z / 2) + (3 * agentRadius) + 0.05f) * -1;
            float upLengthReverse = ((objectToJumpOn.bounds.size.z / 2) - (3 * agentRadius) + 0.45f) * -1;

            upLink.startPoint = new Vector3(0, upHeight, upLength);
            upLink.endPoint = new Vector3(0, objectToJumpOn.bounds.size.y / 2, upLengthReverse);
            upLink.width = objectToJumpOn.bounds.size.x - (3 * agentRadius);

            Transform downChild = transform.Find("FrontLinkDown");
            if (downChild == null)
            {
                downChild = new GameObject("FrontLinkDown").transform;
                downChild.SetParent(this.transform);

                // Optionally reset local position/rotation
                downChild.localPosition = Vector3.zero;
            }

            NavMeshLink downLink = downChild.GetComponent<NavMeshLink>();
            if (downLink == null)
            {
                downLink = downChild.gameObject.AddComponent<NavMeshLink>();
            }

            downLink.startPoint = new Vector3(0, objectToJumpOn.bounds.size.y / 2, upLengthReverse);
            downLink.endPoint = new Vector3(0, upHeight, upLength - 1.5f);
            downLink.width = objectToJumpOn.bounds.size.x - (3 * agentRadius);
        }

        else if (linkGenerationDirection == LinkDirection.Back)
        {
            Transform upChild = transform.Find("BackLinkUp");
            if (upChild == null)
            {
                upChild = new GameObject("BackLinkUp").transform;
                upChild.SetParent(this.transform);

                // Optionally reset local position/rotation
                upChild.localPosition = Vector3.zero;
            }

            NavMeshLink upLink = upChild.GetComponent<NavMeshLink>();
            if (upLink == null)
            {
                upLink = upChild.gameObject.AddComponent<NavMeshLink>();
            }

            float upHeight = linkedSurface.position.y - mainObjectTransform.position.y + (linkedObject.bounds.size.y / 2);
            float upLength = (objectToJumpOn.bounds.size.z / 2) + (3 * agentRadius) + 0.05f;
            float upLengthReverse = (objectToJumpOn.bounds.size.z / 2) - (3 * agentRadius) + 0.45f;

            upLink.startPoint = new Vector3(0, upHeight, upLength);
            upLink.endPoint = new Vector3(0, objectToJumpOn.bounds.size.y / 2, upLengthReverse);
            upLink.width = objectToJumpOn.bounds.size.x - (3 * agentRadius);

            Transform downChild = transform.Find("BackLinkDown");
            if (downChild == null)
            {
                downChild = new GameObject("BackLinkDown").transform;
                downChild.SetParent(this.transform);

                // Optionally reset local position/rotation
                downChild.localPosition = Vector3.zero;
            }

            NavMeshLink downLink = downChild.GetComponent<NavMeshLink>();
            if (downLink == null)
            {
                downLink = downChild.gameObject.AddComponent<NavMeshLink>();
            }

            downLink.startPoint = new Vector3(0, objectToJumpOn.bounds.size.y / 2, upLengthReverse);
            downLink.endPoint = new Vector3(0, upHeight, upLength + 1.5f);
            downLink.width = objectToJumpOn.bounds.size.x - (3 * agentRadius);
        }
    }
}
