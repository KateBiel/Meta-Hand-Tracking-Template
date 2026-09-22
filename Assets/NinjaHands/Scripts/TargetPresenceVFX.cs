using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Handles a target's delayed "poof-in" appearance and "poof-out" defeat, with
/// smoke VFX + sound at each transition. Attach alongside Target3Health/Target1Health
/// on the same GameObject.
///
/// Appear triggers automatically off OnEnable() - so it fires consistently whether
/// the target was just activated by LevelManager switching levels, OR by the
/// target's own ResetHealth() button. No changes needed to either of those.
/// </summary>
public class TargetPresenceVFX : MonoBehaviour
{
    [Header("References")]
    public Collider hitCollider;
    [Tooltip("Renderers to hide during the appear delay / defeat window. Leave empty to auto-find all child renderers.")]
    public Renderer[] renderers;
    [Tooltip("Optional. If left empty, sounds play via AudioSource.PlayClipAtPoint instead.")]
    public GameObject uiRoot;
    public AudioSource audioSource;



    [Header("Appear")]
    public GameObject appearSmokeVFXPrefab;
    public AudioClip appearSound;
    public float appearDelay = 2f;
    [Tooltip("Extra buffer after the smoke starts, before the target itself becomes visible — lets the smoke establish first.")]
    public float vfxLeadTime = 0.15f;

    [Header("Defeat")]
    [Tooltip("Target stays visible during this wait, THEN the smoke plays and it's removed.")]
    public float defeatSmokeDelay = 2f;
    public GameObject defeatSmokeVFXPrefab;
    public AudioClip defeatPoofSound;

    private Coroutine _appearRoutine;

    private void Awake()
    {
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<Renderer>(true);
    }

    private void OnEnable()
    {
        if (_appearRoutine != null) StopCoroutine(_appearRoutine);
        _appearRoutine = StartCoroutine(AppearRoutine());
    }

    private IEnumerator AppearRoutine()
    {
        SetVisible(false);
        yield return new WaitForSeconds(appearDelay);

        SpawnVFX(appearSmokeVFXPrefab);
        PlaySound(appearSound);

        yield return new WaitForSeconds(vfxLeadTime);
        SetVisible(true);

        _appearRoutine = null;
    }

    /// <summary>
    /// Call from Defeat() instead of deactivating the GameObject directly.
    /// Waits defeatSmokeDelay (target stays visible), then plays smoke + sound,
    /// waits for the sound to actually finish (so disabling the GameObject
    /// afterward doesn't cut it off), then invokes onComplete
    /// (typically gameObject.SetActive(false)).
    /// </summary>
    public void PlayDefeatSequence(Action onComplete)
    {
        StartCoroutine(DefeatRoutine(onComplete));
    }

    private IEnumerator DefeatRoutine(Action onComplete)
    {
        yield return new WaitForSeconds(defeatSmokeDelay);

        SpawnVFX(defeatSmokeVFXPrefab);
        PlaySound(defeatPoofSound);

        // Give the poof sound time to actually play before we (likely) disable
        // this GameObject via onComplete — disabling it stops any audio still
        // playing on a child AudioSource immediately.
        float soundWait = (defeatPoofSound != null) ? defeatPoofSound.length : 0f;
        if (soundWait > 0f)
            yield return new WaitForSeconds(soundWait);

        onComplete?.Invoke();
    }

    private void SetVisible(bool visible)
    {
        if (hitCollider != null) hitCollider.enabled = visible;
        if (uiRoot != null) uiRoot.SetActive(visible);
        if (renderers == null) return;
        foreach (var r in renderers)
        {
            if (r != null) r.enabled = visible;
        }
    }

    private void SpawnVFX(GameObject prefab)
    {
        if (prefab == null) return;
        GameObject vfx = Instantiate(prefab, transform.position, transform.rotation);

        ParticleSystem ps = vfx.GetComponent<ParticleSystem>();
        if (ps != null) ps.Play(true); // true = also play any child particle systems
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip == null) return;
        if (audioSource != null) audioSource.PlayOneShot(clip);
        else AudioSource.PlayClipAtPoint(clip, transform.position);
    }
}