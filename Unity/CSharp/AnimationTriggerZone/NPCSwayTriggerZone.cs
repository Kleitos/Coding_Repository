using System.Collections;
using UnityEngine;

public class NPCSwayTriggerZone : MonoBehaviour
{
    [SerializeField] private BaseProceduralAnimation swayAnimation;
    [SerializeField] private float minRepeatDelay = 2f;
    [SerializeField] private float maxRepeatDelay = 6f;

    private Coroutine _repeatRoutine;
    private bool _playerInside;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (!_playerInside)
            {
                _playerInside = true;
                swayAnimation.Play();
                _repeatRoutine = StartCoroutine(RandomPlaybackRoutine());
            }
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
        }
    }

    private IEnumerator RandomPlaybackRoutine()
    {
        while (_playerInside)
        {
            float delay = Random.Range(minRepeatDelay, maxRepeatDelay);
            yield return new WaitForSeconds(delay);

            // Only play again if still inside and not already playing
            if (_playerInside && !swayAnimation.IsPlaying)
            {
                swayAnimation.Play();
            }
        }
    }
}
