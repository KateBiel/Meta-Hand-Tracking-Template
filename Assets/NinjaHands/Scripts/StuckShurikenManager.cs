using System.Collections.Generic;
using UnityEngine;

public class StuckShurikenManager : MonoBehaviour
{
    public static StuckShurikenManager Instance { get; private set; }

    [Tooltip("Total shuriken allowed to exist at once (spawned + held + thrown + stuck).")]
    public int maxActive = 5;

    [Tooltip("Max shuriken allowed stuck at once before the oldest dissolves.")]
    public int maxStuck = 5;

    [Header("Debug")]
    public bool logRegistration = true;

    readonly HashSet<ShurikenHitDetector> activeShuriken = new HashSet<ShurikenHitDetector>();
    readonly Queue<ShurikenHitDetector> stuckOrder = new Queue<ShurikenHitDetector>();

    public int ActiveCount => activeShuriken.Count;
    public int StuckCount => stuckOrder.Count;

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
        if (logRegistration) Debug.Log($"[StuckShurikenManager] Active registered. Active count: {activeShuriken.Count}");
    }

    public void UnregisterActive(ShurikenHitDetector shuriken)
    {
        activeShuriken.Remove(shuriken);
        if (logRegistration) Debug.Log($"[StuckShurikenManager] Active unregistered. Active count: {activeShuriken.Count}");
    }

    public void RegisterStuck(ShurikenHitDetector shuriken)
    {
        stuckOrder.Enqueue(shuriken);
        if (logRegistration) Debug.Log($"[StuckShurikenManager] Stuck registered. Stuck count: {stuckOrder.Count}/{maxStuck}");

        if (stuckOrder.Count > maxStuck)
        {
            ShurikenHitDetector oldest = stuckOrder.Dequeue();
            if (oldest != null)
            {
                if (logRegistration) Debug.Log($"[StuckShurikenManager] Over limit - dissolving oldest: {oldest.name}");
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

    /// <summary>
    /// Destroys every ShurikenHitDetector currently in the scene, active or
    /// inactive - used when switching levels so nothing carries over, including
    /// shurikens that went inactive as a side effect of their parent target
    /// being deactivated (which silently drops them from the tracked set).
    /// </summary>
    public void ClearAll()
    {
        var allShuriken = Object.FindObjectsByType<ShurikenHitDetector>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        if (logRegistration) Debug.Log($"[StuckShurikenManager] ClearAll - destroying {allShuriken.Length} shuriken (scene-wide sweep)");

        foreach (var s in allShuriken)
        {
            if (s != null)
            {
                Destroy(s.gameObject);
            }
        }

        activeShuriken.Clear();
        stuckOrder.Clear();
    }

}