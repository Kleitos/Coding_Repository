using UnityEngine;
using UnityEngine.SceneManagement;

public class HoveringEffectManager : MonoBehaviour
{
    [Header("Hovering Effect Parameters")]
    [SerializeField] private HoveringEffectParameters parameters;

    [Header("Hovering Objects")]
    [SerializeField] private GameObject[] hoveringObjects;

    private AnimationManager animationManager;

    private void Awake()
    {
        animationManager = Object.FindFirstObjectByType<AnimationManager>();

        if (animationManager == null)
        {
            Debug.LogError("AnimationManager not found in the scene.");
            return;
        }

        // Register scene loaded callback
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        // Unregister scene loaded callback to avoid potential memory leaks
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RegisterHoveringEffects();
        TriggerAnimations();
    }

    private void RegisterHoveringEffects()
    {
        if (hoveringObjects.Length == 0)
        {
            HoveringEffect[] effects = FindObjectsByType<HoveringEffect>(FindObjectsSortMode.None);
            foreach (var effect in effects)
            {
                animationManager.RegisterWithRandomParams(effect, parameters);
            }
        }
        else
        {
            foreach (var obj in hoveringObjects)
            {
                HoveringEffect hoveringEffect = obj.GetComponent<HoveringEffect>();
                if (hoveringEffect != null)
                {
                    animationManager.RegisterWithRandomParams(hoveringEffect, parameters);
                }
                else
                {
                    Debug.LogWarning($"No HoveringEffect found on {obj.name}");
                }
            }
        }
    }

    private void TriggerAnimations()
    {
        foreach (var animation in animationManager.RegisteredAnimations)
        {
            if (animation is HoveringEffect)
            {
                animation.Play();
            }
        }
    }
}
