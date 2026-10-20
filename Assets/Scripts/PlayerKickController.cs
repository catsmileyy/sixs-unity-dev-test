using UnityEngine;

public class PlayerKickController : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float kickRange = 1.6f;
    [SerializeField, Min(0.1f)] private float kickSpeed = 27f;
    [SerializeField] private GameObject goalConfettiPrefab;

    private SoccerBallPhysics nearbyBall;

    private void Awake()
    {
        ConfigureGoalMeshColliders();
    }

    private static void ConfigureGoalMeshColliders()
    {
        Transform[] sceneTransforms = FindObjectsOfType<Transform>();
        foreach (Transform goal in sceneTransforms)
        {
            if (!goal.name.StartsWith("soccer goal", System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            MeshFilter meshFilter = goal.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null)
            {
                continue;
            }

            BoxCollider boxCollider = goal.GetComponent<BoxCollider>();
            if (boxCollider != null)
            {
                boxCollider.enabled = false;
            }

            MeshCollider meshCollider = goal.GetComponent<MeshCollider>();
            if (meshCollider == null)
            {
                meshCollider = goal.gameObject.AddComponent<MeshCollider>();
            }

            meshCollider.convex = false;
            meshCollider.sharedMesh = null;
            meshCollider.sharedMesh = meshFilter.sharedMesh;
            meshCollider.enabled = true;
        }
    }

    private void Update()
    {
        UpdateNearbyBall();

        if (nearbyBall != null && Input.GetKeyDown(KeyCode.Space))
        {
            KickNearestBall();
        }
    }

    private void UpdateNearbyBall()
    {
        nearbyBall = null;
        float nearestDistance = kickRange;
        SoccerBallPhysics[] balls = FindObjectsOfType<SoccerBallPhysics>();

        foreach (SoccerBallPhysics ball in balls)
        {
            if (ball == null)
            {
                continue;
            }

            Vector3 offset = ball.transform.position - transform.position;
            offset.y = 0f;
            float distance = offset.magnitude;
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearbyBall = ball;
            }
        }

    }

    public bool HasNearbyBall => nearbyBall != null;

    public void KickNearestBall()
    {
        KickBall(nearbyBall);
    }

    public void KickFarthestBall()
    {
        SoccerBallPhysics farthestBall = null;
        float farthestDistanceSquared = float.NegativeInfinity;
        SoccerBallPhysics[] balls = FindObjectsOfType<SoccerBallPhysics>();

        foreach (SoccerBallPhysics ball in balls)
        {
            if (ball == null)
            {
                continue;
            }

            Vector3 offset = ball.transform.position - transform.position;
            offset.y = 0f;
            float distanceSquared = offset.sqrMagnitude;
            if (distanceSquared > farthestDistanceSquared)
            {
                farthestDistanceSquared = distanceSquared;
                farthestBall = ball;
            }
        }

        KickBall(farthestBall);
    }

    private void KickBall(SoccerBallPhysics ball)
    {
        if (ball == null)
        {
            return;
        }

        Transform nearestGoal = FindNearestGoal(ball.transform.position);
        Rigidbody ballBody = ball.GetComponent<Rigidbody>();
        if (nearestGoal == null || ballBody == null)
        {
            return;
        }

        Vector3 aimPoint = GetGoalAimPoint(nearestGoal);
        Vector3 toGoal = aimPoint - ball.transform.position;
        Vector3 horizontalDirection = Vector3.ProjectOnPlane(toGoal, Vector3.up);
        float horizontalDistance = horizontalDirection.magnitude;
        if (horizontalDistance < 0.01f)
        {
            return;
        }

        horizontalDirection /= horizontalDistance;
        Vector3 kickVelocity = CalculateKickVelocity(
            horizontalDirection,
            horizontalDistance,
            toGoal.y);
        ballBody.velocity = kickVelocity;
        ballBody.angularVelocity = Vector3.Cross(Vector3.up, horizontalDirection) * (kickSpeed * 1.5f);

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            SoccerBallCameraController cameraController = mainCamera.GetComponent<SoccerBallCameraController>();
            if (cameraController == null)
            {
                cameraController = mainCamera.gameObject.AddComponent<SoccerBallCameraController>();
            }

            cameraController.BeginBallFollow(ball, nearestGoal, horizontalDirection, goalConfettiPrefab);
        }

        if (ball == nearbyBall)
        {
            nearbyBall = null;
        }
    }

    private Vector3 CalculateKickVelocity(Vector3 horizontalDirection, float horizontalDistance, float heightDifference)
    {
        float gravity = Mathf.Abs(Physics.gravity.y);
        float speedSquared = kickSpeed * kickSpeed;
        float discriminant = speedSquared * speedSquared
            - gravity * (gravity * horizontalDistance * horizontalDistance
                + 2f * heightDifference * speedSquared);

        if (discriminant >= 0f && gravity > 0.001f)
        {
            float tangent = (speedSquared - Mathf.Sqrt(discriminant))
                / (gravity * horizontalDistance);
            float horizontalSpeed = kickSpeed / Mathf.Sqrt(1f + tangent * tangent);
            float verticalSpeed = horizontalSpeed * tangent;
            return horizontalDirection * horizontalSpeed + Vector3.up * verticalSpeed;
        }

        return (horizontalDirection + Vector3.up * (heightDifference / horizontalDistance)).normalized * kickSpeed;
    }

    private static Vector3 GetGoalAimPoint(Transform goal)
    {
        MeshCollider meshCollider = goal.GetComponent<MeshCollider>();
        Collider goalCollider = meshCollider != null && meshCollider.enabled
            ? (Collider)meshCollider
            : goal.GetComponent<Collider>();
        if (goalCollider != null)
        {
            Bounds bounds = goalCollider.bounds;
            return new Vector3(bounds.center.x, bounds.center.y, bounds.center.z);
        }

        return goal.position + Vector3.up;
    }

    private Transform FindNearestGoal(Vector3 fromPosition)
    {
        Transform nearestGoal = null;
        float nearestDistanceSquared = float.PositiveInfinity;
        Transform[] allTransforms = FindObjectsOfType<Transform>();

        foreach (Transform candidate in allTransforms)
        {
            if (!candidate.name.StartsWith("soccer goal", System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            Vector3 offset = candidate.position - fromPosition;
            offset.y = 0f;
            float distanceSquared = offset.sqrMagnitude;
            if (distanceSquared < nearestDistanceSquared)
            {
                nearestDistanceSquared = distanceSquared;
                nearestGoal = candidate;
            }
        }

        return nearestGoal;
    }

}
