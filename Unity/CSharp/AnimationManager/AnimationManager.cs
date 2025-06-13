using System.Collections.Generic;
using UnityEngine;

public class AnimationManager : MonoBehaviour
{
    private Dictionary<string, List<IProceduralAnimation>> _animationsByID = new();
    public IEnumerable<IProceduralAnimation> GetAnimationsByID(string id)
    {
        if (_animationsByID.TryGetValue(id, out var list))
            return list;
        return new List<IProceduralAnimation>();
    }

    public void Register(IProceduralAnimation animation)
    {
        string id = animation.AnimationID;

        if (!_animationsByID.ContainsKey(id))
            _animationsByID[id] = new List<IProceduralAnimation>();

        if (!_animationsByID[id].Contains(animation))
            _animationsByID[id].Add(animation);
    }

    public void RegisterWithRandomParams(IProceduralAnimation animation, HoveringEffectParameters parameters)
    {
        Register(animation);

        if (animation is HoveringEffect hoveringEffect)
        {
            hoveringEffect.ApplyRandomParameters(parameters);
        }
    }


    public void Play(string id)
    {
        if (_animationsByID.TryGetValue(id, out var animList))
        {
            foreach (var anim in animList)
                anim.Play();
        }
        else
        {
            Debug.LogWarning($"No animation registered with ID: {id}");
        }
    }

    public void Stop(string id)
    {
        if (_animationsByID.TryGetValue(id, out var animList))
        {
            foreach (var anim in animList)
                anim.Stop();
        }
    }
    public void PlayAll()
    {
        foreach (var animList in _animationsByID.Values)
        {
            foreach (var anim in animList)
                anim.Play();
        }
    }


    public void StopAll()
    {
        foreach (var animList in _animationsByID.Values)
        {
            foreach (var anim in animList)
                anim.Stop();
        }
    }
    public IEnumerable<IProceduralAnimation> RegisteredAnimations
    {
        get
        {
            foreach (var list in _animationsByID.Values)
            {
                foreach (var anim in list)
                    yield return anim;
            }
        }
    }

}
