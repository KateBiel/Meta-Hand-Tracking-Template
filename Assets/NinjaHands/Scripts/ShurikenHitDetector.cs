using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ShurikenHitDetector : MonoBehaviour
{
    [Header("Damage")]
    public float damage = 25f;

    [Header("Sticking")]
    public float embedDepth = 0.02f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip defaultStickSound;

    [Header("Dissolve")]
    [Tooltip("How long the shrink-away dissolve takes once this shuriken is bumped out by the limit.")]
    public float dissolveDuration = 0.5f;

    [Header("Debug")]
    public bool logHits = false;

    bool hasHitThisThrow = false;
    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasHitThisThrow) return;
        hasHitThisThrow = true;

        ContactPoint contact = collision.contactCount > 0 ? collision.GetContact(0) : default;
        Vector3 hitPoint = collision.contactCount > 0 ? contact.point : transform.position;
        Vector3 hitNormal = collision.contactCount > 0 ? contact.normal : -transform.forward;

        IHittable hittable = collision.collider.GetComponentInParent<IHittable>();
        if (hittable != null)
        {
            HitInfo info = new HitInfo
            {
                point = hitPoint,
                damage = damage,
                source = HitSource.Shuriken,
                sourceName = "Shuriken"
            };
            hittable.OnHit(info);
        }

        StickToSurface(collision.transform, hitPoint, hitNormal);
        PlayStickSound(collision.collider);

        if (StuckShurikenManager.Instance != null)
        {
            StuckShurikenManager.Instance.RegisterStuck(this);
        }

        if (logHits)
        {
            Debug.Log($"Shuriken stuck to {collision.collider.name}" +
                      (hittable != null ? $" for {damage} damage." : " (no damage)."));
        }
    }

    void StickToSurface(Transform surface, Vector3 hitPoint, Vector3 hitNormal)
    {
        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        transform.position = hitPoint - hitNormal * embedDepth;
        transform.rotation = Quaternion.LookRotation(-hitNormal);
        transform.SetParent(surface, worldPositionStays: true);
    }

    void PlayStickSound(Collider hitCollider)
    {
        if (audioSource == null) return;

        SurfaceAudioTag surfaceTag = hitCollider.GetComponentInParent<SurfaceAudioTag>();
        AudioClip clip = (surfaceTag != null && surfaceTag.stickSound != null)
            ? surfaceTag.stickSound
            : defaultStickSound;

        if (clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    /// <summary>
    /// Called by StuckShurikenManager when this shuriken ages out past the
    /// global stuck limit. Shrinks it away, then deactivates.
    /// TODO: swap the shrink-to-zero for a real dissolve shader effect if/when
    /// one is added - this is a placeholder that works with any material.
    /// </summary>
    public void Dissolve()
    {
        StartCoroutine(DissolveRoutine());
    }

    IEnumerator DissolveRoutine()
    {
        Vector3 startScale = transform.localScale;
        float t = 0f;

        while (t < dissolveDuration)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t / dissolveDuration);
            yield return null;
        }

        gameObject.SetActive(false);
        // Or Destroy(gameObject) if this isn't pooled.
    }

    public void ResetForNewThrow()
    {
        hasHitThisThrow = false;
        rb.isKinematic = false;
        transform.localScale = Vector3.one;
        transform.SetParent(null);

        if (StuckShurikenManager.Instance != null)
        {
            StuckShurikenManager.Instance.UnregisterStuck(this);
        }
    }
}