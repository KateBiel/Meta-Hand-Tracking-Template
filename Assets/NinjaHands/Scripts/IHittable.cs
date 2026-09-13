using UnityEngine;

/// <summary>
/// Shared hit interface for anything that can take damage - targets, the player, etc.
/// Shuriken and jutsu will both call into this once the shared hit pipeline is wired up.
/// </summary>
public struct HitInfo
{
    public Vector3 point;
    public float damage;
    public HitSource source;
    public string sourceName; // optional - jutsu name, shuriken variant, etc.
}

public enum HitSource
{
    Shuriken,
    Jutsu,
    Other
}

public interface IHittable
{
    void OnHit(HitInfo info);
}