using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class JutsuDamageEntry
{
    [Tooltip("Must match JutsuDefinition.jutsuName exactly.")]
    public string jutsuName;

    [Tooltip("Where the raycast originates and points forward from.")]
    public Transform origin;

    public float damagePerTick = 10f;
    public float tickInterval = 1f;
    public float startDelay = 0.3f;
    public float duration = 6f;
    public float maxRange = 20f;

    [Header("Audio")]
    [Tooltip("If left empty, one is added automatically at Origin.")]
    public AudioSource audioSource;
    [Tooltip("Plays once immediately when the jutsu completes/triggers.")]
    public AudioClip startSound;
    [Tooltip("Loops for the duration of the jutsu's damage window (e.g. fire crackling).")]
    public AudioClip loopSound;

    [NonSerialized] public LineRenderer debugLine;
}

public class JutsuDamageController : MonoBehaviour
{
    [SerializeField] private JutsuManager jutsuManager;
    [SerializeField] private List<JutsuDamageEntry> damageEntries = new List<JutsuDamageEntry>();

    [Header("Debug")]
    [SerializeField] private bool drawDebugRay = false;
    [SerializeField] private Material debugLineMaterial;
    [SerializeField] private float debugLineWidth = 0.02f;
    [SerializeField] private Color debugLineColor = Color.red;

    [SerializeField] private LayerMask hittableLayers = ~0;

    private readonly Dictionary<string, JutsuDamageEntry> _lookup = new Dictionary<string, JutsuDamageEntry>();
    private Coroutine _activeRoutine;
    private JutsuDamageEntry _activeEntry;

    private void Awake()
    {
        foreach (var entry in damageEntries)
        {
            if (!string.IsNullOrEmpty(entry.jutsuName))
                _lookup[entry.jutsuName] = entry;

            if (entry.audioSource == null && entry.origin != null)
            {
                entry.audioSource = entry.origin.GetComponent<AudioSource>();
                if (entry.audioSource == null)
                {
                    entry.audioSource = entry.origin.gameObject.AddComponent<AudioSource>();
                }
            }

            if (drawDebugRay)
            {
                entry.debugLine = CreateDebugLine(entry.jutsuName);
            }
        }
    }

    private LineRenderer CreateDebugLine(string name)
    {
        GameObject lineObj = new GameObject($"DebugRay_{name}");
        lineObj.transform.SetParent(transform);

        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.startWidth = debugLineWidth;
        lr.endWidth = debugLineWidth;
        lr.useWorldSpace = true;

        Material mat = debugLineMaterial;
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            mat = new Material(shader);
        }
        lr.material = mat;
        lr.startColor = debugLineColor;
        lr.endColor = debugLineColor;
        lr.enabled = false;

        return lr;
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
        if (!_lookup.TryGetValue(jutsuName, out var entry) || entry.origin == null)
            return;

        if (_activeRoutine != null)
        {
            StopCoroutine(_activeRoutine);
        }
        StopAudio(_activeEntry);

        _activeEntry = entry;
        _activeRoutine = StartCoroutine(DamageTickRoutine(entry));

        PlayStartAndLoopAudio(entry);
    }

    private void PlayStartAndLoopAudio(JutsuDamageEntry entry)
    {
        if (entry.audioSource == null) return;

        if (entry.startSound != null)
        {
            entry.audioSource.PlayOneShot(entry.startSound);
        }

        if (entry.loopSound != null)
        {
            entry.audioSource.clip = entry.loopSound;
            entry.audioSource.loop = true;
            entry.audioSource.Play();
        }
    }

    private void StopAudio(JutsuDamageEntry entry)
    {
        if (entry == null || entry.audioSource == null) return;
        entry.audioSource.loop = false;
        entry.audioSource.Stop();
    }

    private IEnumerator DamageTickRoutine(JutsuDamageEntry entry)
    {
        yield return new WaitForSeconds(entry.startDelay);

        float elapsed = 0f;

        while (elapsed < entry.duration)
        {
            DoTick(entry);
            yield return new WaitForSeconds(entry.tickInterval);
            elapsed += entry.tickInterval;
        }

        if (entry.debugLine != null)
            entry.debugLine.enabled = false;

        StopAudio(entry);

        if (_activeEntry == entry)
        {
            _activeEntry = null;
        }
        _activeRoutine = null;
    }

    private void DoTick(JutsuDamageEntry entry)
    {
        Ray ray = new Ray(entry.origin.position, entry.origin.forward);

        Vector3 endPoint = ray.origin + ray.direction * entry.maxRange;
        bool hitSomething = Physics.Raycast(ray, out RaycastHit hit, entry.maxRange, hittableLayers);

        if (hitSomething)
        {
            endPoint = hit.point;

            IHittable hittable = hit.collider.GetComponentInParent<IHittable>();
            if (hittable != null)
            {
                HitInfo info = new HitInfo
                {
                    point = hit.point,
                    damage = entry.damagePerTick,
                    source = HitSource.Jutsu,
                    sourceName = entry.jutsuName
                };
                hittable.OnHit(info);
            }
        }

        if (drawDebugRay)
        {
            Debug.DrawRay(ray.origin, ray.direction * entry.maxRange, Color.red, 5f);

            if (entry.debugLine != null)
            {
                entry.debugLine.enabled = true;
                entry.debugLine.SetPosition(0, ray.origin);
                entry.debugLine.SetPosition(1, endPoint);
            }
        }
    }

    void OnDrawGizmos()
    {
        if (!drawDebugRay) return;

        foreach (var entry in damageEntries)
        {
            if (entry.origin == null) continue;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(entry.origin.position, 0.1f);
            Gizmos.color = Color.red;
            Gizmos.DrawLine(entry.origin.position, entry.origin.position + entry.origin.forward * entry.maxRange);
        }
    }
}