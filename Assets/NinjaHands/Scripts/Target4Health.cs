using System.Collections;
using UnityEngine;

public class Target4Health : MonoBehaviour, IHittable
{
    [Header("Health")]
    public float maxHealth = 100f;
    public float current;

    [Header("References")]
    public TwoTierHealthBar healthBar;
    public Collider hitCollider;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip hitSound;       // shuriken hit sound (default)
    public AudioClip jutsuHitSound;  // jutsu hit sound (sizzle/burn, etc)
    public AudioClip defeatSound;

    [Header("Approach Behavior")]
    [Tooltip("Fraction of max health at which the target starts approaching the player.")]
    [Range(0.01f, 0.99f)]
    public float approachHealthFraction = 0.5f;
    public Transform player;
    public float moveSpeed = 1.5f;

    [Header("Hit-Stun")]
    [Tooltip("Seconds of no new hits required before movement resumes. Continuous damage (e.g. Wind Sphere ticks) keeps refreshing this, so the target stays frozen the whole time it's being hit.")]
    public float resumeDelayAfterHit = 5f;

    [Header("State")]
    public bool isDefeated = false;

    private bool _hasStartedApproaching;
    private bool _isStunned;
    private float _lastHitTime = -999f;

    [Header("Stop Distance")]
    [Tooltip("Target stops when within this distance of the player.")]
    public float stopDistance = 0.2f;
    [Tooltip("Seconds the player must remain outside stopDistance before the target resumes closing in.")]
    public float reengageDelay = 0.5f;

    [Header("Rotation")]
    public float turnSpeed = 5f;
    [Tooltip("Extra yaw offset to correct for the model's forward axis not matching Unity's +Z.")]
    public float rotationOffset = 180f;

    private bool _atStopDistance = false;
    private float _reengageTimer = 0f;

    private float _fixedX;
    private float _fixedZ;
    private bool _rotationCaptured = false;

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

    void Update()
    {
        if (isDefeated) return;

        if (_isStunned && Time.time - _lastHitTime >= resumeDelayAfterHit)
        {
            _isStunned = false;
        }

        if (_hasStartedApproaching && !_isStunned && player != null)
        {
            Vector3 targetPos = new Vector3(player.position.x, transform.position.y, player.position.z);
            float distance = Vector3.Distance(transform.position, targetPos);

            if (distance <= stopDistance)
            {
                _atStopDistance = true;
                _reengageTimer = 0f;
            }
            else if (_atStopDistance)
            {
                _reengageTimer += Time.deltaTime;
                if (_reengageTimer >= reengageDelay)
                {
                    _atStopDistance = false;
                }
            }

            if (!_atStopDistance)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    targetPos,
                    moveSpeed * Time.deltaTime
                );
            }
        }

        if (_hasStartedApproaching && player != null)
        {
            if (!_rotationCaptured)
            {
                _fixedX = transform.eulerAngles.x;
                _fixedZ = transform.eulerAngles.z;
                _rotationCaptured = true;
            }

            Vector3 lookDir = new Vector3(player.position.x, transform.position.y, player.position.z) - transform.position;
            if (lookDir.sqrMagnitude > 0.0001f)
            {
                float targetYaw = Mathf.Atan2(lookDir.x, lookDir.z) * Mathf.Rad2Deg + rotationOffset;
                float currentYaw = transform.eulerAngles.y;
                float newYaw = Mathf.LerpAngle(currentYaw, targetYaw, turnSpeed * Time.deltaTime);

                transform.rotation = Quaternion.Euler(_fixedX, newYaw, _fixedZ);
            }
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

        // Refresh stun on every hit — single hits pause for resumeDelayAfterHit,
        // continuous hits (Wind Sphere ticking) keep pushing the resume time forward.
        _lastHitTime = Time.time;
        _isStunned = true;

        if (!_hasStartedApproaching && current <= maxHealth * approachHealthFraction)
        {
            _hasStartedApproaching = true;
        }

        if (current <= 0f)
        {
            Defeat();
        }
    }

    void Defeat()
    {
        isDefeated = true;
        _hasStartedApproaching = false;

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

    IEnumerator DefeatSequence()
    {
        float wait = (defeatSound != null) ? defeatSound.length : 1f;
        yield return new WaitForSeconds(wait);

        gameObject.SetActive(false);
    }

    public void ResetHealth()
    {
        current = maxHealth;
        isDefeated = false;
        _hasStartedApproaching = false;
        _isStunned = false;
        _rotationCaptured = false;
        if (hitCollider != null) hitCollider.enabled = true;
        if (healthBar != null) healthBar.SetHealth(current, maxHealth);
        gameObject.SetActive(true);
    }
}