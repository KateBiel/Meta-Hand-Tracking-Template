using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One jutsu's hand-fired projectile: which prefab to spawn, from where, and after how long.
/// The prefab is expected to already carry its own ProjectileMoveScript (speed, accuracy,
/// muzzle/hit VFX, trails, rigidbody/collider) — this spawner just instantiates it facing
/// the fire point's forward direction and lets it fly on its own.
/// </summary>
[Serializable]
public class JutsuProjectileEntry
{
    public string jutsuName;

    [Tooltip("The projectile prefab, e.g. vfx_Projectile_ElectricPurpleBall. Must already have ProjectileMoveScript + Rigidbody + Collider set up on it.")]
    public GameObject projectilePrefab;

    [Tooltip("Hand-anchored transform to spawn from. Its forward (blue) axis is the fire direction — point it away from the palm.")]
    public Transform firePoint;

    [Tooltip("Optional delay after the jutsu completes before the first projectile actually spawns, e.g. to sync with a cast animation/sound.")]
    public float spawnDelay = 0f;

    [Header("Repeat Fire (optional)")]
    [Tooltip("If true, keeps spawning projectiles every Fire Interval seconds for Burst Duration seconds, instead of firing just once.")]
    public bool repeatFire = false;

    [Tooltip("Seconds between each shot while repeat-firing.")]
    public float fireInterval = 0.3f;

    [Tooltip("Total seconds to keep firing, starting after Spawn Delay.")]
    public float burstDuration = 10f;

    [Header("Cleanup Safety Net")]
    [Tooltip("Every spawned projectile is force-destroyed after this many seconds, even if it never hits anything (misses fly forever otherwise since ProjectileMoveScript only destroys itself on collision). Set generously above the projectile's expected flight time.")]
    public float maxLifetime = 5f;
}

public class JutsuProjectileSpawner : MonoBehaviour
{
    [SerializeField] private JutsuManager jutsuManager;
    [SerializeField] private List<JutsuProjectileEntry> projectileEntries = new List<JutsuProjectileEntry>();

    private Dictionary<string, JutsuProjectileEntry> _lookup;

    private void Awake()
    {
        _lookup = new Dictionary<string, JutsuProjectileEntry>();
        foreach (var entry in projectileEntries)
        {
            if (entry == null || string.IsNullOrEmpty(entry.jutsuName)) continue;
            _lookup[entry.jutsuName] = entry;
        }
    }

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

    private void HandleJutsuCompleted(string jutsuName)
    {
        if (_lookup == null || !_lookup.TryGetValue(jutsuName, out JutsuProjectileEntry entry)) return;
        if (entry.projectilePrefab == null || entry.firePoint == null) return;

        StartCoroutine(FireRoutine(entry));
    }

    private IEnumerator FireRoutine(JutsuProjectileEntry entry)
    {
        if (entry.spawnDelay > 0f)
            yield return new WaitForSeconds(entry.spawnDelay);

        if (!entry.repeatFire)
        {
            Spawn(entry);
            yield break;
        }

        float elapsed = 0f;
        float interval = Mathf.Max(entry.fireInterval, 0.01f);
        while (elapsed < entry.burstDuration)
        {
            Spawn(entry);
            yield return new WaitForSeconds(interval);
            elapsed += interval;
        }
    }

    private void Spawn(JutsuProjectileEntry entry)
    {
        // Capture position/rotation at spawn time in case the hand has moved during the delay.
        GameObject instance = Instantiate(entry.projectilePrefab, entry.firePoint.position, entry.firePoint.rotation);

        // Safety net: guarantees cleanup even if it never collides with anything (a miss would
        // otherwise fly forever, since ProjectileMoveScript only destroys itself on collision).
        if (entry.maxLifetime > 0f)
            Destroy(instance, entry.maxLifetime);
    }

    // ---- Debug/testing helpers ----

    [Header("Debug")]
    [Tooltip("Check this box in Play Mode to instantly fire the entry at Debug Entry Index. It un-checks itself right after — it's a button, not a persistent setting.")]
    [SerializeField] private bool debugFireToggle = false;
    [Tooltip("Which entry in Projectile Entries the Debug Fire Toggle checkbox fires.")]
    [SerializeField] private int debugEntryIndex = 0;

    private void OnValidate()
    {
        if (!debugFireToggle) return;
        debugFireToggle = false; // reset immediately so it behaves like a button, not a state

        if (!Application.isPlaying) return; // only actually fire while running
        if (projectileEntries == null || debugEntryIndex < 0 || debugEntryIndex >= projectileEntries.Count)
        {
            Debug.LogWarning("JutsuProjectileSpawner: Debug Entry Index is out of range.");
            return;
        }
        DebugFireEntry(projectileEntries[debugEntryIndex]);
    }

    [ContextMenu("Debug: Fire First Entry")]
    private void DebugFireFirstEntry()
    {
        if (projectileEntries == null || projectileEntries.Count == 0)
        {
            Debug.LogWarning("JutsuProjectileSpawner: no entries to fire.");
            return;
        }
        DebugFireEntry(projectileEntries[0]);
    }

    /// <summary>Call from a debug table button, or right-click the component header in Play Mode and pick "Debug: Fire First Entry".</summary>
    public void DebugFireByJutsuName(string jutsuName)
    {
        if (_lookup == null || !_lookup.TryGetValue(jutsuName, out JutsuProjectileEntry entry))
        {
            Debug.LogWarning($"JutsuProjectileSpawner: no entry named '{jutsuName}'.");
            return;
        }
        DebugFireEntry(entry);
    }

    private void DebugFireEntry(JutsuProjectileEntry entry)
    {
        if (entry.projectilePrefab == null || entry.firePoint == null)
        {
            Debug.LogWarning($"JutsuProjectileSpawner: entry '{entry.jutsuName}' is missing its Projectile Prefab or Fire Point.");
            return;
        }
        Spawn(entry);
    }
}