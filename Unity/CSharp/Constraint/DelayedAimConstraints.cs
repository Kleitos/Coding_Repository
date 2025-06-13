using UnityEngine;

public class DelayedAimConstraints : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Aim Settings")]
    public Vector3 localAimAxis = Vector3.forward;
    public Vector3 upAxis = Vector3.up;
    public bool maintainOffset = true;

    [Range(0f, 1f)]
    public float weight = 1f;

    [Header("Rotation Clamping")]
    public float maxRotationAngle = 180f;

    [Header("Freeze Rotation Axes")]
    public bool freezeX;
    public bool freezeY;
    public bool freezeZ;

    [Header("Timing")]
    public float activationDelay = 0f; // Delay in seconds before constraint activates
    public float transitionSpeed = 5f; // Speed of the transition after the delay

    [Header("Debug")]
    public bool drawDebug = true;

    private Quaternion bindRotationLocal;
    private Quaternion initialWorldToLocal;
    private Vector3 lastValidDirection;
    private float elapsedTime;
    private Quaternion targetRotation;

    void OnEnable()
    {
        elapsedTime = 0f;

        if (target == null) return;

        bindRotationLocal = transform.localRotation;
        initialWorldToLocal = Quaternion.Inverse(transform.rotation) * Quaternion.LookRotation(
            (target.position - transform.position).normalized, upAxis);

        lastValidDirection = (target.position - transform.position).normalized;
    }

    void LateUpdate()
    {
        // Wait until delay has passed
        if (elapsedTime < activationDelay)
        {
            elapsedTime += Time.deltaTime;
            return;
        }

        if (target == null) return;

        Vector3 toTarget = (target.position - transform.position);
        if (toTarget.sqrMagnitude < 0.0001f)
            return;

        Vector3 aimDir = toTarget.normalized;

        // Flip prevention (check for backward direction)
        float dot = Vector3.Dot(lastValidDirection, aimDir);
        if (dot < -0.999f)
        {
            aimDir = lastValidDirection; // prevent flipping
        }
        else
        {
            lastValidDirection = aimDir;
        }

        Quaternion worldRotation = Quaternion.LookRotation(aimDir, upAxis);

        if (maintainOffset)
        {
            worldRotation *= Quaternion.Inverse(initialWorldToLocal);
        }

        // Convert to local
        Quaternion localRotation = transform.parent ? Quaternion.Inverse(transform.parent.rotation) * worldRotation : worldRotation;

        // Clamp rotation angle
        Quaternion currentLocal = transform.localRotation;
        Quaternion delta = Quaternion.Inverse(currentLocal) * localRotation;
        delta.ToAngleAxis(out float angle, out Vector3 axis);

        if (angle > 180f) angle -= 360f;
        angle = Mathf.Clamp(angle, -maxRotationAngle, maxRotationAngle);

        localRotation = currentLocal * Quaternion.AngleAxis(angle, axis);

        // Freeze axes
        Vector3 finalEuler = localRotation.eulerAngles;
        Vector3 bindEuler = bindRotationLocal.eulerAngles;

        if (freezeX) finalEuler.x = bindEuler.x;
        if (freezeY) finalEuler.y = bindEuler.y;
        if (freezeZ) finalEuler.z = bindEuler.z;

        targetRotation = Quaternion.Euler(finalEuler);

        // Smooth transition after delay
        if (elapsedTime >= activationDelay)
        {
            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime * transitionSpeed);
        }
    }

    void OnDrawGizmos()
    {
        if (!drawDebug || target == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, target.position);

        Gizmos.color = Color.green;
        Vector3 aimDir = transform.TransformDirection(localAimAxis);
        Gizmos.DrawRay(transform.position, aimDir * 0.5f);
    }
}
