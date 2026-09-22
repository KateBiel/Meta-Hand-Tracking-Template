using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class JutsuVFXEntry
{
    public string jutsuName;
    public GameObject vfxObject;

    [Tooltip("All particle systems that make up this VFX (e.g. fire + smoke children). Every one listed here will be explicitly Cleared+Played when the jutsu completes, and Stopped when the VFX ends — don't rely on 'Play On Awake'.")]
    public ParticleSystem[] particleSystems;

    public float duration = 6f;
}

public class JutsuVFXController : MonoBehaviour
{
    [SerializeField] private JutsuManager jutsuManager;
    [SerializeField] private List<JutsuVFXEntry> vfxEntries = new List<JutsuVFXEntry>();

    private Dictionary<string, JutsuVFXEntry> _lookup;

    private void Awake()
    {
        _lookup = new Dictionary<string, JutsuVFXEntry>();
        foreach (var entry in vfxEntries)
        {
            if (entry == null || string.IsNullOrEmpty(entry.jutsuName)) continue;
            _lookup[entry.jutsuName] = entry;

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
        if (_lookup == null || !_lookup.TryGetValue(jutsuName, out JutsuVFXEntry entry)) return;
        if (entry.vfxObject == null) return;

        StartCoroutine(PlayVFXForDuration(entry));
    }

    private IEnumerator PlayVFXForDuration(JutsuVFXEntry entry)
    {
        entry.vfxObject.SetActive(true);

        if (entry.particleSystems != null)
        {
            foreach (var ps in entry.particleSystems)
            {
                if (ps == null) continue;
                ps.Clear(true);
                ps.Play(true);
            }
        }

        yield return new WaitForSeconds(entry.duration);

        if (entry.particleSystems != null)
        {
            foreach (var ps in entry.particleSystems)
            {
                if (ps == null) continue;
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        entry.vfxObject.SetActive(false);
    }
}
