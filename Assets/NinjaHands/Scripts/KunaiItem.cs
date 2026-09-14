using UnityEngine;

/// <summary>
/// Attach to the kunai prefab. Tags it so incoming shuriken can detect and
/// block against it, and lets it deal a defeat-hit to targets like Target2
/// when swung/touched against them.
/// </summary>
public class KunaiItem : MonoBehaviour
{
    public float damage = 999f; // one-hit kill by default - Target2Health only needs >0

    void OnTriggerEnter(Collider other)
    {
        IHittable hittable = other.GetComponentInParent<IHittable>();
        if (hittable == null) return;

        HitInfo info = new HitInfo
        {
            point = transform.position,
            damage = damage,
            source = HitSource.Kunai,
            sourceName = "Kunai"
        };
        hittable.OnHit(info);
    }
}