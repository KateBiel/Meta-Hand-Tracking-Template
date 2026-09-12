using UnityEngine;

/// <summary>
/// Turns the hand fire particle systems on while charging and off when the
/// charge pose is released. Optionally scales the fire with chakra level.
/// </summary>
public class ChakraFireController : MonoBehaviour
{
    [SerializeField] private ChakraSystem chakra;

    [Tooltip("Root particle systems for each hand (ChakraFire_L / ChakraFire_R). Children are driven too.")]
    [SerializeField] private ParticleSystem[] fireSystems;

    [Header("Scale with chakra level")]
    [SerializeField] private bool scaleWithChakra = true;

    [Tooltip("Multiplier applied to the fire's emission rate at 0% and 100% chakra.")]
    [SerializeField] private float emissionAtEmpty = 0.4f;
    [SerializeField] private float emissionAtFull = 1.5f;

    [Tooltip("Multiplier applied to the fire's start size at 0% and 100% chakra.")]
    [SerializeField] private float sizeAtEmpty = 0.6f;
    [SerializeField] private float sizeAtFull = 1.2f;

    // Base values captured from each system (incl. children) so scaling is relative.
    private ParticleSystem[] _all;
    private float[] _baseRate;
    private float[] _baseSize;
    private bool _playing;

    private void Awake()
    {
        var list = new System.Collections.Generic.List<ParticleSystem>();
        foreach (var ps in fireSystems)
        {
            if (ps == null) continue;
            list.AddRange(ps.GetComponentsInChildren<ParticleSystem>(true));
        }
        _all = list.ToArray();
        _baseRate = new float[_all.Length];
        _baseSize = new float[_all.Length];

        foreach (var ps in fireSystems)
        {
            if (ps == null) continue;
            var cfxr = ps.GetComponent("CFXR_Effect");
            if (cfxr != null)
                Debug.LogWarning($"[ChakraFireController] {ps.name} has a CFXR_Effect component. Make sure its Clear Behavior is set to None, or CFXR will destroy the effect when it stops.", ps);
        }

        for (int i = 0; i < _all.Length; i++)
        {
            _baseRate[i] = _all[i].emission.rateOverTime.constant;
            _baseSize[i] = _all[i].main.startSizeMultiplier;
            var main = _all[i].main;
            main.playOnAwake = false;
            _all[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void OnEnable()
    {
        if (chakra == null) return;
        chakra.OnChargingStarted.AddListener(StartFire);
        chakra.OnChargingStopped.AddListener(StopFire);
        chakra.OnChakraChanged.AddListener(HandleChakraChanged);
        if (chakra.IsCharging) StartFire();
    }

    private void OnDisable()
    {
        if (chakra == null) return;
        chakra.OnChargingStarted.RemoveListener(StartFire);
        chakra.OnChargingStopped.RemoveListener(StopFire);
        chakra.OnChakraChanged.RemoveListener(HandleChakraChanged);
        StopFire();
    }

    private void StartFire()
    {
        if (_playing) return;
        _playing = true;
        ApplyScale(chakra.Normalized);
        foreach (var ps in _all) if (ps != null) ps.Play(true);
    }

    private void StopFire()
    {
        if (!_playing) return;
        _playing = false;
        // StopEmitting lets live particles finish their lifetime instead of popping out.
        foreach (var ps in _all) if (ps != null) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    private void HandleChakraChanged(float current, float max)
    {
        if (_playing) ApplyScale(max > 0f ? current / max : 0f);
    }

    private void ApplyScale(float fill)
    {
        if (!scaleWithChakra) return;
        float rateMul = Mathf.Lerp(emissionAtEmpty, emissionAtFull, fill);
        float sizeMul = Mathf.Lerp(sizeAtEmpty, sizeAtFull, fill);

        for (int i = 0; i < _all.Length; i++)
        {
            if (_all[i] == null) continue; // destroyed (e.g. CFXR_Effect Clear Behavior = Destroy)
            var emission = _all[i].emission;
            emission.rateOverTime = _baseRate[i] * rateMul;
            var main = _all[i].main;
            main.startSizeMultiplier = _baseSize[i] * sizeMul;
        }
    }
}