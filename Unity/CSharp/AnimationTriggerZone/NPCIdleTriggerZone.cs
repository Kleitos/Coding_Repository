using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HybridNPCIdleTriggerZone : MonoBehaviour
{
    [Header("Behavior Flags")]
    [SerializeField] private TriggerAnimationBehavior behavior = TriggerAnimationBehavior.PlayOnEnter | TriggerAnimationBehavior.RepeatWhileInside | TriggerAnimationBehavior.StopOnExit;

    [Header("Animation References")]
    [SerializeField] private List<BaseProceduralAnimation> constantAnimations = new();
    [SerializeField] private List<BaseProceduralAnimation> randomAnimations = new();
    [SerializeField] private List<string> constantAnimationIDs = new();
    [SerializeField] private List<string> randomAnimationIDs = new();

    [Header("Repeat Delay")]
    [SerializeField] private float minRepeatDelay = 2f;
    [SerializeField] private float maxRepeatDelay = 6f;

    private bool _playerInside = false;
    private Coroutine _repeatRoutine;
    private AnimationManager _animationManager;

    private void Awake()
    {
        _animationManager = Object.FindFirstObjectByType<AnimationManager>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || _playerInside)
            return;

        _playerInside = true;

        if (behavior.HasFlag(TriggerAnimationBehavior.PlayOnEnter))
        {
            PlayConstantAnimations();
            PlayRandomAnimation();
        }

        if (behavior.HasFlag(TriggerAnimationBehavior.RepeatWhileInside))
        {
            _repeatRoutine = StartCoroutine(RepeatRandomRoutine());
        }

        if (behavior.HasFlag(TriggerAnimationBehavior.RepeatWhileInside))
        {
            EnableLooping(constantAnimations);
            EnableLoopingFromIDs(constantAnimationIDs);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        _playerInside = false;

        if (_repeatRoutine != null)
        {
            StopCoroutine(_repeatRoutine);
            _repeatRoutine = null;
        }

        if (behavior.HasFlag(TriggerAnimationBehavior.StopOnExit))
        {
            StopAllFromList(constantAnimations);
            StopAllFromIDs(constantAnimationIDs);
            StopAllFromList(randomAnimations);
            StopAllFromIDs(randomAnimationIDs);
        }

        DisableLooping(constantAnimations);
        DisableLoopingFromIDs(constantAnimationIDs);
    }

    private void PlayConstantAnimations()
    {
        foreach (var anim in constantAnimations)
            anim?.Play();

        foreach (var id in constantAnimationIDs)
            _animationManager?.Play(id);
    }

    private void PlayRandomAnimation()
    {
        List<IProceduralAnimation> allRandoms = new();

        if (_animationManager != null)
        {
            foreach (string id in randomAnimationIDs)
            {
                foreach (var anim in _animationManager.RegisteredAnimations)
                {
                    if (anim.AnimationID == id)
                        allRandoms.Add(anim);
                }
            }
        }

        foreach (var anim in randomAnimations)
            if (anim != null)
                allRandoms.Add(anim);

        if (allRandoms.Count > 0)
        {
            int index = Random.Range(0, allRandoms.Count);
            allRandoms[index].Play();
        }
    }

    private IEnumerator RepeatRandomRoutine()
    {
        while (_playerInside)
        {
            float delay = Random.Range(minRepeatDelay, maxRepeatDelay);
            yield return new WaitForSeconds(delay);

            if (_playerInside)
                PlayRandomAnimation();
        }
    }

    private void StopAllFromList(List<BaseProceduralAnimation> anims)
    {
        foreach (var anim in anims)
            anim?.Stop();
    }

    private void StopAllFromIDs(List<string> ids)
    {
        if (_animationManager == null) return;

        foreach (var id in ids)
            _animationManager.Stop(id);
    }

    private void EnableLooping(List<BaseProceduralAnimation> anims)
    {
        foreach (var anim in anims)
        {
            if (anim is TranslateEffect translate)
                translate.SetLooping(true);
        }
    }

    private void EnableLoopingFromIDs(List<string> ids)
    {
        if (_animationManager == null) return;

        foreach (var id in ids)
        {
            foreach (var anim in _animationManager.RegisteredAnimations)
            {
                if (anim.AnimationID == id && anim is TranslateEffect translate)
                    translate.SetLooping(true);
            }
        }
    }

    private void DisableLooping(List<BaseProceduralAnimation> anims)
    {
        foreach (var anim in anims)
        {
            if (anim is TranslateEffect translate)
                translate.SetLooping(false);
        }
    }

    private void DisableLoopingFromIDs(List<string> ids)
    {
        if (_animationManager == null) return;

        foreach (var id in ids)
        {
            foreach (var anim in _animationManager.RegisteredAnimations)
            {
                if (anim.AnimationID == id && anim is TranslateEffect translate)
                    translate.SetLooping(false);
            }
        }
    }
}
