using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class JutsuVFXEntry
{
    [Tooltip("Must match JutsuDefinition.jutsuName exactly.")]
    public string jutsuName;

    [Tooltip("VFX GameObject, positioned at the mouth, parented under the head/camera rig.")]
    public GameObject vfxObject;

    [Tooltip("Optional: if the VFX object has a ParticleSystem, Play()/Stop() is called on it. If left empty, the VFX GameObject is just SetActive(true/false).")]
    public ParticleSystem particleSystem;

    [Tooltip("Seconds the VFX stays active.")]
    public float duration = 6f;
}

public class JutsuVFXController : MonoBehaviour
{
    [SerializeField] private JutsuManager jutsuManager;
    [SerializeField] private List<JutsuVFXEntry> vfxEntries = new List<JutsuVFXEntry>();

    private readonly Dictionary<string, JutsuVFXEntry> _lookup = new Dictionary<string, JutsuVFXEntry>();
    private Coroutine _activeRoutine;

    private void Awake()
    {
        foreach (var entry in vfxEntries)
        {
            if (!string.IsNullOrEmpty(entry.jutsuName))
                _lookup[entry.jutsuName] = entry;

            // start hidden
            if (entry.vfxObject != null)
                entry.vfxObject.SetActive(false);
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
        if (!_lookup.TryGetValue(jutsuName, out var entry) || entry.vfxObject == null)
            return;

        if (_activeRoutine != null)
            StopCoroutine(_activeRoutine);

        _activeRoutine = StartCoroutine(PlayVFXForDuration(entry));
    }

    private IEnumerator PlayVFXForDuration(JutsuVFXEntry entry)
    {
        entry.vfxObject.SetActive(true);

        if (entry.particleSystem != null)
        {
            entry.particleSystem.Clear();
            entry.particleSystem.Play();
        }

        yield return new WaitForSeconds(entry.duration);

        if (entry.particleSystem != null)
            entry.particleSystem.Stop();

        entry.vfxObject.SetActive(false);
        _activeRoutine = null;
    }
}