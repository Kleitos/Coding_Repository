using UnityEngine;

public class FABRIKSolver : MonoBehaviour
{
    public Transform[] joints;
    public Transform target;
    public int maxIterations = 10;
    public float tolerance = 0.001f;
    public Transform pole;

    private float[] boneLengths;
    private float totalLength;
    private Vector3[] positions;
    private Quaternion[] bindRotations;
    private Vector3[] bindDirections;

    void Start()
    {
        Init();
    }

    void LateUpdate()
    {
        Solve();
    }

    void Init()
    {
        int boneCount = joints.Length - 1;
        boneLengths = new float[boneCount];
        positions = new Vector3[joints.Length];
        totalLength = 0f;
        bindRotations = new Quaternion[joints.Length];
        bindDirections = new Vector3[joints.Length];

        for (int i = 0; i < boneCount; i++)
        {
            boneLengths[i] = Vector3.Distance(joints[i].position, joints[i + 1].position);
            totalLength += boneLengths[i];

            bindRotations[i] = joints[i].rotation;
            bindDirections[i] = (joints[i + 1].position - joints[i].position).normalized;
        }
    }

    public void Solve()
    {
        if (target == null || joints.Length < 2)
            return;

        for (int i = 0; i < joints.Length; i++)
            positions[i] = joints[i].position;

        Vector3 root = positions[0];

        // Clamp very close targets to avoid instability
        float minTargetDistance = 0.001f;
        Vector3 toTarget = target.position - root;
        if (toTarget.sqrMagnitude < minTargetDistance * minTargetDistance)
            toTarget = toTarget.normalized * minTargetDistance;

        Vector3 endTarget = root + toTarget;

        if (Vector3.Distance(root, endTarget) > totalLength)
        {
            // Stretch chain in a straight line toward target
            for (int i = 0; i < joints.Length - 1; i++)
            {
                float dist = Vector3.Distance(positions[i], endTarget);
                float ratio = boneLengths[i] / dist;
                positions[i + 1] = Vector3.Lerp(positions[i], endTarget, ratio);
            }
        }
        else
        {
            // FABRIK iterations
            for (int iteration = 0; iteration < maxIterations; iteration++)
            {
                // Backward
                positions[positions.Length - 1] = endTarget;
                for (int i = joints.Length - 2; i >= 0; i--)
                {
                    Vector3 dir = (positions[i] - positions[i + 1]).normalized;
                    positions[i] = positions[i + 1] + dir * boneLengths[i];
                }

                // Forward
                positions[0] = root;
                for (int i = 0; i < joints.Length - 1; i++)
                {
                    Vector3 dir = (positions[i + 1] - positions[i]).normalized;
                    positions[i + 1] = positions[i] + dir * boneLengths[i];
                }

                if (Vector3.Distance(positions[positions.Length - 1], endTarget) < tolerance)
                    break;
                // Clamp joints that are collapsing onto the root or each other
                for (int i = 1; i < joints.Length; i++)
                {
                    float minSeparation = 0.001f;
                    if ((positions[i] - positions[i - 1]).sqrMagnitude < minSeparation * minSeparation)
                    {
                        Vector3 dir = (positions[i] - positions[i - 1]).normalized;
                        if (dir.sqrMagnitude < 0.001f)
                            dir = Vector3.forward; // fallback if direction is invalid

                        positions[i] = positions[i - 1] + dir * minSeparation;
                    }
                }
            }
        }

        // Pole Vector Correction (AFTER FABRIK)
        if (pole != null)
        {
            for (int i = 1; i < joints.Length - 1; i++)
            {
                Vector3 prev = positions[i - 1];
                Vector3 next = positions[i + 1];
                Vector3 current = positions[i];

                Plane plane = new Plane(next - prev, prev);
                Vector3 projectedPole = plane.ClosestPointOnPlane(pole.position);
                Vector3 projectedJoint = plane.ClosestPointOnPlane(current);

                float angle = Vector3.SignedAngle(projectedJoint - prev, projectedPole - prev, plane.normal);
                positions[i] = Quaternion.AngleAxis(angle, plane.normal) * (current - prev) + prev;
                angle = Mathf.Clamp(angle, -160f, 160f); // prevent over-twisting
                float poleWeight = 0.5f; // 0 = no pole, 1 = full pole
                positions[i] = Vector3.Lerp(current, Quaternion.AngleAxis(angle, plane.normal) * (current - prev) + prev, poleWeight);


            }
        }

        // Apply solved positions
        for (int i = 0; i < joints.Length; i++)
            joints[i].position = positions[i];

        // Apply rotations
        for (int i = 0; i < joints.Length - 1; i++)
        {
            Vector3 dir = positions[i + 1] - positions[i];

            if (dir.sqrMagnitude > 0.0001f)
            {
                Vector3 newDir = dir.normalized;
                Vector3 bindDir = bindDirections[i];
                float dot = Vector3.Dot(newDir, bindDir);

                if (dot < -0.999f)
                {
                    // Prevent flipping
                    Vector3 fallbackAxis = Vector3.Cross(bindDir, Vector3.up);
                    if (fallbackAxis.sqrMagnitude < 0.0001f)
                        fallbackAxis = Vector3.Cross(bindDir, Vector3.right);

                    joints[i].rotation = Quaternion.AngleAxis(180f, fallbackAxis.normalized) * bindRotations[i];
                }
                else if (dot < -0.95f)
                {
                    // Smooth blend near inversion
                    Quaternion fromTo = Quaternion.FromToRotation(bindDir, newDir);
                    float angleBetween = Vector3.Angle(bindDir, newDir);
                    float t = Mathf.InverseLerp(0f, 180f, angleBetween); // normalize
                    Quaternion smoothed = Quaternion.Slerp(Quaternion.identity, fromTo, t);
                    joints[i].rotation = smoothed * bindRotations[i];
                }
                else
                {
                    Quaternion fromTo = Quaternion.FromToRotation(bindDir, newDir);
                    joints[i].rotation = fromTo * bindRotations[i];
                }
            }
        }
    }

    public void Solve(Vector3 newTargetPosition)
    {
        if (target == null)
        {
            Debug.LogWarning("FABRIKSolver has no target assigned — creating temporary target.");
            GameObject tempTarget = new GameObject("FABRIK_TempTarget");
            target = tempTarget.transform;
        }

        target.position = newTargetPosition;
        Solve(); // Call the original solve with the updated target
    }
}
