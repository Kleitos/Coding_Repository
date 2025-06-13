using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProceduralAnimationTriggerZone : MonoBehaviour
{
    [Header("Animation References")]
    [SerializeField] private List<BaseProceduralAnimation> directAnimations;
    [SerializeField] private List<string> animationIDs;

    [Header("Timing Settings")]
    [SerializeField] private float minRepeatDelay = 2f;
    [SerializeField] private float maxRepeatDelay = 6f;

    [Header("Behavior Flags")]
    [SerializeField] private bool playOnEnter = true;
    [SerializeField] private bool repeatWhileInside = true;
    [SerializeField] private bool stopOnExit = true;

    private AnimationManager _animationManager;
    private Coroutine _repeatRoutine;
    private bool _playerInside;

    private void Awake()
    {
        _animationManager = FindFirstObjectByType<AnimationManager>();
        if (_animationManager == null)
        {
            Debug.LogError("No AnimationManager found in the scene.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !_playerInside)
        {
            _playerInside = true;

            if (playOnEnter)
                PlayAllAnimations();

            if (repeatWhileInside)
                _repeatRoutine = StartCoroutine(RandomPlaybackRoutine());
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _playerInside = false;

            if (_repeatRoutine != null)
            {
                StopCoroutine(_repeatRoutine);
                _repeatRoutine = null;
            }

            if (stopOnExit)
                StopAllAnimations();
        }
    }

    private IEnumerator RandomPlaybackRoutine()
    {
        while (_playerInside)
        {
            float delay = Random.Range(minRepeatDelay, maxRepeatDelay);
            yield return new WaitForSeconds(delay);

            if (!_playerInside) yield break;

            PlayAllAnimations();
        }
    }

    private void PlayAllAnimations()
    {
        foreach (var anim in directAnimations)
        {
            if (anim != null && !anim.IsPlaying)
                anim.Play();
        }

        foreach (var id in animationIDs)
        {
            foreach (var anim in _animationManager.RegisteredAnimations)
            {
                if (anim.AnimationID == id && !anim.IsPlaying)
                    anim.Play();
            }
        }
    }

    private void StopAllAnimations()
    {
        foreach (var anim in directAnimations)
        {
            if (anim != null && anim.IsPlaying)
                anim.Stop();
        }

        foreach (var id in animationIDs)
        {
            foreach (var anim in _animationManager.RegisteredAnimations)
            {
                if (anim.AnimationID == id && anim.IsPlaying)
                    anim.Stop();
            }
        }
    }
}
