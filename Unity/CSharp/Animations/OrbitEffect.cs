using System.Collections;
using UnityEngine;

public class OrbitEffect : MonoBehaviour
{
    [Header("Float Effect Settings")]
    [SerializeField] private Transform transformToAffect;
    [SerializeField] private float baseAmplitude = 0.5f;
    [SerializeField, Range(0.1f, 2f)] private float animationDuration = 0.5f;

    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool looping = true;
    [SerializeField] private float loopingDelay = 0.2f;

    [Header("Space Settings")]
    [SerializeField] private bool allowZMovement = false;
    [SerializeField, Range(0f, 1f)] private float zMovementStrength = 0.3f;

    [Header("Orbit Settings")]
    [SerializeField] private float orbitSpeed = 30f; // degrees per second
    [SerializeField, Range(0f, 1f)] private float randomDriftStrength = 0.1f; // minor random drift

    [Header("Centering Settings")]
    [SerializeField] private bool constrainToCenter = true;
    [SerializeField] private float maxDriftDistance = 0.8f;
    [SerializeField, Range(0f, 2f)] private float softCenterPullStrength = 0.5f;

    private Coroutine _floatCoroutine;
    private WaitForSeconds _loopingDelayWaitForSeconds;

    private Vector3 _currentOffset = Vector3.zero;
    private Vector3 _orbitAxis = Vector3.up;
    private Vector3 _startPosition;

    private void Awake()
    {
        if (transformToAffect == null)
            transformToAffect = transform;

        _loopingDelayWaitForSeconds = new WaitForSeconds(loopingDelay);
    }

    private void Start()
    {
        _startPosition = transformToAffect.position;
        InitializeOrbitAxis();

        if (playOnStart)
            StartFloating();
    }

    [ContextMenu("Start Floating")]
    public void StartFloating()
    {
        if (_floatCoroutine != null)
            StopCoroutine(_floatCoroutine);

        _floatCoroutine = StartCoroutine(FloatCoroutine());
    }

    private void InitializeOrbitAxis()
    {
        _orbitAxis = Random.onUnitSphere;
        if (!allowZMovement)
            _orbitAxis.z = 0f;
        else
            _orbitAxis.z *= zMovementStrength;

        _orbitAxis.Normalize();
    }

    private IEnumerator FloatCoroutine()
    {
        do
        {
            float elapsedTime = 0f;

            // Prepare random slight drift
            Vector3 randomDrift = Random.insideUnitSphere * randomDriftStrength;
            if (!allowZMovement)
                randomDrift.z = 0f;
            else
                randomDrift.z *= zMovementStrength;

            randomDrift.Normalize();

            Vector3 rotationAxis = _orbitAxis + randomDrift;
            rotationAxis.Normalize();

            Vector3 initialOffset = _currentOffset;
            Vector3 rotatedOffset = Quaternion.AngleAxis(orbitSpeed * animationDuration, rotationAxis) * (initialOffset == Vector3.zero ? Vector3.right * baseAmplitude : initialOffset);

            // Optional center pull
            if (constrainToCenter)
            {
                Vector3 predictedPosition = _startPosition + rotatedOffset;
                Vector3 toCenter = _startPosition - predictedPosition;
                float distance = toCenter.magnitude;

                if (distance > maxDriftDistance)
                {
                    // Soft pull toward center
                    Vector3 correction = toCenter.normalized * (distance - maxDriftDistance);
                    rotatedOffset += correction * softCenterPullStrength;
                }
            }

            while (elapsedTime < animationDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsedTime / animationDuration);

                _currentOffset = Vector3.Lerp(initialOffset, rotatedOffset, t);
                transformToAffect.position = _startPosition + _currentOffset;

                yield return null;
            }

            _currentOffset = rotatedOffset;

            if (looping)
                yield return _loopingDelayWaitForSeconds;

        } while (looping);
    }

    public void SetLooping(bool shouldLoop)
    {
        looping = shouldLoop;
    }
}
