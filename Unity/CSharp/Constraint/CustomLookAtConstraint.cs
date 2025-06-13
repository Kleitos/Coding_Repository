using UnityEngine;

[ExecuteAlways]
public class StableAimConstraint : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Aim Settings")]
    public Vector3 localAimAxis = Vector3.forward;
    public Vector3 upAxis = Vector3.up;
    public bool maintainOffset = true;

    [Range(0f, 1f)]
    public float weight = 1f;

    [Header("Rotation Clamping (degrees)")]
    public Vector3 maxRotationAngles = new Vector3(45f, 45f, 45f); // x = pitch, y = yaw, z = roll

    [Header("Freeze Rotation Axes")]
    public bool freezeX;
    public bool freezeY;
    public bool freezeZ;

    [Header("Debug")]
    public bool drawDebug = true;

    private Quaternion bindRotationLocal;
    private Quaternion aimOffset;
    private Vector3 lastValidDirection;

    void OnEnable()
    {
        if (target == null) return;

        bindRotationLocal = transform.localRotation;

        Vector3 toTarget = (target.position - transform.position).normalized;
        if (toTarget != Vector3.zero)
        {
            Quaternion worldToTarget = Quaternion.LookRotation(toTarget, upAxis);
            aimOffset = Quaternion.Inverse(worldToTarget) * transform.rotation;
        }
        else
        {
            aimOffset = Quaternion.identity;
        }

        lastValidDirection = toTarget;
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 toTarget = target.position - transform.position;
        if (toTarget.sqrMagnitude < 0.0001f) return;

        Vector3 aimDir = toTarget.normalized;

        // Flip prevention
        if (Vector3.Dot(lastValidDirection, aimDir) < -0.999f)
        {
            aimDir = lastValidDirection;
        }
        else
        {
            lastValidDirection = aimDir;
        }

        Quaternion targetWorldRot = Quaternion.LookRotation(aimDir, upAxis);
        if (maintainOffset)
        {
            targetWorldRot *= aimOffset;
        }

        Quaternion targetLocalRot = transform.parent
            ? Quaternion.Inverse(transform.parent.rotation) * targetWorldRot
            : targetWorldRot;

        Vector3 targetEuler = NormalizeAngles(targetLocalRot.eulerAngles);
        Vector3 baseEuler = bindRotationLocal.eulerAngles;

        // Clamp to range relative to bind pose
        targetEuler.x = freezeX ? baseEuler.x : Mathf.Clamp(targetEuler.x, baseEuler.x - maxRotationAngles.x, baseEuler.x + maxRotationAngles.x);
        targetEuler.y = freezeY ? baseEuler.y : Mathf.Clamp(targetEuler.y, baseEuler.y - maxRotationAngles.y, baseEuler.y + maxRotationAngles.y);
        targetEuler.z = freezeZ ? baseEuler.z : Mathf.Clamp(targetEuler.z, baseEuler.z - maxRotationAngles.z, baseEuler.z + maxRotationAngles.z);

        Quaternion clampedLocalRot = Quaternion.Euler(targetEuler);
        Quaternion currentLocalRot = transform.localRotation;

        transform.localRotation = Quaternion.Slerp(currentLocalRot, clampedLocalRot, weight);
    }


    private Vector3 NormalizeAngles(Vector3 angles)
    {
        angles.x = NormalizeAngle(angles.x);
        angles.y = NormalizeAngle(angles.y);
        angles.z = NormalizeAngle(angles.z);
        return angles;
    }

    private float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f) angle -= 360f;
        if (angle < -180f) angle += 360f;
        return angle;
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
