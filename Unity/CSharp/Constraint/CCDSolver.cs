using UnityEngine;

public class CCDSolver : MonoBehaviour
{
    public Transform[] joints;
    public Transform target;
    public int maxIterations = 10;
    public float tolerance = 0.001f;
    public float[] jointRotationLimits; // degrees per joint

    [Header("Axis Lock Settings")]
    public bool lockX = false;
    public bool lockY = false;
    public bool lockZ = false;

    void LateUpdate()
    {
        Solve();
    }

    void Solve()
    {
        if (joints.Length < 2 || target == null) return;

        for (int iter = 0; iter < maxIterations; iter++)
        {
            for (int i = joints.Length - 2; i >= 0; i--)
            {
                Transform joint = joints[i];
                Vector3 toEnd = joints[joints.Length - 1].position - joint.position;
                Vector3 toTarget = target.position - joint.position;

                float angle = Vector3.Angle(toEnd, toTarget);
                if (angle < tolerance) continue;

                Vector3 axis = Vector3.Cross(toEnd, toTarget).normalized;
                if (axis.sqrMagnitude < 0.0001f) continue;

                float maxAngle = (i < jointRotationLimits.Length) ? jointRotationLimits[i] : 180f;
                float clampedAngle = Mathf.Min(angle, maxAngle);

                joint.Rotate(axis, clampedAngle, Space.World);

                // Axis Locking
                Vector3 euler = joint.localEulerAngles;
                euler.x = lockX ? 0f : euler.x;
                euler.y = lockY ? 0f : euler.y;
                euler.z = lockZ ? 0f : euler.z;
                joint.localEulerAngles = euler;
            }

            if ((joints[joints.Length - 1].position - target.position).sqrMagnitude < tolerance * tolerance)
                break;
        }
    }
}
