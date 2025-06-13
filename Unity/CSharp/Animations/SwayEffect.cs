using System.Collections;
using UnityEngine;

public class SwayEffect : BaseProceduralAnimation
{
    [Header("Target Transform")]
    [SerializeField] private Transform transformToAffect;

    [Header("Sway Effect Settings")]
    [SerializeField] private float swayAmount = 15f;

    [SerializeField, Range(0f, 1f)] private float baseAnimationDuration = 0.25f;

    [Header("Animation Settings")]
    [SerializeField]
    private AnimationCurve swayCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.25f, 1f),
        new Keyframe(1f, 0f)
    );

    [Header("Axis Randomization Chances (%)")]
    [SerializeField, Range(0, 100)] private float swayChanceX = 100f;
    [SerializeField, Range(0, 100)] private float swayChanceY = 0f;
    [SerializeField, Range(0, 100)] private float swayChanceZ = 100f;

    [Header("Randomization Settings")]
    [SerializeField, Range(0f, 1f)] private float swayAmplitudeRandomness = 0.25f;
    [SerializeField, Range(0f, 0.5f)] private float animationDurationRandomness = 0.1f;
    [SerializeField, Range(0f, 1f)] private float swaySmoothSpeedRandomness = 0.3f;

    private Vector3 _currentSway;
    private Vector3 _currentTarget;
    private Vector3 _currentSmoothSpeeds;
    private float _elapsedTime = 0f;
    private float _currentDuration;

    private Coroutine _routine;

    protected override void Awake()
    {
        if (transformToAffect == null)
        {
            transformToAffect = transform;
            transformToAffect.rotation = Quaternion.identity;
        }


        Object.FindFirstObjectByType<AnimationManager>()?.Register(this);
    }

    public void Pause()
    {
        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }
    }

    public override void Stop()
    {
        if (_animationCoroutine != null)
        {
            StopCoroutine(_animationCoroutine);
            _animationCoroutine = null;
        }
        _elapsedTime = 0f;
        _currentSway = Vector3.zero;
        _currentTarget = Vector3.zero;
        transformToAffect.rotation = Quaternion.identity; // Reset rotation
    }

    public override void Reset()
    {
        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }

        transformToAffect.rotation = Quaternion.identity;
        _elapsedTime = 0f;
        InitializeSwayEffect(); // Reinitialize for next sway
        _routine = StartCoroutine(Animate());
    }


    [ContextMenu("Play Sway Effect")]
    public void StartSwayEffect()
    {
        InitializeSwayEffect();
        _routine = StartCoroutine(Animate());
    }

    private void InitializeSwayEffect()
    {
        transformToAffect.rotation = Quaternion.identity;

        Vector3 axisMask = PickRandomAxes();

        _currentTarget = new Vector3(
            axisMask.x != 0 ? swayAmount * RandomAmplitude() * RandomDirection() : 0f,
            axisMask.y != 0 ? swayAmount * RandomAmplitude() * RandomDirection() : 0f,
            axisMask.z != 0 ? swayAmount * RandomAmplitude() * RandomDirection() : 0f
        );

        _currentSmoothSpeeds = new Vector3(
            5f * (1f + Random.Range(-swaySmoothSpeedRandomness, swaySmoothSpeedRandomness)),
            5f * (1f + Random.Range(-swaySmoothSpeedRandomness, swaySmoothSpeedRandomness)),
            5f * (1f + Random.Range(-swaySmoothSpeedRandomness, swaySmoothSpeedRandomness))
        );

        _currentDuration = baseAnimationDuration * (1f + Random.Range(-animationDurationRandomness, animationDurationRandomness));
        _elapsedTime = 0f;
    }

    protected override IEnumerator Animate()
    {
        InitializeSwayEffect();

        while (_elapsedTime < _currentDuration)
        {
            _elapsedTime += Time.deltaTime;

            float curvePosition = Mathf.Clamp01(_elapsedTime / _currentDuration);
            float curveValue = swayCurve.Evaluate(curvePosition);

            _currentSway.x = Mathf.Lerp(_currentSway.x, _currentTarget.x, Time.deltaTime * _currentSmoothSpeeds.x);
            _currentSway.y = Mathf.Lerp(_currentSway.y, _currentTarget.y, Time.deltaTime * _currentSmoothSpeeds.y);
            _currentSway.z = Mathf.Lerp(_currentSway.z, _currentTarget.z, Time.deltaTime * _currentSmoothSpeeds.z);

            Quaternion modifiedRotation = Quaternion.Euler(_currentSway * curveValue);
            transformToAffect.rotation = Quaternion.Lerp(transformToAffect.rotation, modifiedRotation, curveValue);

            yield return null;
        }

        transformToAffect.rotation = Quaternion.identity;
        _animationCoroutine = null;
    }

    private Vector3 PickRandomAxes()
    {
        bool x = Random.Range(0f, 100f) < swayChanceX;
        bool y = Random.Range(0f, 100f) < swayChanceY;
        bool z = Random.Range(0f, 100f) < swayChanceZ;

        if (!x && !y && !z)
        {
            int forced = Random.Range(0, 3);
            if (forced == 0) x = true;
            else if (forced == 1) y = true;
            else z = true;
        }

        return new Vector3(x ? 1f : 0f, y ? 1f : 0f, z ? 1f : 0f);
    }

    private float RandomAmplitude()
    {
        return 1f + Random.Range(-swayAmplitudeRandomness, swayAmplitudeRandomness);
    }

    private int RandomDirection()
    {
        return Random.value < 0.5f ? 1 : -1;
    }
}
