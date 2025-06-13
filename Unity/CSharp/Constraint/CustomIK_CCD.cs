using UnityEngine;

public class CustomIK_CCDSolver : MonoBehaviour
{
    [Header("Chain Joints (base → tip)")]
    public Transform[] joints;

    [Header("Lock Tip Here")]
    public Transform controller;

    [Header("CCD Settings")]
    public int maxIterations = 10;
    public float tolerance = 0.01f;
    public float rotationLimit = 180f;

    [Header("Freeze Base Joint X-Axis")]
    [Tooltip("Number of joints at the root of the chain to lock local X rotation")]
    public int numBaseJointsToLockX = 2;

    [Header("Rotation Clamping")]
    public float minY = -45f;
    public float maxY = 45f;
    public float minZ = -45f;
    public float maxZ = 45f;

    [Header("Smooth Reset")]
    [Tooltip("Time in seconds to smoothly revert Y and Z axis to bind pose")]
    public float smoothResetDuration = 1f;

    [Header("Tentacle Base Compression")]
    public Transform[] compressibleJoints;
    public Transform bodyRoot; // the body or base of the tentacle
    public float maxCompressionDistance = 1.0f; // when controller is this close or closer, apply max compression
    [Tooltip("Maximum X-axis compression per joint (must match compressibleJoints count)")]
    public float[] maxLocalXCompressionPerJoint;

    public float compressionLerpSpeed = 5f;     // how fast the compression interpolates


    private float[] _originalXAngles;
    private float[] _originalYAngles;
    private float[] _originalZAngles;
    private float[] _xLerpTimers;
    private float[] _yLerpTimers;
    private float[] _zLerpTimers;
    private Vector3[] _originalLocalPositions;


    private bool _isControllerMoving = false;

    void Start()
    {
        // Store the original rotations
        _originalXAngles = new float[joints.Length];
        _originalYAngles = new float[joints.Length];
        _originalZAngles = new float[joints.Length];

        for (int i = 0; i < joints.Length; i++)
        {
            _originalXAngles[i] = joints[i].localEulerAngles.x;
            _originalYAngles[i] = joints[i].localEulerAngles.y;
            _originalZAngles[i] = joints[i].localEulerAngles.z;
        }

        // Initialize timers for smoothing
        _xLerpTimers = new float[joints.Length];
        _yLerpTimers = new float[joints.Length];
        _zLerpTimers = new float[joints.Length];

        if (compressibleJoints != null)
        {
            _originalLocalPositions = new Vector3[compressibleJoints.Length];
            for (int i = 0; i < compressibleJoints.Length; i++)
            {
                _originalLocalPositions[i] = compressibleJoints[i].localPosition;
            }

            if (maxLocalXCompressionPerJoint.Length != compressibleJoints.Length)
            {
                Debug.LogWarning("CustomIK_CCDSolver: maxLocalXCompressionPerJoint size doesn't match compressibleJoints size.");
            }
        }

    }

    void Update()
    {
        // Check if the controller is moving by comparing positions
        _isControllerMoving = Vector3.Distance(controller.position, controller.position) > 0.01f;
    }

    void LateUpdate()
    {
        SolveCCD();
        ApplyBaseCompression();

    }
    void ApplyBaseCompression()
    {
        if (compressibleJoints == null || compressibleJoints.Length == 0 || controller == null || bodyRoot == null)
            return;

        float distance = Vector3.Distance(controller.position, bodyRoot.position);


        for (int i = 0; i < compressibleJoints.Length; i++)
        {
            Transform joint = compressibleJoints[i];
            Vector3 originalPos = _originalLocalPositions[i];
            Vector3 localPos = joint.localPosition;

            float maxCompression = maxLocalXCompressionPerJoint.Length > i ? maxLocalXCompressionPerJoint[i] : 0f;
            float targetOffset = 0f;

            if (distance <= maxCompressionDistance)
            {
                float t = Mathf.InverseLerp(maxCompressionDistance, 0f, distance);
                targetOffset = Mathf.Lerp(0f, maxCompression, t);
            }

            float newX = Mathf.Lerp(localPos.x, originalPos.x + targetOffset, Time.deltaTime * compressionLerpSpeed);
            localPos.x = newX;
            joint.localPosition = localPos;

        }

    }



    void SolveCCD()
    {
        if (joints.Length < 2 || controller == null) return;

        Transform tip = joints[joints.Length - 1];
        Vector3 targetPos = controller.position;

        // Run CCD on joints except the tip
        for (int iter = 0; iter < maxIterations; iter++)
        {
            for (int i = joints.Length - 2; i >= numBaseJointsToLockX; i--)
            {
                Transform joint = joints[i];
                Vector3 toTip = (tip.position - joint.position).normalized;
                Vector3 toTgt = (targetPos - joint.position).normalized;

                if (toTip.sqrMagnitude < 1e-6f || toTgt.sqrMagnitude < 1e-6f) continue;

                Vector3 axis = Vector3.Cross(toTip, toTgt).normalized;
                float angle = Vector3.Angle(toTip, toTgt);
                angle = Mathf.Min(angle, rotationLimit);

                Quaternion swing = Quaternion.AngleAxis(angle, axis);
                joint.rotation = swing * joint.rotation;
            }

            if ((tip.position - targetPos).sqrMagnitude < tolerance * tolerance)
                break;
        }

        // Lock base joints' local X axis
        for (int i = 0; i < numBaseJointsToLockX && i < joints.Length; i++)
        {
            Vector3 euler = joints[i].localEulerAngles;
            euler.x = _originalXAngles[i];
            joints[i].localEulerAngles = euler;
        }

        // Smoothly revert Y and Z axis to bind pose
        for (int i = numBaseJointsToLockX; i < joints.Length; i++)
        {
            if (_isControllerMoving)
            {
                // Only revert to bind pose if the controller has stopped moving
                _yLerpTimers[i] = 0f;
                _zLerpTimers[i] = 0f;
            }
            else
            {
                // Smooth transition of Y and Z angles back to the bind pose
                float tY = Mathf.Clamp01(_yLerpTimers[i] / smoothResetDuration);
                float smoothedY = Mathf.LerpAngle(joints[i].localEulerAngles.y, _originalYAngles[i], tY);

                float tZ = Mathf.Clamp01(_zLerpTimers[i] / smoothResetDuration);
                float smoothedZ = Mathf.LerpAngle(joints[i].localEulerAngles.z, _originalZAngles[i], tZ);

                Vector3 euler = joints[i].localEulerAngles;
                euler.y = Mathf.Clamp(smoothedY, minY, maxY);
                euler.z = Mathf.Clamp(smoothedZ, minZ, maxZ);

                joints[i].localEulerAngles = euler;

                // Update timers
                _yLerpTimers[i] += Time.deltaTime;
                _zLerpTimers[i] += Time.deltaTime;
            }
        }

        // Snap tip to controller position
        tip.position = controller.position;
    }
}
