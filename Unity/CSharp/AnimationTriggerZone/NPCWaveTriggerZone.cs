using UnityEngine;

public class NPCWaveTriggerZone : MonoBehaviour
{
    [SerializeField] private BaseProceduralAnimation waveAnimation;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            waveAnimation.Play();
        }
    }
}
