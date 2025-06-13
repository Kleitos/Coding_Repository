using System.Collections;
using UnityEngine;

public class HoveringEffect : BaseProceduralAnimation
{
    [Header("Target Transform")]
    [SerializeField] private Transform transformToAffect;

    [Header("Vertical Floating")]
    [SerializeField] private bool enableFloating = true;
    [SerializeField] private float verticalAmplitude = 0.5f;
    [SerializeField] private float verticalSpeed = 1f;

    [Header("Horizontal Drift")]
    [SerializeField] private bool enableDrift = true;
    [SerializeField] private bool usePerlinDrift = true;
    [SerializeField] private float driftAmplitude = 0.1f;
    [SerializeField] private float driftSpeed = 0.5f;

    [Header("Wobble Rotation")]
    [SerializeField] private bool enableWobble = true;
    [SerializeField] private float wobbleAngle = 5f;
    [SerializeField] private float wobbleSpeed = 1f;

    [Header("Amplitude Modulation")]
    [SerializeField] private bool enableAmplitudeModulation = false;
    [SerializeField] private float modulationSpeed = 0.1f;
    [SerializeField] private float modulationStrength = 0.5f; // 0–1

    [Header("Attraction Target")]
    [SerializeField] private bool enableAttraction = false;
    [SerializeField] private Transform attractionTarget;
    [SerializeField] private float attractionStrength = 0.5f;

    [Header("Pulse Scale Effect")]
    [SerializeField] private bool enablePulse = false;
    [SerializeField] private float pulseScale = 0.05f;
    [SerializeField] private float pulseSpeed = 1f;

    private Vector3 _startPosition;
    private Quaternion _startRotation;
    private Vector3 _startScale;
    private float _timeOffset;
    private Coroutine _routine;


    protected override void Awake()
    {
        if (transformToAffect == null)
            transformToAffect = transform;

        _startPosition = transformToAffect.position;
        _startRotation = transformToAffect.rotation;
        _startScale = transformToAffect.localScale;
        _timeOffset = Random.Range(0f, 100f);

        Object.FindFirstObjectByType<AnimationManager>()?.Register(this);
    }

    public void ApplyRandomParameters(HoveringEffectParameters parameters)
    {
        verticalAmplitude = Random.Range(parameters.verticalAmplitudeMin, parameters.verticalAmplitudeMax);
        verticalSpeed = Random.Range(parameters.verticalSpeedMin, parameters.verticalSpeedMax);

        driftAmplitude = Random.Range(parameters.driftAmplitudeMin, parameters.driftAmplitudeMax);
        driftSpeed = Random.Range(parameters.driftSpeedMin, parameters.driftSpeedMax);

        wobbleAngle = Random.Range(parameters.wobbleAngleMin, parameters.wobbleAngleMax);
        wobbleSpeed = Random.Range(parameters.wobbleSpeedMin, parameters.wobbleSpeedMax);

        pulseScale = Random.Range(parameters.pulseScaleMin, parameters.pulseScaleMax);
        pulseSpeed = Random.Range(parameters.pulseSpeedMin, parameters.pulseSpeedMax);
    }


    public override void Stop()
    {
        if (_animationCoroutine != null)
        {
            StopCoroutine(_animationCoroutine);
            _animationCoroutine = null;
        }
        transformToAffect.position = _startPosition;
                transformToAffect.rotation = _startRotation;
                transformToAffect.localScale = _startScale;
    }

    protected override IEnumerator Animate()
    {
        while (true)
        {
            float time = Time.time + _timeOffset;

            // Vertical Floating
            float y = 0f;
            if (enableFloating)
            {
                float mod = 1f;
                if (enableAmplitudeModulation)
                    mod = Mathf.PerlinNoise(0f, time * modulationSpeed) * modulationStrength + (1f - modulationStrength);
                y = Mathf.Sin(time * verticalSpeed) * verticalAmplitude * mod;
            }

            // Horizontal Drift
            float x = 0f, z = 0f;
            if (enableDrift)
            {
                if (usePerlinDrift)
                {
                    x = (Mathf.PerlinNoise(time * driftSpeed, 0f) * 2f - 1f) * driftAmplitude;
                    z = (Mathf.PerlinNoise(0f, time * driftSpeed) * 2f - 1f) * driftAmplitude;
                }
                else
                {
                    x = Mathf.Sin(time * driftSpeed * 1.2f) * driftAmplitude;
                    z = Mathf.Cos(time * driftSpeed * 1.5f) * driftAmplitude;
                }
            }

            Vector3 targetPosition = _startPosition + new Vector3(x, y, z);

            // Attraction
            if (enableAttraction && attractionTarget != null)
            {
                targetPosition = Vector3.Lerp(targetPosition, attractionTarget.position, Time.deltaTime * attractionStrength);
            }

            transformToAffect.position = targetPosition;

            // Wobble Rotation
            if (enableWobble)
            {
                float angleX = Mathf.Sin(time * wobbleSpeed) * wobbleAngle;
                float angleZ = Mathf.Cos(time * wobbleSpeed) * wobbleAngle;
                transformToAffect.rotation = _startRotation * Quaternion.Euler(angleX, 0f, angleZ);
            }

            // Pulse Scale
            if (enablePulse)
            {
                float scale = 1f + Mathf.Sin(time * pulseSpeed) * pulseScale;
                transformToAffect.localScale = _startScale * scale;
            }

            yield return null;
        }
    }
}
