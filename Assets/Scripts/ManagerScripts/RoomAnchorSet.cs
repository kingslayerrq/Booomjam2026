using UnityEngine;

public class RoomAnchorSet : MonoBehaviour
{
    private const float GoldenAngleDegrees = 137.50776f;

    [Header("Authored Anchors")]
    [SerializeField] private Transform[] entryPoints;
    [SerializeField] private Transform[] wanderCenters;

    [Header("Fallback Entry Spread")]
    [SerializeField] private Vector3 fallbackEntryCenterLocalPosition;
    [SerializeField] private float fallbackEntryRadius = 1.2f;
    [SerializeField] private float fallbackEntrySpacing = 0.75f;

    [Header("Fallback Wander")]
    [SerializeField] private Vector3 fallbackWanderCenterLocalPosition;
    [SerializeField] private float fallbackWanderRadius = 1.5f;

    public float FallbackWanderRadius => fallbackWanderRadius;

    public Vector3 GetEntryLocalPosition(int index)
    {
        if (entryPoints != null && entryPoints.Length > 0)
        {
            Transform point = entryPoints[Mathf.Abs(index) % entryPoints.Length];
            if (point != null)
            {
                return transform.InverseTransformPoint(point.position);
            }
        }

        return GetFallbackLocalPosition(index, fallbackEntryCenterLocalPosition, fallbackEntryRadius, fallbackEntrySpacing);
    }

    public Vector3 GetWanderCenterLocalPosition(int index, Vector3 customCenterLocalPosition, bool useCustomCenter)
    {
        if (wanderCenters != null && wanderCenters.Length > 0)
        {
            Transform point = wanderCenters[Mathf.Abs(index) % wanderCenters.Length];
            if (point != null)
            {
                return transform.InverseTransformPoint(point.position);
            }
        }

        if (useCustomCenter)
            return customCenterLocalPosition;

        float centerSpreadRadius = Mathf.Max(0f, fallbackWanderRadius * 0.5f);
        return GetFallbackLocalPosition(index, fallbackWanderCenterLocalPosition, centerSpreadRadius, fallbackEntrySpacing);
    }

    private static Vector3 GetFallbackLocalPosition(int index, Vector3 center, float baseRadius, float spacing)
    {
        if (index <= 0)
            return center;

        float radius = baseRadius + spacing * Mathf.Floor(index / 8f);
        float angle = index * GoldenAngleDegrees * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
        return center + offset;
    }
}
