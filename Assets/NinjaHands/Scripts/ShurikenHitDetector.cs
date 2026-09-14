using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ShurikenHitDetector : MonoBehaviour
{
    [Header("Damage")]
    public float damage = 25f;

    [Header("Sticking")]
    public float embedDepth = 0.02f;

    [Header("Ground")]
    [Tooltip("Tag used to identify the floor/ground. On contact, the shuriken freezes completely in place - no embedding, no parenting.")]
    public string groundTag = "Ground";

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip defaultStickSound;

    [Header("Dissolve")]
    public float dissolveDuration = 0.5f;

    [Header("Debug")]
    public bool logHits = false;

    bool hasHitThisThrow = false;
    bool isDeflectedAndFalling = false;
    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void OnEnable()
    {
        StuckShurikenManager.Instance?.RegisterActive(this);
    }

    void OnDisable()
    {
        StuckShurikenManager.Instance?.UnregisterActive(this);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasHitThisThrow)
        {
            // Already deflected and falling (post-kunai-block) - freeze it in
            // place on the next thing it touches, unchanged behavior.
            if (isDeflectedAndFalling)
            {
                FreezeInPlace();
            }
            return;
        }

        // Blocked by kunai - knock away and let it fall. Unchanged.
        if (collision.collider.GetComponentInParent<KunaiItem>() != null)
        {
            hasHitThisThrow = true;
            DeflectAndFall(collision);
            return;
        }

        // Hits the ground - stop dead instantly, no embedding, no parenting,
        // no IHittable/damage logic (ground isn't a valid damage target anyway).
        if (collision.collider.CompareTag(groundTag))
        {
            hasHitThisThrow = true;
            PlayStickSound(collision.collider);
            FreezeInPlace();

            if (logHits)
            {
                Debug.Log($"Shuriken hit ground ({collision.collider.name}) - froze in place.");
            }
            return;
        }

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

    void DeflectAndFall(Collision collision)
    {
        rb.isKinematic = false;
        rb.useGravity = true;
        isDeflectedAndFalling = true;

        Vector3 knockback = -collision.relativeVelocity.normalized * 1.5f;
        rb.linearVelocity = knockback;

        Destroy(gameObject, 4f);
    }

    void FreezeInPlace()
    {
        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        isDeflectedAndFalling = false;

        ShurikenSpin spin = GetComponent<ShurikenSpin>();
        if (spin != null)
        {
            spin.enabled = false;
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

    public void Dissolve()
    {
        if (!gameObject.activeInHierarchy)
        {
            gameObject.SetActive(false);
            return;
        }

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
    }

    public void ResetForNewThrow()
    {
        hasHitThisThrow = false;
        isDeflectedAndFalling = false;
        rb.isKinematic = false;
        transform.localScale = Vector3.one;
        transform.SetParent(null);

        if (StuckShurikenManager.Instance != null)
        {
            StuckShurikenManager.Instance.UnregisterStuck(this);
        }
    }
}