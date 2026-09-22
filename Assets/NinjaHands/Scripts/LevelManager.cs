using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TargetSetEntry
{
    [Tooltip("Label for your own reference - not matched against anything.")]
    public string setName;

    [Tooltip("All target GameObjects that belong to this set. Shown together, hidden together.")]
    public List<GameObject> targets = new List<GameObject>();
}

/// <summary>
/// Switches between mutually-exclusive sets of targets (levels/modes).
/// Objects in alwaysActive (player rig, spawn points, debug tools) are never
/// touched by set switching - they stay exactly as they are, always.
/// </summary>
public class LevelManager : MonoBehaviour
{
    [SerializeField] private List<TargetSetEntry> targetSets = new List<TargetSetEntry>();

    [Tooltip("Which set is active at scene start.")]
    [SerializeField] private int startingSetIndex = 2; // Level3 by default

    [Header("Editor Preview")]
    [Tooltip("Drag to preview a level directly in the Inspector, in Edit Mode or Play Mode.")]
    [Range(0, 5)]
    [SerializeField] private int previewSetIndex = 2;

    [Header("Never Hidden")]
    [Tooltip("Objects that should always stay active regardless of which set is showing - OVRCameraRig, debug tools, shuriken spawn point, etc.")]
    [SerializeField] private List<GameObject> alwaysActive = new List<GameObject>();

    private int _activeIndex = -1;
    private int _lastPreviewIndex = -1;

    private void Start()
    {
        // Intentionally does nothing by default now — call BeginGame() explicitly
        // instead (either directly, or from a title screen's Start button).
        // If you want the OLD auto-start behavior when there's no title screen,
        // just call BeginGame() here directly.
    }

    public void BeginGame()
    {
        EnforceAlwaysActive();
        ShowSet(startingSetIndex);
        previewSetIndex = startingSetIndex;
        _lastPreviewIndex = startingSetIndex;
    }

    /// <summary>
    /// Immediate set switch. Safe to call directly from code, BeginGame(), or OnValidate().
    /// If wiring a table button's When Select() to change levels, use SelectLevel(int)
    /// below instead — it waits a frame so we never deactivate something the
    /// Interaction SDK is still mid-update on for that same select event.
    /// </summary>
    public void ShowSet(int index)
    {
        if (index < 0 || index >= targetSets.Count) return;

        // Wipe every shuriken currently in the scene before switching levels,
        // so nothing from the old level carries over.
        StuckShurikenManager.Instance?.ClearAll();

        // Also wipe any currently-spawned FireTarget clone — these are dynamically
        // instantiated with no parent, so they're invisible to targetSets tracking
        // and would otherwise survive a level switch indefinitely (e.g. stuck
        // mid-air between up/down cycles).
        DespawnAllFireTargets();

        for (int i = 0; i < targetSets.Count; i++)
        {
            SetActiveState(targetSets[i], i == index);
        }

        _activeIndex = index;
        previewSetIndex = index;
        _lastPreviewIndex = index;
        EnforceAlwaysActive();
    }

    /// <summary>Wire this to a table button's When Select() instead of ShowSet() directly.</summary>
    public void SelectLevel(int index)
    {
        StartCoroutine(ShowSetNextFrame(index));
    }

    private IEnumerator ShowSetNextFrame(int index)
    {
        yield return null;
        ShowSet(index);
    }

    /// <summary>Resets every resettable target (Target3Health, Target2Health) in the currently active set.</summary>
    public void ResetCurrentLevel()
    {
        if (_activeIndex < 0 || _activeIndex >= targetSets.Count) return;

        foreach (var target in targetSets[_activeIndex].targets)
        {
            if (target == null) continue;

            var t1 = target.GetComponentInChildren<Target1Health>(true);
            if (t1 != null) t1.ResetHealth();

            var t3 = target.GetComponentInChildren<Target3Health>(true);
            if (t3 != null) t3.ResetHealth();

            var t2 = target.GetComponentInChildren<Target2Health>(true);
            if (t2 != null) t2.ResetHealth();
        }
    }

    private void SetActiveState(TargetSetEntry set, bool active)
    {
        foreach (var target in set.targets)
        {
            if (target != null)
            {
                target.SetActive(active);
            }
        }
    }

    private void EnforceAlwaysActive()
    {
        foreach (var obj in alwaysActive)
        {
            if (obj != null && !obj.activeSelf)
            {
                obj.SetActive(true);
            }
        }
    }

    public int ActiveSetIndex => _activeIndex;

    private void OnValidate()
    {
        if (targetSets == null || targetSets.Count == 0) return;

        previewSetIndex = Mathf.Clamp(previewSetIndex, 0, targetSets.Count - 1);

        if (previewSetIndex != _lastPreviewIndex)
        {
            ShowSet(previewSetIndex);
        }
    }

    /// <summary>
    /// Immediate hide-everything. Used internally by BeginGame-adjacent flows.
    /// If wiring a "Back to Title" button's own When Select() to this,
    /// use HideAllSetsDeferred() below instead, for the same reason as SelectLevel().
    /// </summary>
    public void HideAllSets()
    {
        StuckShurikenManager.Instance?.ClearAll();

        // Same cleanup as ShowSet() — a mid-flight or stuck FireTarget clone
        // shouldn't survive a return to the title screen either.
        DespawnAllFireTargets();

        foreach (var set in targetSets)
        {
            SetActiveState(set, false);
        }

        _activeIndex = -1;
        // alwaysActive objects (player rig, level-select buttons, etc.) are left
        // untouched here intentionally — they should stay visible/functional even
        // back at the title screen.
    }

    /// <summary>Frame-delayed version of HideAllSets(), safe to call from a button's own select event.</summary>
    public void HideAllSetsDeferred()
    {
        StartCoroutine(HideAllSetsNextFrame());
    }

    private IEnumerator HideAllSetsNextFrame()
    {
        yield return null;
        HideAllSets();
    }

    private void DespawnAllFireTargets()
    {
        var fireTargets = FindObjectsByType<FireTarget>(FindObjectsSortMode.None);
        foreach (var ft in fireTargets)
        {
            ft.Despawn();
        }
    }
}
