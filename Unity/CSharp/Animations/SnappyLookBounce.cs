using UnityEngine;
using System.Collections;

public class SnappyLookBounce : MonoBehaviour
{
    private AnimationManager animationManager;

    private Transform player;

    [Header("Bounce Rotation Parameters")]
    [Tooltip("Value of each Rotation.")]
    public float stepSize = 15f;

    [Tooltip("Maximum limit in both negative and positive value.")]
    public float maxYaw = 90f;

    [Tooltip("Height of Bounce.")]
    public float bounceHeight = 0.2f;

    [Tooltip("Duration of Bounce.")]
    public float bounceDuration = 0.2f;

    [Tooltip("No bouncing or turning if within this angle range.")]
    public float deadZoneAngle = 10f;

    [HideInInspector] public bool isActive = false;

    private float currentYaw = 0f;
    private Quaternion initialRotation;
    private Vector3 basePosition;
    private bool isBouncing = false;
    private bool isWakingUp = false;
    public bool IsLookingAtPlayer => isActive && !isWakingUp && Mathf.Abs(currentYaw - GetTargetYaw()) >= deadZoneAngle;

    private float GetTargetYaw()
    {
        Vector3 toPlayer = (player.position - transform.position).normalized;
        Vector3 flatToPlayer = new Vector3(toPlayer.x, 0, toPlayer.z).normalized;
        Vector3 flatForward = initialRotation * Vector3.forward;

        float targetYaw = Vector3.SignedAngle(flatForward, flatToPlayer, Vector3.up);
        return Mathf.Clamp(targetYaw, -maxYaw, maxYaw);
    }

    void Start()
    {
        basePosition = transform.position;
        initialRotation = transform.rotation;
        animationManager = Object.FindFirstObjectByType<AnimationManager>();
        player = GameMANAGER.Instance.ControlerScript.m_mainBody.transform;
    }

    void Update()
    {
        if (!isActive || isWakingUp || isBouncing)
            return;

        float targetYaw = GetTargetYaw();
        float angleDelta = targetYaw - currentYaw;

        if (Mathf.Abs(angleDelta) < deadZoneAngle)
        {
            return; // No idle animation handling here
        }

        int steps = Mathf.FloorToInt(Mathf.Abs(angleDelta) / stepSize);
        if (steps > 0)
        {
            float totalStep = stepSize * steps * Mathf.Sign(angleDelta);
            currentYaw += totalStep;
            currentYaw = Mathf.Clamp(currentYaw, -maxYaw, maxYaw);

            Quaternion stepRotation = Quaternion.AngleAxis(currentYaw, Vector3.up) * initialRotation;
            transform.rotation = stepRotation;

            StartCoroutine(Bounce());
        }
    }

    public void WakeUpAndActivate()
    {
        StartCoroutine(WakeUpBounce());
    }

    private IEnumerator WakeUpBounce()
    {
        isWakingUp = true;
        float time = 0f;

        while (time < bounceDuration)
        {
            float t = time / bounceDuration;
            float height = Mathf.Sin(t * Mathf.PI) * bounceHeight;
            transform.position = basePosition + Vector3.up * height;
            time += Time.deltaTime;
            yield return null;
        }

        transform.position = basePosition;
        isWakingUp = false;
        isActive = true; // Activate after bounce finishes
    }

    IEnumerator Bounce()
    {
        isBouncing = true;

        float time = 0f;

        while (time < bounceDuration)
        {
            float t = time / bounceDuration;
            float height = Mathf.Sin(t * Mathf.PI) * bounceHeight;
            transform.position = basePosition + Vector3.up * height;
            time += Time.deltaTime;
            yield return null;
        }

        transform.position = basePosition;

        isBouncing = false;
    }
}
