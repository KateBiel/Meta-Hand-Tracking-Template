using System.Collections;
using UnityEngine;

/// <summary>
/// Sounds for chakra charging:
///   - chargeStart: one-shot when charging begins
///   - chargeLoop: loops while charging; pitch/volume rise with chakra level
///   - chargeStop / chakraFull: optional one-shots
/// Needs two AudioSources (one-shot + loop). Put this on an object between the hands
/// or on the camera; set the AudioSources' Spatial Blend to taste.
/// </summary>
public class ChakraAudio : MonoBehaviour
{
    [SerializeField] private ChakraSystem chakra;

    [Header("Sources")]
    [SerializeField] private AudioSource oneShotSource;
    [SerializeField] private AudioSource loopSource;

    [Header("Clips")]
    [SerializeField] private AudioClip chargeStartClip;
    [SerializeField] private AudioClip chargeLoopClip;
    [SerializeField] private AudioClip chargeStopClip;   // optional
    [SerializeField] private AudioClip chakraFullClip;   // optional

    [Header("Loop shaping")]
    [SerializeField] private float loopFadeIn = 0.15f;
    [SerializeField] private float loopFadeOut = 0.35f;
    [SerializeField] private float loopVolumeAtEmpty = 0.4f;
    [SerializeField] private float loopVolumeAtFull = 1.0f;
    [SerializeField] private float loopPitchAtEmpty = 0.9f;
    [SerializeField] private float loopPitchAtFull = 1.25f;

    private Coroutine _fade;
    private float _targetVolume;

    private void OnEnable()
    {
        if (chakra == null) return;
        chakra.OnChargingStarted.AddListener(HandleStart);
        chakra.OnChargingStopped.AddListener(HandleStop);
        chakra.OnChakraChanged.AddListener(HandleChanged);
        chakra.OnChakraFull.AddListener(HandleFull);
    }

    private void OnDisable()
    {
        if (chakra == null) return;
        chakra.OnChargingStarted.RemoveListener(HandleStart);
        chakra.OnChargingStopped.RemoveListener(HandleStop);
        chakra.OnChakraChanged.RemoveListener(HandleChanged);
        chakra.OnChakraFull.RemoveListener(HandleFull);
        if (loopSource != null) loopSource.Stop();
    }

    private void HandleStart()
    {
        if (oneShotSource != null && chargeStartClip != null)
            oneShotSource.PlayOneShot(chargeStartClip);

        if (loopSource != null && chargeLoopClip != null)
        {
            loopSource.clip = chargeLoopClip;
            loopSource.loop = true;
            ApplyLevel(chakra.Normalized);
            if (!loopSource.isPlaying) { loopSource.volume = 0f; loopSource.Play(); }
            StartFade(_targetVolume, loopFadeIn, stopAtEnd: false);
        }
    }

    private void HandleStop()
    {
        if (oneShotSource != null && chargeStopClip != null)
            oneShotSource.PlayOneShot(chargeStopClip);

        if (loopSource != null && loopSource.isPlaying)
            StartFade(0f, loopFadeOut, stopAtEnd: true);
    }

    private void HandleChanged(float current, float max)
    {
        if (chakra.IsCharging) ApplyLevel(max > 0f ? current / max : 0f);
    }

    private void HandleFull()
    {
        if (oneShotSource != null && chakraFullClip != null)
            oneShotSource.PlayOneShot(chakraFullClip);
    }

    private void ApplyLevel(float fill)
    {
        if (loopSource == null) return;
        loopSource.pitch = Mathf.Lerp(loopPitchAtEmpty, loopPitchAtFull, fill);
        _targetVolume = Mathf.Lerp(loopVolumeAtEmpty, loopVolumeAtFull, fill);
        if (_fade == null) loopSource.volume = _targetVolume;
    }

    private void StartFade(float to, float time, bool stopAtEnd)
    {
        if (_fade != null) StopCoroutine(_fade);
        _fade = StartCoroutine(Fade(to, time, stopAtEnd));
    }

    private IEnumerator Fade(float to, float time, bool stopAtEnd)
    {
        float from = loopSource.volume;
        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            loopSource.volume = Mathf.Lerp(from, to, time > 0f ? t / time : 1f);
            yield return null;
        }
        loopSource.volume = to;
        if (stopAtEnd) loopSource.Stop();
        _fade = null;
    }
}