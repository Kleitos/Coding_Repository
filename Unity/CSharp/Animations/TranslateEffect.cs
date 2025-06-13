using System.Collections;
using UnityEngine;

public class TranslateEffect : BaseProceduralAnimation
{
    [SerializeField] private Transform transformToAffect;

    [Header("Movement Settings")]
    [SerializeField] private float baseAmplitude = 0.5f;
    [SerializeField, Range(0.1f, 2f)] private float animationDuration = 0.5f;

    [Header("Axis Control")]
    [SerializeField] private Vector3 axisMask = Vector3.one; // Use 1 or 0 to enable/disable movement per axis
    [SerializeField] private bool allowNegativeMovement = true;

    [Header("Movement Mode")]
    [SerializeField] private bool useNoise = false;
    [SerializeField] private bool randomizeDirection = true;

    [Header("Loop Settings")]
    [SerializeField] private bool looping = true;
    [SerializeField] private float loopingDelay = 0.2f;

    [Header("Centering Settings")]
    [SerializeField] private bool constrainToCenter = true;
    [SerializeField] private float maxDriftDistance = 0.8f;
    [SerializeField, Range(0f, 2f)] private float softCenterPullStrength = 0.5f;
    
    [Header("Resting Position Settings")]
    [SerializeField] private Transform worldTargetRestingPosition; // Target in world space
    private Vector3 _restingPosition;


    private Coroutine _floatCoroutine;
    private WaitForSeconds _loopingDelayWaitForSeconds;

    private Vector3 _currentOffset = Vector3.zero;
    private Vector3 _currentDirection = Vector3.zero;

    private bool _registered = false;

    protected override void Awake()
    {
        if (transformToAffect == null)
            transformToAffect = transform;

        _loopingDelayWaitForSeconds = new WaitForSeconds(loopingDelay);
        if (worldTargetRestingPosition != null)
        {
            _restingPosition = worldTargetRestingPosition.position;
        }
        else
        {
            _restingPosition = transformToAffect.position;
        }


        if (!_registered)
        {
            Object.FindFirstObjectByType<AnimationManager>()?.Register(this);
            _registered = true;
        }
    }

    protected override void Start()
    {

        if (playOnStart)
            Play();
    }
    public override void Stop()
    {
        if (_animationCoroutine != null)
        {
            StopCoroutine(_animationCoroutine);
            _animationCoroutine = null;
        }

        looping = false;

        StartCoroutine(SmoothReturnToStart());
    }


    [ContextMenu("Start Floating")]
    

    private Vector3 GetRandomDirection()
    {
        Vector3 dir = Random.onUnitSphere;
        if (!allowNegativeMovement)
            dir = new Vector3(Mathf.Abs(dir.x), Mathf.Abs(dir.y), Mathf.Abs(dir.z));
        return Vector3.Scale(dir, axisMask).normalized;
    }

    private Vector3 GetNoiseDirection()
    {
        float noiseX = Mathf.PerlinNoise(Time.time * 0.5f, 0f) - 0.5f;
        float noiseY = Mathf.PerlinNoise(0f, Time.time * 0.5f) - 0.5f;
        float noiseZ = Mathf.PerlinNoise(Time.time * 0.5f, Time.time * 0.5f) - 0.5f;
        Vector3 noise = new Vector3(noiseX, noiseY, noiseZ);
        return Vector3.Scale(noise, axisMask).normalized;
    }

    protected override IEnumerator Animate()
    {
        do
        {
            float elapsedTime = 0f;

            // Determine direction
            if (useNoise)
                _currentDirection = GetNoiseDirection();
            else if (randomizeDirection)
                _currentDirection = GetRandomDirection();

            // Compute new target
            Vector3 targetOffset = _currentOffset + _currentDirection * baseAmplitude;

            // Constrain to center logic
            if (constrainToCenter)
            {
                Vector3 predictedPosition = _restingPosition + targetOffset;
                Vector3 toCenter = _restingPosition - predictedPosition;
                float distance = toCenter.magnitude;

                if (distance > maxDriftDistance)
                {
                    Vector3 corrected = toCenter.normalized * (distance - maxDriftDistance);
                    targetOffset += corrected * softCenterPullStrength;
                }
            }

            Vector3 initialOffset = _currentOffset;

            while (elapsedTime < animationDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsedTime / animationDuration);

                _currentOffset = Vector3.Lerp(initialOffset, targetOffset, t);
                transformToAffect.position = _restingPosition + _currentOffset;

                yield return null;
            }

            _currentOffset = targetOffset;

            if (looping)
                yield return _loopingDelayWaitForSeconds;

        } while (looping);
    }
    private IEnumerator SmoothReturnToStart()
    {
        Vector3 initialPosition = transformToAffect.position;
        float elapsedTime = 0f;

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsedTime / animationDuration);

            transformToAffect.position = Vector3.Lerp(initialPosition, _restingPosition, t);
            yield return null;
        }

        transformToAffect.position = _restingPosition; // Ensure it's exactly at the resting position at the end
    }



    public void SetLooping(bool shouldLoop)
    {
        looping = shouldLoop;
    }
}
