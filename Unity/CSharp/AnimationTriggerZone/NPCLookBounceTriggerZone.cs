using UnityEngine;
using System.Collections;

public class NPCLookBounceTriggerZone : MonoBehaviour
{
    public SnappyLookBounce targetCharacter;
    public float activationDelay = 1f;
    public float cooldownDuration = 2f;

    private Coroutine activationCoroutine;
    private float lastExitTime = -999f;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // Respect cooldown
        if (Time.time < lastExitTime + cooldownDuration)
        {
            Debug.Log("In cooldown, skipping activation");
            return;
        }

        if (activationCoroutine != null)
            StopCoroutine(activationCoroutine);

        activationCoroutine = StartCoroutine(DelayedActivate());
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        lastExitTime = Time.time;

        if (activationCoroutine != null)
        {
            StopCoroutine(activationCoroutine);
            activationCoroutine = null;
        }

        targetCharacter.isActive = false;
    }

    private IEnumerator DelayedActivate()
    {
        yield return new WaitForSeconds(activationDelay);
        targetCharacter.WakeUpAndActivate();
    }
}