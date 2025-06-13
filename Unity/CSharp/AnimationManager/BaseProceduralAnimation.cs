using System.Collections;
using UnityEngine;

public abstract class BaseProceduralAnimation : MonoBehaviour, IProceduralAnimation
{
    [Header("Animation Manager Settings")]
    [SerializeField] private string animationIDOverride;

    public string AnimationID => string.IsNullOrEmpty(animationIDOverride) ? gameObject.name : animationIDOverride;

    protected Coroutine _animationCoroutine;
    protected bool _isPlaying => _animationCoroutine != null;
    public bool IsPlaying => _isPlaying;

    [SerializeField] protected bool playOnStart = false;

    protected virtual void Awake()
    {
        Object.FindFirstObjectByType<AnimationManager>()?.Register(this);
    }

    protected virtual void Start()
    {
        if (playOnStart)
            Play();
    }

    public virtual void Play()
    {
        if (_animationCoroutine != null)
            StopCoroutine(_animationCoroutine);

        _animationCoroutine = StartCoroutine(Animate());
    }

    public virtual void Stop()
    {
        if (_animationCoroutine != null)
        {
            StopCoroutine(_animationCoroutine);
            _animationCoroutine = null;
        }
    }

    /// <summary>
    /// Optional: Override this in derived classes if a reset-to-neutral state is required.
    /// </summary>
    public virtual void Reset()
    {
        Stop(); // Stop running coroutine by default
    }

    protected abstract IEnumerator Animate();
}
