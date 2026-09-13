using UnityEngine;

/// <summary>
/// Attach to any surface (floor, wall, a target) to define what sound a
/// shuriken should play when it sticks to it. If a surface has no tag,
/// ShurikenHitDetector falls back to its own default stick sound.
/// </summary>
public class SurfaceAudioTag : MonoBehaviour
{
    public AudioClip stickSound;
}