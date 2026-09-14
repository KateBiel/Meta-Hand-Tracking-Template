using UnityEngine;

/// <summary>
/// Plays a block/clang sound from the kunai itself when a projectile
/// (anything with ShurikenHitDetector) touches it. Independent of whatever
/// the shuriken's own Audio Source is doing - the sound plays from the
/// kunai's position, which is more correct since that's where the player
/// is actually holding it.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class KunaiBlockAudio : MonoBehaviour
{
    public AudioClip blockSound;

    AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void OnCollisionEnter(Collision collision)
    {
        TryPlayBlockSound(collision.collider);
    }

    void OnTriggerEnter(Collider other)
    {
        TryPlayBlockSound(other);
    }

    void TryPlayBlockSound(Collider hitCollider)
    {
        if (hitCollider.GetComponentInParent<ShurikenHitDetector>() == null) return;

        if (audioSource != null && blockSound != null)
        {
            audioSource.PlayOneShot(blockSound);
        }
    }
}