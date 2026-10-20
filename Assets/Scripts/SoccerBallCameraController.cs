using UnityEngine;

public class SoccerBallCameraController : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float cameraSmoothTime = 0.18f;
    [SerializeField, Min(0f)] private float goalHoldDuration = 2f;
    [SerializeField, Min(0.1f)] private float returnDuration = 0.8f;
    [SerializeField, Min(1f)] private float failedShotFollowTimeout = 4f;

    private enum CameraMode
    {
        Player,
        FollowingBall,
        HoldingAtGoal,
        ReturningToPlayer
    }

    private CameraMode mode = CameraMode.Player;
    private PlayerMovement playerMovement;
    private Transform playerTransform;
    private SoccerBallPhysics followedBall;
    private Transform followedGoal;
    private GameObject goalConfettiPrefab;
    private Vector3 kickDirection;
    private Vector3 normalWorldPosition;
    private Vector3 normalWorldOffset;
    private Quaternion normalWorldRotation;
    private Vector3 positionSmoothVelocity;
    private float followStartedAt;
    private bool goalReachedForCurrentShot;
    private float goalReachedAt;
    private float returnStartedAt;
    private bool confettiSpawnedForCurrentShot;
    private Vector3 returnStartWorldPosition;

    private void Awake()
    {
        playerMovement = GetComponentInParent<PlayerMovement>();
        if (playerMovement == null)
        {
            playerMovement = FindObjectOfType<PlayerMovement>();
        }

        playerTransform = playerMovement != null ? playerMovement.transform : transform.parent;
        normalWorldPosition = transform.position;
        normalWorldOffset = playerTransform != null
            ? normalWorldPosition - playerTransform.position
            : Vector3.zero;
        normalWorldRotation = transform.rotation;
        transform.SetParent(null, true);
    }

    public void BeginBallFollow(
        SoccerBallPhysics ball,
        Transform goal,
        Vector3 direction,
        GameObject confettiPrefab)
    {
        if (ball == null || goal == null)
        {
            return;
        }

        followedBall = ball;
        followedGoal = goal;
        goalConfettiPrefab = confettiPrefab;
        confettiSpawnedForCurrentShot = false;
        goalReachedForCurrentShot = false;
        kickDirection = Vector3.ProjectOnPlane(direction, Vector3.up).normalized;
        if (kickDirection.sqrMagnitude < 0.001f)
        {
            kickDirection = Vector3.forward;
        }

        followStartedAt = Time.time;
        goalReachedAt = -1f;
        positionSmoothVelocity = Vector3.zero;
        mode = CameraMode.FollowingBall;

        if (playerMovement == null)
        {
            playerMovement = GetComponentInParent<PlayerMovement>();
        }
        if (playerTransform == null && playerMovement != null)
        {
            playerTransform = playerMovement.transform;
        }

        if (playerMovement != null)
        {
            playerMovement.SetMovementLocked(true);
        }
    }

    public void RegisterGoal(
        SoccerBallPhysics ball,
        Transform goal,
        GameObject confettiPrefab)
    {
        if (ball == null || goal == null)
        {
            return;
        }

        followedBall = ball;
        followedGoal = goal;
        goalConfettiPrefab = confettiPrefab;
        confettiSpawnedForCurrentShot = false;
        goalReachedForCurrentShot = true;
        if (!confettiSpawnedForCurrentShot)
        {
            SpawnGoalConfetti();
            StopBallAtGoal();
        }
    }

    private void LateUpdate()
    {
        switch (mode)
        {
            case CameraMode.Player:
                if (playerTransform != null)
                {
                    transform.position = playerTransform.position + normalWorldOffset;
                }

                transform.rotation = normalWorldRotation;
                break;
            case CameraMode.FollowingBall:
            case CameraMode.HoldingAtGoal:
                UpdateBallView();
                break;
            case CameraMode.ReturningToPlayer:
                UpdateReturnToPlayerView();
                break;
        }
    }

    private void UpdateBallView()
    {
        if (followedBall == null || followedGoal == null)
        {
            BeginReturnToPlayer();
            return;
        }

        // A shot that has not reached the goal must never keep control of the camera
        // beyond this deadline, including when the ball slows or leaves the camera view.
        if (!goalReachedForCurrentShot && Time.time - followStartedAt >= failedShotFollowTimeout)
        {
            BeginReturnToPlayer();
            return;
        }

        if (mode == CameraMode.FollowingBall)
        {
            bool reachedGoal = IsBallAtGoal(followedBall, followedGoal);
            Rigidbody ballBody = followedBall.GetComponent<Rigidbody>();
            bool stoppedNearGoal = Time.time - followStartedAt > 1f
                && ballBody != null
                && ballBody.velocity.magnitude < 0.8f
                && Vector3.Distance(followedBall.transform.position, GetGoalCenter(followedGoal)) < 4f;
            if (reachedGoal || stoppedNearGoal)
            {
                if (reachedGoal)
                {
                    goalReachedForCurrentShot = true;
                    if (!confettiSpawnedForCurrentShot)
                    {
                        SpawnGoalConfetti();
                        StopBallAtGoal();
                    }
                }

                mode = CameraMode.HoldingAtGoal;
                goalReachedAt = Time.time;
            }
        }
        else if (Time.time - goalReachedAt >= goalHoldDuration)
        {
            BeginReturnToPlayer();
            return;
        }

        Vector3 targetPosition = followedBall.transform.position;
        Vector3 cameraPosition = targetPosition + normalWorldOffset;
        transform.position = Vector3.SmoothDamp(
            transform.position,
            cameraPosition,
            ref positionSmoothVelocity,
            cameraSmoothTime);
        transform.rotation = normalWorldRotation;
    }

    private void SpawnGoalConfetti()
    {
        confettiSpawnedForCurrentShot = true;
        if (goalConfettiPrefab == null || followedBall == null)
        {
            return;
        }

        GameObject confetti = Instantiate(
            goalConfettiPrefab,
            followedBall.transform.position,
            Quaternion.identity);

        ParticleSystem[] particleSystems = confetti.GetComponentsInChildren<ParticleSystem>(true);
        float cleanupDelay = 0f;
        foreach (ParticleSystem particleSystem in particleSystems)
        {
            particleSystem.Play(true);
            ParticleSystem.MainModule main = particleSystem.main;
            float lifetime = main.startLifetime.constantMax;
            cleanupDelay = Mathf.Max(cleanupDelay, main.duration + lifetime);
        }

        Destroy(confetti, Mathf.Max(cleanupDelay + 0.5f, 1f));
    }

    private void StopBallAtGoal()
    {
        if (followedBall == null)
        {
            return;
        }

        Rigidbody ballBody = followedBall.GetComponent<Rigidbody>();
        if (ballBody == null)
        {
            return;
        }

        ballBody.velocity = Vector3.zero;
        ballBody.angularVelocity = Vector3.zero;
        ballBody.isKinematic = false;
    }

    private void BeginReturnToPlayer()
    {
        mode = CameraMode.ReturningToPlayer;
        returnStartedAt = Time.time;
        returnStartWorldPosition = transform.position;
        followedBall = null;
        followedGoal = null;
    }

    private void UpdateReturnToPlayerView()
    {
        float progress = Mathf.Clamp01((Time.time - returnStartedAt) / returnDuration);
        float easedProgress = progress * progress * (3f - 2f * progress);
        Vector3 targetPosition = playerTransform != null
            ? playerTransform.position + normalWorldOffset
            : normalWorldPosition;
        transform.position = Vector3.Lerp(returnStartWorldPosition, targetPosition, easedProgress);
        transform.rotation = Quaternion.Slerp(transform.rotation, normalWorldRotation, easedProgress);

        if (progress >= 1f)
        {
            mode = CameraMode.Player;
            transform.rotation = normalWorldRotation;
            if (playerMovement != null)
            {
                playerMovement.SetMovementLocked(false);
            }
        }
    }

    private bool IsBallAtGoal(SoccerBallPhysics ball, Transform goal)
    {
        MeshCollider goalMeshCollider = goal.GetComponent<MeshCollider>();
        MeshFilter goalMeshFilter = goal.GetComponent<MeshFilter>();
        Mesh goalMesh = goalMeshCollider != null && goalMeshCollider.sharedMesh != null
            ? goalMeshCollider.sharedMesh
            : goalMeshFilter != null ? goalMeshFilter.sharedMesh : null;
        if (goalMesh == null)
        {
            return false;
        }

        Bounds localGoalBounds = goalMesh.bounds;
        SphereCollider ballCollider = ball.GetComponent<SphereCollider>();
        Vector3 ballScale = ball.transform.lossyScale;
        float ballWorldRadius = ballCollider != null
            ? ballCollider.radius * Mathf.Max(Mathf.Abs(ballScale.x), Mathf.Abs(ballScale.y), Mathf.Abs(ballScale.z))
            : 0.12f;

        Vector3 goalScale = goal.lossyScale;
        Vector3 localBallRadius = new Vector3(
            ballWorldRadius / Mathf.Max(Mathf.Abs(goalScale.x), 0.001f),
            ballWorldRadius / Mathf.Max(Mathf.Abs(goalScale.y), 0.001f),
            ballWorldRadius / Mathf.Max(Mathf.Abs(goalScale.z), 0.001f));
        Vector3 localBallPosition = goal.InverseTransformPoint(
            ballCollider != null ? ballCollider.bounds.center : ball.transform.position);

        float sideClearance = localBallRadius.x + 0.08f / Mathf.Max(Mathf.Abs(goalScale.x), 0.001f);
        float topClearance = localBallRadius.y + 0.08f / Mathf.Max(Mathf.Abs(goalScale.y), 0.001f);
        float bottomClearance = localBallRadius.y * 0.5f;

        bool insideMouthWidth = localBallPosition.x > localGoalBounds.min.x + sideClearance
            && localBallPosition.x < localGoalBounds.max.x - sideClearance;
        bool insideGoalHeight = localBallPosition.y > localGoalBounds.min.y + bottomClearance
            && localBallPosition.y < localGoalBounds.max.y - topClearance;

        Vector3 localKickDirection = goal.InverseTransformDirection(kickDirection).normalized;
        if (Mathf.Abs(localKickDirection.z) < 0.1f)
        {
            return false;
        }

        float distanceInsideFromEntry = (localBallPosition.z - localGoalBounds.center.z)
            * Mathf.Sign(localKickDirection.z) + localGoalBounds.extents.z;
        float entryClearance = localBallRadius.z + 0.1f / Mathf.Max(Mathf.Abs(goalScale.z), 0.001f);

        return insideMouthWidth && insideGoalHeight && distanceInsideFromEntry > entryClearance;
    }

    private static Vector3 GetGoalCenter(Transform goal)
    {
        Collider goalCollider = GetGoalCollider(goal);
        return goalCollider != null ? goalCollider.bounds.center : goal.position + Vector3.up;
    }

    private static Collider GetGoalCollider(Transform goal)
    {
        MeshCollider meshCollider = goal.GetComponent<MeshCollider>();
        if (meshCollider != null && meshCollider.enabled)
        {
            return meshCollider;
        }

        Collider anyCollider = goal.GetComponent<Collider>();
        return anyCollider != null && anyCollider.enabled ? anyCollider : null;
    }
}
