using System.Collections.Generic;
using UnityEngine;

public class StuckShurikenManager : MonoBehaviour
{
    public static StuckShurikenManager Instance { get; private set; }

    [Tooltip("Total shuriken allowed to exist at once (spawned + held + thrown + stuck).")]
    public int maxActive = 5;

    [Tooltip("Max shuriken allowed stuck at once before the oldest dissolves.")]
    public int maxStuck = 5;

    readonly HashSet<ShurikenHitDetector> activeShuriken = new HashSet<ShurikenHitDetector>();
    readonly Queue<ShurikenHitDetector> stuckOrder = new Queue<ShurikenHitDetector>();

    public int ActiveCount => activeShuriken.Count;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void RegisterActive(ShurikenHitDetector shuriken)
    {
        activeShuriken.Add(shuriken);
    }

    public void UnregisterActive(ShurikenHitDetector shuriken)
    {
        activeShuriken.Remove(shuriken);
    }

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

    public void UnregisterStuck(ShurikenHitDetector shuriken)
    {
        if (!stuckOrder.Contains(shuriken)) return;

        Queue<ShurikenHitDetector> rebuilt = new Queue<ShurikenHitDetector>();
        foreach (var s in stuckOrder)
        {
            if (s != shuriken) rebuilt.Enqueue(s);
        }
        stuckOrder.Clear();
        foreach (var s in rebuilt) stuckOrder.Enqueue(s);
    }
}