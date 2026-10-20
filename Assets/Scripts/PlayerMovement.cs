using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField, Min(0f)] private float moveSpeed = 5f;
    [SerializeField, Min(0f)] private float rotationSpeed = 720f;
    [SerializeField] private Animator animator;

    private Rigidbody body;
    private PlayerFieldBoundary fieldBoundary;
    private Vector2 movementInput;
    private bool movementLocked;
    private Transform playerCamera;
    private Vector3 cameraOffset;
    private Quaternion cameraRotation;

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

        Camera mainCamera = Camera.main;
        if (mainCamera != null && mainCamera.transform.IsChildOf(transform))
        {
            playerCamera = mainCamera.transform;
            cameraOffset = playerCamera.position - transform.position;
            cameraRotation = playerCamera.rotation;
            playerCamera.SetParent(null, true);
        }
    }

    private void Update()
    {
        movementInput = movementLocked
            ? Vector2.zero
            : new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        movementInput = Vector2.ClampMagnitude(movementInput, 1f);

        if (animator != null)
        {
            animator.SetFloat(BlendParameter, movementInput.magnitude * 0.6f, 0.1f, Time.deltaTime);
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

        Vector3 targetVelocity = new Vector3(movementInput.x, 0f, movementInput.y) * moveSpeed;

        if (fieldBoundary != null)
        {
            Vector3 nextPosition = body.position + targetVelocity * Time.fixedDeltaTime;
            Vector3 clampedPosition = fieldBoundary.ClampPosition(nextPosition);
            targetVelocity = (clampedPosition - body.position) / Time.fixedDeltaTime;
        }

        body.velocity = new Vector3(targetVelocity.x, 0f, targetVelocity.z);
        Vector3 movementDirection = new Vector3(movementInput.x, 0f, movementInput.y);
        if (movementDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(movementDirection, Vector3.up);
            Quaternion nextRotation = Quaternion.RotateTowards(
                body.rotation,
                targetRotation,
                rotationSpeed * Time.fixedDeltaTime);
            body.MoveRotation(nextRotation);
        }

        body.angularVelocity = Vector3.zero;
    }

    private void LateUpdate()
    {
        if (playerCamera == null || playerCamera.GetComponent<SoccerBallCameraController>() != null)
        {
            return;
        }

        playerCamera.position = transform.position + cameraOffset;
        playerCamera.rotation = cameraRotation;
    }
}
