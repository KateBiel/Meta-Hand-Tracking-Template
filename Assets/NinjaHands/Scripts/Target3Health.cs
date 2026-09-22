using System.Collections;
using UnityEngine;

public class Target3Health : MonoBehaviour, IHittable
{
    [Header("Health")]
    public float maxHealth = 100f;
    public float current;

    [Header("Presence VFX (optional)")]
    public TargetPresenceVFX presenceVFX;

    [Header("References")]
    public TwoTierHealthBar healthBar;
    public Collider hitCollider;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip hitSound;       // shuriken hit sound (default)
    public AudioClip jutsuHitSound;  // jutsu hit sound (sizzle/burn, etc)
    public AudioClip defeatSound;

    [Header("Events")]
    public bool isDefeated = false;

    void Awake()
    {
        current = maxHealth;
    }

    void Start()
    {
        if (healthBar != null)
        {
            healthBar.SetHealth(current, maxHealth);
        }
    }

    public void OnHit(HitInfo info)
    {
        if (isDefeated) return;

        current = Mathf.Max(0f, current - info.damage);

        if (healthBar != null)
        {
            healthBar.SetHealth(current, maxHealth);
        }

        if (audioSource != null)
        {
            AudioClip clipToPlay = (info.source == HitSource.Jutsu && jutsuHitSound != null)
                ? jutsuHitSound
                : hitSound;

            if (clipToPlay != null)
            {
                audioSource.PlayOneShot(clipToPlay);
            }
        }

        if (current <= 0f)
        {
            Defeat();
        }
    }

    void Defeat()
    {
        isDefeated = true;

        if (hitCollider != null)
        {
            hitCollider.enabled = false;
        }

        if (audioSource != null && defeatSound != null)
        {
            audioSource.PlayOneShot(defeatSound);
        }

        if (presenceVFX != null)
        {
            presenceVFX.PlayDefeatSequence(() => gameObject.SetActive(false));
        }
        else
        {
            StartCoroutine(DefeatSequence());
        }
    }

    IEnumerator DefeatSequence()
    {
        float wait = (defeatSound != null) ? defeatSound.length : 1f;
        yield return new WaitForSeconds(wait);

        gameObject.SetActive(false);
    }

    public void ResetHealth()
    {
        gameObject.SetActive(true);
        current = maxHealth;
        isDefeated = false;
        if (hitCollider != null) hitCollider.enabled = true;
        if (healthBar != null) healthBar.SetHealth(current, maxHealth);
    }
}