using UnityEngine;

public class EnemyAudio : MonoBehaviour
{
    public AudioSource source;
    public AudioClip alertClip;

    public void PlayAlert()
    {
        if (source != null && alertClip != null)
        {
            source.PlayOneShot(alertClip);
        }
    }
}