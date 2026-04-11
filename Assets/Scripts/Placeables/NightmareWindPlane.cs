using UnityEngine;

namespace Placeables
{
    public class NightmareWindPlane : MonoBehaviour
    {
        private const float DebugLogIntervalSeconds = 0.25f;

        public enum LocalAxisDirection
        {
            PositiveX,
            NegativeX,
            PositiveY,
            NegativeY,
            PositiveZ,
            NegativeZ
        }

        [Header("Plane Orientation")]
        [SerializeField] private LocalAxisDirection outsideNormalAxis = LocalAxisDirection.PositiveZ;
        [SerializeField] private LocalAxisDirection lateralAxis = LocalAxisDirection.PositiveX;

        [Header("Blend Overrides")]
        [SerializeField] private bool useSimpleControls;
        [SerializeField] private float halfWidthOverride;
        [SerializeField] private float blendDepthOverride;
        [SerializeField] private float maxInfluenceDistanceOverride;
        [SerializeField, Range(0f, 1f)] private float interiorAmountFloor = 0f;
        [SerializeField, Range(0f, 1f)] private float interiorAmountCeiling = 1f;

        [Header("Debug")]
        [SerializeField] private bool debugOutput;

        [Header("Editor")]
        [SerializeField] private Color editorGizmoColor = new Color(0.15f, 0.7f, 1f, 0.2f);

        private float _nextDebugLogTime;

        public bool UseSimpleControls => useSimpleControls;
        public float HalfWidthOverride => useSimpleControls
            ? BlendDepthOverride * 0.5f
            : Mathf.Max(0f, halfWidthOverride);
        public float BlendDepthOverride => useSimpleControls
            ? MaxInfluenceDistanceOverride * 0.5f
            : Mathf.Max(0f, blendDepthOverride);
        public float MaxInfluenceDistanceOverride => Mathf.Max(0f, maxInfluenceDistanceOverride);
        public float InteriorAmountFloor => Mathf.Clamp01(interiorAmountFloor);
        public float InteriorAmountCeiling => Mathf.Clamp01(interiorAmountCeiling);
        public bool DebugOutputEnabled => debugOutput;

        public Vector3 GetOutsideNormalWorld()
        {
            return GetAxisDirectionWorld(outsideNormalAxis);
        }

        public Vector3 GetLateralDirectionWorld()
        {
            return GetAxisDirectionWorld(lateralAxis);
        }

        public void LogDebugBlendState(
            bool isActivePlane,
            float signedDepth,
            float lateralOffset,
            float planarDistance,
            float interiorAmount,
            float halfWidth,
            float blendDepth,
            float maxInfluenceDistance)
        {
            if (!debugOutput || !Application.isPlaying)
            {
                return;
            }

            if (Time.unscaledTime < _nextDebugLogTime)
            {
                return;
            }

            _nextDebugLogTime = Time.unscaledTime + DebugLogIntervalSeconds;
            Debug.Log(
                $"NightmareWindPlane '{name}': active={isActivePlane}, interior={interiorAmount:F3}, signedDepth={signedDepth:F3}, lateral={lateralOffset:F3}/{halfWidth:F3}, planar={planarDistance:F3}/{maxInfluenceDistance:F3}, blendDepth={blendDepth:F3}, floor={InteriorAmountFloor:F3}, ceiling={InteriorAmountCeiling:F3}",
                this);
        }

        private Vector3 GetAxisDirectionWorld(LocalAxisDirection axisDirection)
        {
            return axisDirection switch
            {
                LocalAxisDirection.PositiveX => transform.right,
                LocalAxisDirection.NegativeX => -transform.right,
                LocalAxisDirection.PositiveY => transform.up,
                LocalAxisDirection.NegativeY => -transform.up,
                LocalAxisDirection.PositiveZ => transform.forward,
                LocalAxisDirection.NegativeZ => -transform.forward,
                _ => transform.forward
            };
        }

        private void OnValidate()
        {
            if (UsesSameAxis(outsideNormalAxis, lateralAxis))
            {
                lateralAxis = GetFallbackLateralAxis(outsideNormalAxis);
            }

            halfWidthOverride = Mathf.Max(0f, halfWidthOverride);
            blendDepthOverride = Mathf.Max(0f, blendDepthOverride);
            maxInfluenceDistanceOverride = Mathf.Max(0f, maxInfluenceDistanceOverride);
            interiorAmountFloor = Mathf.Clamp01(interiorAmountFloor);
            interiorAmountCeiling = Mathf.Clamp01(interiorAmountCeiling);
            if (interiorAmountCeiling < interiorAmountFloor)
            {
                interiorAmountCeiling = interiorAmountFloor;
            }
        }

        private static bool UsesSameAxis(LocalAxisDirection a, LocalAxisDirection b)
        {
            return GetAxisFamily(a) == GetAxisFamily(b);
        }

        private static int GetAxisFamily(LocalAxisDirection axisDirection)
        {
            return axisDirection switch
            {
                LocalAxisDirection.PositiveX or LocalAxisDirection.NegativeX => 0,
                LocalAxisDirection.PositiveY or LocalAxisDirection.NegativeY => 1,
                _ => 2
            };
        }

        private static LocalAxisDirection GetFallbackLateralAxis(LocalAxisDirection normalAxis)
        {
            return GetAxisFamily(normalAxis) == 0
                ? LocalAxisDirection.PositiveZ
                : LocalAxisDirection.PositiveX;
        }

        private void OnDrawGizmos()
        {
            Vector3 outsideNormal = GetOutsideNormalWorld().normalized;
            Vector3 lateral = GetLateralDirectionWorld().normalized;
            if (outsideNormal.sqrMagnitude <= 0.0001f || lateral.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            float halfWidth = Mathf.Max(0.5f, HalfWidthOverride > 0f ? HalfWidthOverride : 1f);
            float blendDepth = Mathf.Max(0.5f, BlendDepthOverride > 0f ? BlendDepthOverride : 1f);

            Vector3 center = transform.position;
            Vector3 left = center - lateral * halfWidth;
            Vector3 right = center + lateral * halfWidth;
            Vector3 outsideLeft = left + outsideNormal * blendDepth;
            Vector3 outsideRight = right + outsideNormal * blendDepth;
            Vector3 insideLeft = left - outsideNormal * blendDepth;
            Vector3 insideRight = right - outsideNormal * blendDepth;

            Gizmos.color = editorGizmoColor;
            Gizmos.DrawLine(left, right);
            Gizmos.DrawLine(left, outsideLeft);
            Gizmos.DrawLine(right, outsideRight);
            Gizmos.DrawLine(left, insideLeft);
            Gizmos.DrawLine(right, insideRight);
            Gizmos.DrawLine(outsideLeft, outsideRight);
            Gizmos.DrawLine(insideLeft, insideRight);

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(center, center + outsideNormal * blendDepth);
        }
    }
}
