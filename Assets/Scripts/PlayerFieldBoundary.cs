using UnityEngine;

public class PlayerFieldBoundary : MonoBehaviour
{
    [SerializeField] private BoxCollider playingFieldBounds;
    [Tooltip("Inset from the collider edge in world units.")]
    [SerializeField, Min(0f)] private float edgePadding = 0.5f;

    public Vector3 ClampPosition(Vector3 worldPosition, float additionalInset = 0f)
    {
        if (playingFieldBounds == null)
        {
            return worldPosition;
        }

        Transform boundsTransform = playingFieldBounds.transform;
        Vector3 localPosition = boundsTransform.InverseTransformPoint(worldPosition);
        Vector3 center = playingFieldBounds.center;
        Vector3 halfSize = playingFieldBounds.size * 0.5f;
        Vector3 scale = boundsTransform.lossyScale;
        float worldInset = edgePadding + Mathf.Max(0f, additionalInset);
        float localPaddingX = worldInset / Mathf.Max(Mathf.Abs(scale.x), 0.0001f);
        float localPaddingZ = worldInset / Mathf.Max(Mathf.Abs(scale.z), 0.0001f);
        float paddingX = Mathf.Min(localPaddingX, halfSize.x * 0.45f);
        float paddingZ = Mathf.Min(localPaddingZ, halfSize.z * 0.45f);

        localPosition.x = Mathf.Clamp(
            localPosition.x,
            center.x - halfSize.x + paddingX,
            center.x + halfSize.x - paddingX);
        localPosition.z = Mathf.Clamp(
            localPosition.z,
            center.z - halfSize.z + paddingZ,
            center.z + halfSize.z - paddingZ);

        return boundsTransform.TransformPoint(localPosition);
    }
}
