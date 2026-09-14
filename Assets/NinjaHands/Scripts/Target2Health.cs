using System.Collections;
using UnityEngine;

/// <summary>
/// Defeated by a single hit from Jutsu or Kunai. Regular shuriken hits are
/// ignored entirely (per design: shuriken must be reflected with kunai first,
/// then jutsu/kunai finishes it).
/// </summary>
public class Target2Health : MonoBehaviour, IHittable
{
    public Collider hitCollider;
    public AudioSource audioSource;
    public AudioClip defeatSound;
    public bool isDefeated = false;

    public void OnHit(HitInfo info)
    {
        if (isDefeated) return;
        if (info.source != HitSource.Jutsu && info.source != HitSource.Kunai) return;

        Defeat();
    }

    void Defeat()
    {
        isDefeated = true;

        if (hitCollider != null) hitCollider.enabled = false;
        if (audioSource != null && defeatSound != null) audioSource.PlayOneShot(defeatSound);

        StartCoroutine(DefeatSequence());
    }

    IEnumerator DefeatSequence()
    {
        float wait = (defeatSound != null) ? defeatSound.length : 1f;
        yield return new WaitForSeconds(wait);
        gameObject.SetActive(false);
    }

    public void ResetHealth()
    {
        isDefeated = false;
        if (hitCollider != null) hitCollider.enabled = true;
        gameObject.SetActive(true);
    }
}