using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpiderPivot : MonoBehaviour
{
    [SerializeField] Transform pivot;
    [SerializeField] Scan scan;
    [SerializeField, Range(0, 1)] float positionWeight = 0;
    [SerializeField, Range(0, 1)] float rotationWeight = 1;

    public bool lockYRotation = false;

    public Transform Pivot { get => pivot; }

    void OnDisable()
    {
        pivot.localPosition = Vector3.zero;
        pivot.localRotation = Quaternion.identity;
    }

    void Update()
    {
        UpdatePivot();
    }

    void UpdatePivot()
    {
        List<(Vector3 pos, Quaternion rot, float weight)> points = scan.Points();
        if (points.Count == 0){ return;}

        Quaternion rotAvg;
        List<Quaternion> rots = new List<Quaternion>();
        List<float> weights = new List<float>();

        Vector3 posAvg = Vector3.zero;
        int nbPoint = 0;

        foreach ((Vector3 pos, Quaternion rot, float weight) point in points)
        {
            rots.Add(point.rot);
            weights.Add(point.weight);
            posAvg += point.pos;
            nbPoint++;
        }

        posAvg /= nbPoint;
        rotAvg = MathExtension.QuatAvgApprox(rots.ToArray(), weights.ToArray());

        pivot.position = Vector3.Lerp(transform.position, posAvg, positionWeight);
        if (!lockYRotation)
        {
            pivot.rotation = Quaternion.Lerp(transform.rotation, rotAvg, rotationWeight);
        }
        else
		{
            Vector3 currentEuler = transform.eulerAngles;

            Quaternion awaa = Quaternion.Lerp(transform.rotation, rotAvg, rotationWeight);

            pivot.rotation = new Quaternion(awaa.x, pivot.rotation.y, awaa.z, pivot.rotation.w);
        }
    }
}
