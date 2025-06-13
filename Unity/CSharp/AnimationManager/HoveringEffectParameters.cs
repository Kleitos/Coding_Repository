using UnityEngine;

[System.Serializable]
public struct HoveringEffectParameters
{
    [Header("Vertical Floating")]
    public float verticalAmplitudeMin;
    public float verticalAmplitudeMax;
    public float verticalSpeedMin;
    public float verticalSpeedMax;

    [Header("Horizontal Drift")]
    public float driftAmplitudeMin;
    public float driftAmplitudeMax;
    public float driftSpeedMin;
    public float driftSpeedMax;

    [Header("Wobble Rotation")]
    public float wobbleAngleMin;
    public float wobbleAngleMax;
    public float wobbleSpeedMin;
    public float wobbleSpeedMax;

    [Header("Pulse Scale Effect")]
    public float pulseScaleMin;
    public float pulseScaleMax;
    public float pulseSpeedMin;
    public float pulseSpeedMax;
}
