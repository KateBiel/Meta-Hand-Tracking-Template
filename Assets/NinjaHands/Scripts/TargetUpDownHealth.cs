using UnityEngine;

public class TargetUpDownHealth : MonoBehaviour, IHittable
{
    [Header("Health")]
    public float maxHealth = 100f;
    public float current;

    [Header("Hit Zone")]
    [Tooltip("Reference point for measuring hit distance. Leave empty to use this object's own position.")]
    public Transform center;
    [Tooltip("Full radius of the target's hittable area.")]
    public float targetRadius = 0.15f;
    [Tooltip("Fraction of targetRadius considered the 'center' zone (0-1).")]
    [Range(0f, 1f)] public float innerZoneFraction = 0.3f;
    [Tooltip("Damage dealt (as a fraction of maxHealth) when hit inside the inner zone.")]
    [Range(0f, 1f)] public float centerHitFraction = 0.6f;
    [Tooltip("Damage dealt (as a fraction of maxHealth) when hit outside the inner zone.")]
    [Range(0f, 1f)] public float outerHitFraction = 0.2f;

    [Header("References")]
    public ThreeColorHealthBar healthBar;
    public Collider hitCollider;
    [Tooltip("The target's UI Canvas (health bar) — hidden/shown alongside the mesh, since Canvas elements aren't Renderers and won't be caught automatically.")]
    public GameObject uiRoot;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip hitSound;
    public AudioClip jutsuHitSound;
    public AudioClip defeatSound;

    [Header("Defeat VFX")]
    public GameObject defeatSmokeVFXPrefab;
    public AudioClip defeatPoofSound;

    [Header("Despawn")]
    [Tooltip("Minimum seconds before this object is destroyed. The actual wait is whichever is longer: this value, or the length of whichever defeat sound(s) are playing — so audio never gets cut off.")]
    public float despawnDelay = 0.3f;

    [Header("Events")]
    public bool isDefeated = false;
    private bool _lastIsDefeated = false;

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

    void OnValidate()
    {
        if (!Application.isPlaying) return;

        if (isDefeated && !_lastIsDefeated)
        {
            current = 0f;
            Defeat();
        }
        _lastIsDefeated = isDefeated;

        if (healthBar != null)
        {
            healthBar.SetHealth(current, maxHealth);
        }

        if (current <= 0f && !isDefeated)
        {
            Defeat();
        }
    }

    public void OnHit(HitInfo info)
    {
        if (isDefeated) return;

        float damage = ComputeDamage(info.point);
        current = Mathf.Max(0f, current - damage);

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

    /// <summary>
    /// Distance-based damage: a hit inside the inner zone (close to center) deals
    /// centerHitFraction of maxHealth; a hit outside it deals outerHitFraction.
    /// </summary>
    private float ComputeDamage(Vector3 hitPoint)
    {
        Transform reference = center != null ? center : transform;
        float distance = Vector3.Distance(hitPoint, reference.position);
        float innerRadius = targetRadius * innerZoneFraction;

        float fraction = (distance <= innerRadius) ? centerHitFraction : outerHitFraction;
        return maxHealth * fraction;
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

        StartCoroutine(DefeatSequence());
    }

    private System.Collections.IEnumerator DefeatSequence()
    {
        // Hide immediately for anything already parented...
        SetVisible(false);

        // ...then wait one frame and sweep again, to catch a shuriken that finishes
        // parenting onto us via StickToSurface() AFTER OnHit() returns, in this
        // same collision event — same frame, just later in the call stack.
        yield return null;
        SetVisible(false);

        SpawnDefeatVFX();

        // Wait long enough for whichever defeat sound is actually playing to
        // finish, so Destroy() doesn't cut it off mid-clip. Everything is
        // already invisible from SetVisible(false) above, so this extra wait
        // has zero visual effect — it only protects the audio.
        float soundLength = 0f;
        if (defeatSound != null) soundLength = Mathf.Max(soundLength, defeatSound.length);
        if (defeatPoofSound != null) soundLength = Mathf.Max(soundLength, defeatPoofSound.length);

        float wait = Mathf.Max(despawnDelay, soundLength);
        yield return new WaitForSeconds(wait);
        Destroy(gameObject);
    }

    private void SetVisible(bool visible)
    {
        foreach (var r in GetComponentsInChildren<Renderer>(true))
        {
            r.enabled = visible;
        }

        if (uiRoot != null)
        {
            uiRoot.SetActive(visible);
        }
    }

    private void SpawnDefeatVFX()
    {
        if (defeatSmokeVFXPrefab != null)
        {
            GameObject vfx = Instantiate(defeatSmokeVFXPrefab, transform.position, transform.rotation);
            ParticleSystem ps = vfx.GetComponent<ParticleSystem>();
            if (ps != null) ps.Play(true);
        }

        if (defeatPoofSound != null)
        {
            if (audioSource != null) audioSource.PlayOneShot(defeatPoofSound);
            else AudioSource.PlayClipAtPoint(defeatPoofSound, transform.position);
        }
    }

    public void ResetHealth()
    {
        current = maxHealth;
        isDefeated = false;
        if (hitCollider != null) hitCollider.enabled = true;
        if (healthBar != null) healthBar.SetHealth(current, maxHealth);
        SetVisible(true);
    }

    [ContextMenu("Force Defeat (Test)")]
    public void ForceDefeat()
    {
        if (isDefeated) return;
        current = 0f;
        Defeat();
    }
}