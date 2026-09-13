using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks every currently-stuck shuriken across the whole scene. Once more
/// than maxStuck are stuck at once, the oldest one dissolves automatically.
/// Add this to a single persistent object (e.g. a "Managers" GameObject).
/// </summary>
public class StuckShurikenManager : MonoBehaviour
{
    public static StuckShurikenManager Instance { get; private set; }

    [Tooltip("Maximum shuriken allowed stuck at once, globally, before the oldest dissolves.")]
    public int maxStuck = 5;

    readonly Queue<ShurikenHitDetector> stuckOrder = new Queue<ShurikenHitDetector>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Call when a shuriken sticks to any surface. Dissolves the oldest one
    /// if this push puts the count over the limit.
    /// </summary>
    public void RegisterStuck(ShurikenHitDetector shuriken)
    {
        stuckOrder.Enqueue(shuriken);

        if (stuckOrder.Count > maxStuck)
        {
            ShurikenHitDetector oldest = stuckOrder.Dequeue();
            if (oldest != null)
            {
                oldest.Dissolve();
            }
        }
    }

    /// <summary>
    /// Call if a stuck shuriken is removed some other way (e.g. manually reset,
    /// pooled/respawned) so it doesn't linger as a stale reference in the queue.
    /// </summary>
    public void UnregisterStuck(ShurikenHitDetector shuriken)
    {
        if (!stuckOrder.Contains(shuriken)) return;

        // Queue<T> has no direct remove - rebuild without the target entry.
        Queue<ShurikenHitDetector> rebuilt = new Queue<ShurikenHitDetector>();
        foreach (var s in stuckOrder)
        {
            if (s != shuriken) rebuilt.Enqueue(s);
        }
        stuckOrder.Clear();
        foreach (var s in rebuilt) stuckOrder.Enqueue(s);
    }
}