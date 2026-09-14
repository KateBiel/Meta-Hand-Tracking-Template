using System.Collections;
using UnityEngine;

public class Target2ShooterController : MonoBehaviour
{
    public GameObject shurikenProjectilePrefab;
    public Transform firePoint;
    public Transform playerTarget; // assign OVRCameraRig's head/CenterEyeAnchor

    public int shotCount = 3;
    public float shotInterval = 2f;
    public float projectileSpeed = 8f;

    void OnEnable()
    {
        Debug.Log("[Target2Shooter] OnEnable - starting shoot routine");
        StartCoroutine(ShootRoutine());
    }

    IEnumerator ShootRoutine()
    {
        for (int i = 0; i < shotCount; i++)
        {
            yield return new WaitForSeconds(shotInterval);
            Debug.Log($"[Target2Shooter] Firing shot {i + 1}/{shotCount}");
            FireAtPlayer();
        }
    }

    void FireAtPlayer()
    {
        if (shurikenProjectilePrefab == null || firePoint == null || playerTarget == null)
        {
            Debug.LogWarning("[Target2Shooter] Missing a reference");
            return;
        }

        GameObject proj = Instantiate(shurikenProjectilePrefab, firePoint.position, firePoint.rotation);
        Rigidbody rb = proj.GetComponent<Rigidbody>();
        if (rb == null) return;

        // Prevent immediate self-collision with Target2's own colliders.
        Collider projCollider = proj.GetComponent<Collider>();
        Collider[] shooterColliders = GetComponentsInChildren<Collider>();
        if (projCollider != null)
        {
            foreach (var col in shooterColliders)
            {
                Physics.IgnoreCollision(projCollider, col);
            }
        }

        Vector3 dir = (playerTarget.position - firePoint.position).normalized;
        proj.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        rb.linearVelocity = dir * projectileSpeed;

        ShurikenSpin spin = proj.GetComponent<ShurikenSpin>();
        if (spin == null)
        {
            spin = proj.AddComponent<ShurikenSpin>();
        }

        Debug.Log($"[Target2Shooter] Fired straight at player, velocity: {rb.linearVelocity}");
    }
}