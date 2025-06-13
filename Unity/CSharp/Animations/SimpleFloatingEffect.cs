using System.Collections;
using UnityEngine;

public class SimpleFloatingEffect : BaseProceduralAnimation
{
    [SerializeField] private Transform transformToAffect;

    [Header("Vertical Floating")]
    [SerializeField] private float verticalAmplitude = 0.5f;
    [SerializeField] private float verticalSpeed = 1f;

    [Header("Horizontal Drift")]
    [SerializeField] private float driftAmplitude = 0.1f;
    [SerializeField] private float driftSpeed = 0.5f;

    [Header("Amplitude Modulation")]
    [SerializeField] private float modulationSpeed = 0.1f;
    [SerializeField] private float modulationStrength = 0.5f;

    [Header("Anchor Pull")]
    [SerializeField] private bool enableGravitationalPull = true;
    [SerializeField] private float pullStrength = 1f; // Higher = snappier return to center

    private Vector3 _startPosition;
    private float _timeOffset;

    protected override void Awake()
    {
        if (transformToAffect == null)
            transformToAffect = transform;

        _startPosition = transformToAffect.position;
        _timeOffset = Random.Range(0f, 100f);

        Object.FindFirstObjectByType<AnimationManager>()?.Register(this);
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

    protected override IEnumerator Animate()
    {
        Vector3 currentOffset = Vector3.zero;

        while (true)
        {
            float time = Time.time + _timeOffset;

            // Amplitude modulation
            float mod = Mathf.PerlinNoise(0f, time * modulationSpeed) * modulationStrength + (1f - modulationStrength);

            // Vertical float
            float y = Mathf.Sin(time * verticalSpeed) * verticalAmplitude * mod;

            // Horizontal drift
            float x = (Mathf.PerlinNoise(time * driftSpeed, 0f) * 2f - 1f) * driftAmplitude;
            float z = (Mathf.PerlinNoise(0f, time * driftSpeed) * 2f - 1f) * driftAmplitude;

            Vector3 driftOffset = new Vector3(x, y, z);

            // Gravitational pull toward the original position
            if (enableGravitationalPull)
            {
                Vector3 targetOffset = Vector3.Lerp(currentOffset, driftOffset, Time.deltaTime * pullStrength);
                currentOffset = targetOffset;
            }
            else
            {
                currentOffset = driftOffset;
            }

            transformToAffect.position = _startPosition + currentOffset;

            yield return null;
        }
    }
}
