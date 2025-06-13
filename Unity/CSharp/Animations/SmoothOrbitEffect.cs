using System.Collections;
using UnityEngine;

public class SmoothOrbitEffect : BaseProceduralAnimation
{
    [Header("Target Transform")]
    [SerializeField] private Transform transformToAffect;

    [Header("Float Effect Settings")]
    [SerializeField] private float baseAmplitude = 0.5f;

    [Header("Space Settings")]
    [SerializeField] private bool allowZMovement = false;
    [SerializeField, Range(0f, 1f)] private float zMovementStrength = 0.3f;

    [Header("Orbit Settings")]
    [SerializeField] private float baseOrbitSpeed = 30f;
    [SerializeField] private float orbitSpeedDriftRange = 5f;
    [SerializeField] private float orbitDriftSpeed = 0.1f;

    [Header("Centering Settings")]
    [SerializeField] private bool constrainToCenter = true;
    [SerializeField] private float maxDriftDistance = 0.8f;
    [SerializeField, Range(0f, 2f)] private float softCenterPullStrength = 0.5f;

    [Header("Start Options")]
    //[SerializeField] private bool playOnStart = false;

    private Vector3 _orbitAxis;
    private Vector3 _targetOrbitAxis;
    private float _currentOrbitSpeed;
    private float _targetOrbitSpeed;
    private Vector3 _currentOffset = Vector3.zero;
    private Vector3 _startPosition;

    private Coroutine _routine;

    protected override void Awake()
    {
        if (transformToAffect == null)
            transformToAffect = transform;

        _startPosition = transformToAffect.position;

        _orbitAxis = GetRandomAxis();
        _targetOrbitAxis = GetRandomAxis();
        _currentOrbitSpeed = baseOrbitSpeed;
        _targetOrbitSpeed = baseOrbitSpeed + Random.Range(-orbitSpeedDriftRange, orbitSpeedDriftRange);

        // Register this effect with the AnimationManager
        Object.FindFirstObjectByType<AnimationManager>()?.Register(this);
    }

    protected override IEnumerator Animate()
    {
        while (true)
        {
            OrbitStep(Time.deltaTime);
            yield return null;
        }
    }

    private void OrbitStep(float deltaTime)
    {
        _orbitAxis = Vector3.Slerp(_orbitAxis, _targetOrbitAxis, orbitDriftSpeed * deltaTime).normalized;
        _currentOrbitSpeed = Mathf.Lerp(_currentOrbitSpeed, _targetOrbitSpeed, orbitDriftSpeed * deltaTime);

        if (Vector3.Angle(_orbitAxis, _targetOrbitAxis) < 5f)
            _targetOrbitAxis = GetRandomAxis();

        if (Mathf.Abs(_currentOrbitSpeed - _targetOrbitSpeed) < 0.1f)
            _targetOrbitSpeed = baseOrbitSpeed + Random.Range(-orbitSpeedDriftRange, orbitSpeedDriftRange);

        Quaternion rotation = Quaternion.AngleAxis(_currentOrbitSpeed * deltaTime, _orbitAxis);
        _currentOffset = rotation * (_currentOffset == Vector3.zero ? Vector3.right * baseAmplitude : _currentOffset);

        Vector3 predictedPos = _startPosition + _currentOffset;
        if (constrainToCenter)
        {
            Vector3 toCenter = _startPosition - predictedPos;
            float dist = toCenter.magnitude;
            if (dist > maxDriftDistance)
            {
                Vector3 correction = toCenter.normalized * (dist - maxDriftDistance);
                _currentOffset += correction * softCenterPullStrength;
            }
        }

        transformToAffect.position = _startPosition + _currentOffset;
    }

    public override void Stop()
    {
        if (_animationCoroutine != null)
        {
            StopCoroutine(_animationCoroutine);
            _animationCoroutine = null;
        }
            transformToAffect.position = _startPosition;
    }

    private Vector3 GetRandomAxis()
    {
        Vector3 axis = Random.onUnitSphere;
        if (!allowZMovement)
            axis.z = 0f;
        else
            axis.z *= zMovementStrength;
        return axis.normalized;
    }
}
