using UnityEngine;

/// <summary>
/// Spins a shuriken around its own axis while in flight - for
/// shooter-launched projectiles. Add manually or via script on spawn.
/// </summary>
public class ShurikenSpin : MonoBehaviour
{
    public float spinSpeed = 720f; // degrees per second
    public Vector3 spinAxis = Vector3.forward; // local axis to spin around

    void Update()
    {
        transform.Rotate(spinAxis, spinSpeed * Time.deltaTime, Space.Self);
    }
}