using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attach to the Wind Sphere orb (the VFX object on the player's hand).
/// While the jutsu is active, any IHittable overlapping the orb's trigger
/// collider takes damagePerTick every tickInterval, for as long as contact
/// continues — right up until the technique's duration ends.
/// </summary>
public class WindSphereDamage : MonoBehaviour
{
    [SerializeField] private JutsuManager jutsuManager;
    [SerializeField] private string jutsuName = "Wind Sphere";

    [Header("Damage")]
    public float damagePerTick = 70f;
    public float tickInterval = 1f;
    public float duration = 6f;

    private bool _active = false;
    private readonly Dictionary<IHittable, Coroutine> _activeTargets = new Dictionary<IHittable, Coroutine>();

    private void OnEnable()
    {
        if (jutsuManager != null)
            jutsuManager.OnJutsuCompleted.AddListener(HandleJutsuCompleted);
    }

    private void OnDisable()
    {
        if (jutsuManager != null)
            jutsuManager.OnJutsuCompleted.RemoveListener(HandleJutsuCompleted);
    }

    private void HandleJutsuCompleted(string completedJutsuName)
    {
        if (completedJutsuName != jutsuName) return;
        StartCoroutine(ActiveWindow());
    }

    private IEnumerator ActiveWindow()
    {
        _active = true;
        yield return new WaitForSeconds(duration);
        _active = false;

        // Stop any still-running per-target tick loops when the technique ends.
        foreach (var kvp in _activeTargets)
        {
            if (kvp.Value != null) StopCoroutine(kvp.Value);
        }
        _activeTargets.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_active) return;

        IHittable hittable = other.GetComponentInParent<IHittable>();
        if (hittable == null || _activeTargets.ContainsKey(hittable)) return;

        Coroutine routine = StartCoroutine(TickDamage(hittable));
        _activeTargets[hittable] = routine;
    }

    private void OnTriggerExit(Collider other)
    {
        IHittable hittable = other.GetComponentInParent<IHittable>();
        if (hittable == null) return;

        if (_activeTargets.TryGetValue(hittable, out Coroutine routine))
        {
            if (routine != null) StopCoroutine(routine);
            _activeTargets.Remove(hittable);
        }
    }

    private IEnumerator TickDamage(IHittable hittable)
    {
        while (_active)
        {
            HitInfo info = new HitInfo
            {
                point = transform.position,
                damage = damagePerTick,
                source = HitSource.Jutsu,
                sourceName = jutsuName
            };
            hittable.OnHit(info);

            yield return new WaitForSeconds(tickInterval);
        }

        if (_activeTargets.ContainsKey(hittable))
            _activeTargets.Remove(hittable);
    }
}