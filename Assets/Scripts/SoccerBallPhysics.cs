using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
public class SoccerBallPhysics : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float mass = 0.43f;
    [SerializeField, Min(0f)] private float groundClearance = 0f;

    private Rigidbody body;
    private SphereCollider ballCollider;
    private PlayerFieldBoundary fieldBoundary;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        ballCollider = GetComponent<SphereCollider>();

        body.mass = mass;
        body.useGravity = true;
        body.isKinematic = false;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.constraints = RigidbodyConstraints.None;

        fieldBoundary = FindObjectOfType<PlayerFieldBoundary>();
    }

    private void Start()
    {
        GameObject fieldObject = GameObject.Find("land soccer field");
        BoxCollider fieldCollider = fieldObject != null ? fieldObject.GetComponent<BoxCollider>() : null;
        if (fieldCollider == null)
        {
            return;
        }

        float radius = GetWorldRadius();
        float groundY = fieldCollider.bounds.max.y + radius + groundClearance;
        Vector3 position = body.position;
        if (position.y - radius < fieldCollider.bounds.max.y + groundClearance)
        {
            position.y = groundY;
            body.position = position;
        }
    }

    private void FixedUpdate()
    {
        if (fieldBoundary == null)
        {
            return;
        }

        Vector3 position = body.position;
        Vector3 constrainedPosition = fieldBoundary.ClampPosition(position, GetWorldRadius());
        if ((constrainedPosition - position).sqrMagnitude > 0.000001f)
        {
            body.position = constrainedPosition;
        }
    }

    private float GetWorldRadius()
    {
        Vector3 scale = transform.lossyScale;
        float largestScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        return ballCollider.radius * largestScale;
    }
}
