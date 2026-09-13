using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sounds for the jutsu system. Shared cues (sign landed, denied, reset, detection started)
/// plus a per-jutsu cast clip looked up by jutsuName.
/// </summary>
public class JutsuAudio : MonoBehaviour
{
    [Serializable]
    public class JutsuSound
    {
        [Tooltip("Must match JutsuDefinition.jutsuName exactly.")]
        public string jutsuName;
        public AudioClip castClip;
        [Range(0f, 1f)] public float volume = 1f;
    }

    [SerializeField] private JutsuManager jutsuManager;
    [SerializeField] private AudioSource source;

    [Header("Shared cues")]
    [SerializeField] private AudioClip detectionStartedClip;
    [SerializeField] private AudioClip signCompletedClip;
    [SerializeField] private AudioClip notEnoughChakraClip;
    [SerializeField] private AudioClip sequenceResetClip;

    [Header("Per-jutsu cast sounds")]
    [SerializeField] private List<JutsuSound> jutsuSounds = new List<JutsuSound>();

    private readonly Dictionary<string, JutsuSound> _lookup = new Dictionary<string, JutsuSound>();

    private void Awake()
    {
        foreach (var s in jutsuSounds)
            if (!string.IsNullOrEmpty(s.jutsuName)) _lookup[s.jutsuName] = s;
    }

    private void OnEnable()
    {
        if (jutsuManager == null) return;
        jutsuManager.OnDetectionStarted.AddListener(HandleDetectionStarted);
        jutsuManager.OnSignCompleted.AddListener(HandleSignCompleted);
        jutsuManager.OnJutsuCompleted.AddListener(HandleJutsuCompleted);
        jutsuManager.OnNotEnoughChakra.AddListener(HandleDenied);
        jutsuManager.OnSequenceReset.AddListener(HandleReset);
    }

    private void OnDisable()
    {
        if (jutsuManager == null) return;
        jutsuManager.OnDetectionStarted.RemoveListener(HandleDetectionStarted);
        jutsuManager.OnSignCompleted.RemoveListener(HandleSignCompleted);
        jutsuManager.OnJutsuCompleted.RemoveListener(HandleJutsuCompleted);
        jutsuManager.OnNotEnoughChakra.RemoveListener(HandleDenied);
        jutsuManager.OnSequenceReset.RemoveListener(HandleReset);
    }

    private void HandleDetectionStarted(string jutsuName) => Play(detectionStartedClip);
    private void HandleSignCompleted(string jutsuName, int index) => Play(signCompletedClip);
    private void HandleDenied(string jutsuName, float cost, float current) => Play(notEnoughChakraClip);
    private void HandleReset(string jutsuName) => Play(sequenceResetClip);

    private void HandleJutsuCompleted(string jutsuName)
    {
        if (_lookup.TryGetValue(jutsuName, out var s) && s.castClip != null)
            Play(s.castClip, s.volume);
    }

    private void Play(AudioClip clip, float volume = 1f)
    {
        if (source != null && clip != null) source.PlayOneShot(clip, volume);
    }
}