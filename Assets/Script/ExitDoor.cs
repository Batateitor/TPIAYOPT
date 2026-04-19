using UnityEngine;

public class ExitDoor : MonoBehaviour
{
    public FadeController fadeController;
    public AudioSource audioSource;
    public AudioClip victoryClip;

    private bool activated = false;

    private void OnTriggerEnter(Collider other)
    {
        if (activated) return;

        if (other.CompareTag("Player"))
        {
            activated = true;

            if (audioSource != null && victoryClip != null)
            {
                audioSource.PlayOneShot(victoryClip);
            }

            if (fadeController != null)
            {
                fadeController.FadeAndReload();
            }
        }
    }
}