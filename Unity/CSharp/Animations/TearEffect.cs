using System.Collections;
using UnityEngine;

public class TearEffect : BaseProceduralAnimation
{
    [SerializeField] private Transform transformToAffect;

    [Header("Movement Settings")]
    [SerializeField] private float movementAmount = 1f;
    [SerializeField] private float speed = 1f;

    [Header("Loop Settings")]
    [SerializeField] private float loopingDelay = 0.2f;

    private float _startY;

    protected override void Awake()
    {
        base.Awake();  // Registers with the AnimationManager

        if (transformToAffect == null)
            transformToAffect = transform;

        _startY = transformToAffect.position.y;
    }

    public override void Play()
    {
        base.Play(); // This will call the base class Play method and start the coroutine
    }

    public override void Stop()
    {
        base.Stop();
        ResetPosition();
    }

    public override void Reset()
    {
        Stop();
        ResetPosition();
    }

    private void ResetPosition()
    {
        transformToAffect.position = new Vector3(transformToAffect.position.x, _startY, transformToAffect.position.z);
    }

    protected override IEnumerator Animate()
    {
        while (true)
        {
            float elapsedTime = 0f;
            float targetY = _startY - movementAmount;

            // Move down
            while (elapsedTime < 1f / speed)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsedTime * speed);
                float newY = Mathf.Lerp(_startY, targetY, t);
                transformToAffect.position = new Vector3(transformToAffect.position.x, newY, transformToAffect.position.z);
                yield return null;
            }

            elapsedTime = 0f;

            // Move back up
            while (elapsedTime < 1f / speed)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsedTime * speed);
                float newY = Mathf.Lerp(targetY, _startY, t);
                transformToAffect.position = new Vector3(transformToAffect.position.x, newY, transformToAffect.position.z);
                yield return null;
            }

            yield return new WaitForSeconds(loopingDelay);
        }
    }
}
