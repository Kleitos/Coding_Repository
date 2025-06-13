using UnityEngine;

public class NPCTentacleFloatTriggerZone : MonoBehaviour
{
    [SerializeField] private BaseProceduralAnimation floatAnimation;

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player") && !floatAnimation.IsPlaying)
        {
            floatAnimation.Play();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            floatAnimation.Stop();   // Stop the animation coroutine
            floatAnimation.Reset();  // Optionally reset to default state (e.g., neutral pose)
        }
    }
}
