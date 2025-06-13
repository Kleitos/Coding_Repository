using System.Collections;
using UnityEngine;

public class NPCWaveController : BaseProceduralAnimation
{
    public FABRIKSolver ikSolver;
    public Transform restTarget;
    public Transform poleTarget;
    public Transform targetPosition;
    public Transform wristJoint;

    [Header("Wave Motion Settings")]
    [Range(1f, 20f)] public float waveSpeed = 8f;
    [Range(0.01f, 0.2f)] public float waveAmplitude = 0.05f; [Range(0f, 0.1f)]

    [Header("Joint Overrides")]
    public float twistRadius = 0.02f;
    public Vector3 twistAxis = Vector3.forward; // local twist axis (e.g., roll)
    [Range(0f, 45f)] public float twistAngle = 5f;
    [Range(1f, 30f)] public float twistSpeed = 12f;




    [Header("Arm Pose Settings")]
    [Range(0f, 0.5f)] public float shoulderLift = 0.15f;
    [Range(0f, 0.5f)] public float elbowBendAmount = 0.1f;

    [Header("Timing Settings")]
    [Range(1f, 1000f)] public float waveDuration = 5f;
    [Range(0.1f, 3f)] public float transitionDuration = 1f;

    [Header("Smooth Transition")]
    public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Vector3 baseTargetPos;
    private Vector3 basePolePos;

    private bool transitioningIn = false;
    private bool transitioningOut = false;
    private float transitionTimer;
    private float waveTimer;
    private Quaternion baseWristLocalRotation;

    private Vector3 transitionStartTarget;
    private Vector3 transitionStartPole;

    protected override void Awake()
    {
        base.Awake();
        baseWristLocalRotation = wristJoint.localRotation;
        baseTargetPos = restTarget.position;
        basePolePos = poleTarget.position;

        ikSolver.target.position = baseTargetPos;
        poleTarget.position = basePolePos;

        Object.FindFirstObjectByType<AnimationManager>()?.Register(this);
    }

    protected override IEnumerator Animate()
    {
        transitioningIn = true;
        transitionTimer = 0f;

        // Transition in
        while (transitioningIn)
        {
            transitionTimer += Time.deltaTime;
            float t = Mathf.Clamp01(transitionTimer / transitionDuration);
            float curvedT = transitionCurve.Evaluate(t);

            ikSolver.target.position = Vector3.Lerp(baseTargetPos, GetWaveTarget(), curvedT);
            poleTarget.position = Vector3.Lerp(basePolePos, GetWavePole(), curvedT);

            if (transitionTimer >= transitionDuration)
            {
                transitioningIn = false;
                waveTimer = 0f;
            }

            yield return null;
        }

        // Wave motion
        Quaternion baseRotation = wristJoint.localRotation;

        while (waveTimer < waveDuration)
        {
            waveTimer += Time.deltaTime;

            float flick = Mathf.Sin(Time.time * waveSpeed) * waveAmplitude;
            Vector3 twistOffset = GetTwistOffset();

            // IK target position (affects full chain)
            ikSolver.target.position = GetWaveTarget()
                + restTarget.right * flick;

            poleTarget.position = GetWavePole();

            // Apply twist to the wrist after IK
            ApplyWristTwist();

            yield return null;
        }

        transitioningOut = true;
        transitionStartTarget = ikSolver.target.position;
        transitionStartPole = poleTarget.position;
        transitionTimer = 0f;

        // Transition out
        while (transitioningOut)
        {
            transitionTimer += Time.deltaTime;
            float t = Mathf.Clamp01(transitionTimer / transitionDuration);
            float curvedT = transitionCurve.Evaluate(t);

            ikSolver.target.position = Vector3.Lerp(transitionStartTarget, baseTargetPos, curvedT);
            poleTarget.position = Vector3.Lerp(transitionStartPole, basePolePos, curvedT);

            if (transitionTimer >= transitionDuration)
                transitioningOut = false;

            yield return null;
        }

        Stop(); // Optional: auto stop and reset to rest
    }

    private Vector3 GetWaveTarget()
    {
        if (targetPosition != null)
        {
            Vector3 toPlayer = (targetPosition.position - baseTargetPos).normalized;
            Vector3 forwardOffset = toPlayer * 0.2f;
            Vector3 verticalOffset = Vector3.up * shoulderLift;
            return baseTargetPos + forwardOffset + verticalOffset;
        }

        return baseTargetPos + new Vector3(0, shoulderLift, 0);
    }

    private void ApplyWristTwist()
    {
        if (wristJoint == null) return;

        float angle = Mathf.Sin(Time.time * twistSpeed) * twistAngle;
        Quaternion twistRotation = Quaternion.AngleAxis(angle, twistAxis.normalized);
        wristJoint.localRotation = baseWristLocalRotation * twistRotation;
    }


    private Vector3 GetTwistOffset()
    {
        // Get world-space twist axis (e.g., roll axis from restTarget)
        Vector3 axis = restTarget.TransformDirection(twistAxis.normalized);

        // Generate a perpendicular vector for circular offset
        Vector3 radial = Vector3.Cross(axis, Vector3.up);
        if (radial == Vector3.zero)
            radial = Vector3.Cross(axis, Vector3.forward);

        // Animate circular twist
        float angle = Mathf.Sin(Time.time * waveSpeed) * 360f;
        Quaternion twistRot = Quaternion.AngleAxis(angle, axis);
        return twistRot * radial.normalized * twistRadius;
    }


    private Vector3 GetWavePole()
    {
        if (targetPosition != null)
        {
            Vector3 toPlayer = (targetPosition.position - basePolePos).normalized;
            return basePolePos + toPlayer * elbowBendAmount;
        }

        return basePolePos + restTarget.forward * elbowBendAmount;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !IsPlaying)
        {
            Play(); // Start waving
        }
    }
}
