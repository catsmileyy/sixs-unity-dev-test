using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField, Min(0f)] private float moveSpeed = 5f;
    [SerializeField, Min(0f)] private float turnSpeed = 180f;
    [SerializeField] private Animator animator;

    private Rigidbody body;
    private PlayerFieldBoundary fieldBoundary;
    private float turnInput;
    private float moveInput;
    private bool movementLocked;

    private static readonly int BlendParameter = Animator.StringToHash("Blend");

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        if (body == null)
        {
            body = gameObject.AddComponent<Rigidbody>();
        }

        CapsuleCollider playerCollider = GetComponent<CapsuleCollider>();
        if (playerCollider == null)
        {
            playerCollider = gameObject.AddComponent<CapsuleCollider>();
        }

        playerCollider.direction = 1;
        playerCollider.center = new Vector3(0f, 1f, 0f);
        playerCollider.height = 2f;
        playerCollider.radius = 0.35f;

        body.mass = 70f;
        body.useGravity = false;
        body.isKinematic = false;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.constraints = RigidbodyConstraints.FreezePositionY
            | RigidbodyConstraints.FreezeRotationX
            | RigidbodyConstraints.FreezeRotationZ;

        fieldBoundary = GetComponent<PlayerFieldBoundary>();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

    }

    private void Update()
    {
        turnInput = movementLocked ? 0f : Input.GetAxisRaw("Horizontal");
        moveInput = movementLocked ? 0f : Input.GetAxisRaw("Vertical");

        if (animator != null)
        {
            animator.SetFloat(BlendParameter, Mathf.Abs(moveInput) * 0.6f, 0.1f, Time.deltaTime);
        }
    }

    public void SetMovementLocked(bool locked)
    {
        movementLocked = locked;
        if (locked && body != null)
        {
            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
    }

    private void FixedUpdate()
    {
        if (movementLocked)
        {
            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            return;
        }

        Quaternion nextRotation = body.rotation
            * Quaternion.Euler(0f, turnInput * turnSpeed * Time.fixedDeltaTime, 0f);
        body.MoveRotation(nextRotation);

        Vector3 targetVelocity = (nextRotation * Vector3.forward) * (moveInput * moveSpeed);

        if (fieldBoundary != null)
        {
            Vector3 nextPosition = body.position + targetVelocity * Time.fixedDeltaTime;
            Vector3 clampedPosition = fieldBoundary.ClampPosition(nextPosition);
            targetVelocity = (clampedPosition - body.position) / Time.fixedDeltaTime;
        }

        body.velocity = new Vector3(targetVelocity.x, 0f, targetVelocity.z);
        body.angularVelocity = Vector3.zero;
    }
}
