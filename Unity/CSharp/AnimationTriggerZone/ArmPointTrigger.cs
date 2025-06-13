using UnityEngine;

public class ArmPointTrigger : MonoBehaviour
{
    public FABRIKSolver ikSolver;
    public FABRIKRestPoseController restPoseController;


    [Header("Pointing Target")]
    public Transform pointTarget;    // Where the hand should point
    [Tooltip("Optional: If left empty, elbow guide will be auto-placed.")]
    public Transform elbowGuide;     // Used as a pole direction for natural elbow bend


    [Header("Smoothing Settings")]
    public float transitionToPointDuration = 1f;
    public AnimationCurve pointEaseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    public float transitionToRestDuration = 0.5f;
    public AnimationCurve restEaseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("End Effector Twist (Optional)")]
    public bool applyHandTwist = false;
    [Tooltip("Axis on the hand to align (e.g., right for palm forward).")]
    public Vector3 handLocalAxis = Vector3.right;
    [Tooltip("World direction the hand axis should point to.")]
    public Vector3 targetWorldDirection = Vector3.right;
    [Range(0f, 1f)] public float twistWeight = 1f;


    private Vector3 originalTargetPos;
    private Vector3 originalPolePos;
    private Quaternion originalWristLocalRotation;
    private Transform wrist;


    private Coroutine activeRoutine;

    private Vector3 GetAutoPolePosition()
    {
        if (ikSolver.joints.Length < 3)
        {
            Debug.LogWarning("FABRIK chain too short for auto pole placement.");
            return ikSolver.joints[0].position + Vector3.up; // fallback
        }

        Transform shoulder = ikSolver.joints[0];
        Transform elbow = ikSolver.joints[1];
        Transform wrist = ikSolver.joints[2];

        Vector3 dir = (wrist.position - shoulder.position).normalized;
        Vector3 elbowDir = Vector3.Cross(dir, Vector3.up).normalized;

        float elbowOffset = Vector3.Distance(shoulder.position, wrist.position) * 0.5f;
        return elbow.position + elbowDir * elbowOffset;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (ikSolver == null || pointTarget == null)
        {
            Debug.LogWarning("Missing IK Solver or Point Target.");
            return;
        }

        if (elbowGuide == null)
        {
            GameObject guide = new GameObject("AutoElbowGuide");
            guide.transform.SetParent(transform);
            guide.transform.position = GetAutoPolePosition();
            elbowGuide = guide.transform;
        }

        StoreOriginalPose();

        // Cache wrist
        wrist = ikSolver.joints[^1];
        originalWristLocalRotation = wrist.localRotation;

        if (activeRoutine != null) StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(
            BlendToPose(pointTarget.position, elbowGuide.position,
                transitionToPointDuration, pointEaseCurve, applyTwist: true));
    }


    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (restPoseController == null) return;

        if (activeRoutine != null) StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(
            BlendToPose(restPoseController.restTarget.position,
                        restPoseController.restPole.position,
                        transitionToRestDuration,
                        restEaseCurve,
                        applyTwist: false)); // No twist on exit
    }


    private void StoreOriginalPose()
    {
        if (ikSolver.target != null)
            originalTargetPos = ikSolver.target.position;

        if (ikSolver.pole != null)
            originalPolePos = ikSolver.pole.position;
    }

    private System.Collections.IEnumerator BlendToPose(
    Vector3 targetPos,
    Vector3 polePos,
    float duration,
    AnimationCurve curve,
    bool applyTwist)
    {
        float time = 0f;
        Vector3 startTarget = ikSolver.target.position;
        Vector3 startPole = ikSolver.pole.position;

        Quaternion startWristRot = wrist != null ? wrist.localRotation : Quaternion.identity;
        Quaternion endWristRot = applyTwist ? ComputeWristTwistRotation() : originalWristLocalRotation;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = curve.Evaluate(time / duration);

            ikSolver.target.position = Vector3.Lerp(startTarget, targetPos, t);
            ikSolver.pole.position = Vector3.Lerp(startPole, polePos, t);

            if (wrist != null)
                wrist.localRotation = Quaternion.Slerp(startWristRot, endWristRot, t);

            yield return null;
        }

        ikSolver.target.position = targetPos;
        ikSolver.pole.position = polePos;

        if (wrist != null)
            wrist.localRotation = endWristRot;
    }

    private Quaternion ComputeWristTwistRotation()
    {
        if (wrist == null || !applyHandTwist) return wrist.localRotation;

        Vector3 currentWorldAxis = wrist.rotation * handLocalAxis.normalized;
        Quaternion twistDelta = Quaternion.FromToRotation(currentWorldAxis, targetWorldDirection.normalized);
        Quaternion targetWorldRotation = twistDelta * wrist.rotation;

        return wrist.parent != null
            ? Quaternion.Inverse(wrist.parent.rotation) * targetWorldRotation
            : targetWorldRotation;
    }


}
