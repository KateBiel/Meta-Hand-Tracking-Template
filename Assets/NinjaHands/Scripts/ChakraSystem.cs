using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Player chakra pool. Starts at 0, fills only while charging (held charge pose),
/// never decays on its own. JutsuManager spends from it.
/// </summary>
public class ChakraSystem : MonoBehaviour
{
    [SerializeField] private float maxChakra = 100f;
    [SerializeField] private float startChakra = 0f;

    [Header("Charging")]
    [Tooltip("Chakra gained per second while charging. 25 = 4 seconds from empty to full.")]
    [SerializeField] private float chargePerSecond = 25f;

    [Header("Events")]
    [Tooltip("current, max — fires whenever the value changes.")]
    public UnityEvent<float, float> OnChakraChanged;
    public UnityEvent<float> OnChakraSpent;   // amount spent
    public UnityEvent OnChakraEmpty;
    public UnityEvent OnChakraFull;
    public UnityEvent OnChargingStarted;
    public UnityEvent OnChargingStopped;

    private float _current;
    private bool _charging;
    private bool _wasFull;
    private bool _wasEmpty;

    public float Current => _current;
    public float Max => maxChakra;
    public float Normalized => maxChakra > 0f ? _current / maxChakra : 0f;
    public bool IsCharging => _charging;
    public bool IsFull => _current >= maxChakra;

    private void Awake()
    {
        _current = Mathf.Clamp(startChakra, 0f, maxChakra);
        _wasFull = _current >= maxChakra;
        _wasEmpty = _current <= 0f;
    }

    private void Start()
    {
        OnChakraChanged?.Invoke(_current, maxChakra);
    }

    private void Update()
    {
        if (!_charging || chargePerSecond <= 0f || _current >= maxChakra) return;
        SetChakra(_current + chargePerSecond * Time.deltaTime);
    }

    /// <summary>Called by ChakraCharger every frame with the charge-pose state.</summary>
    public void SetCharging(bool charging)
    {
        if (charging == _charging) return;
        _charging = charging;
        if (_charging) OnChargingStarted?.Invoke();
        else OnChargingStopped?.Invoke();
    }

    public bool CanAfford(float cost) => _current >= cost;

    /// <summary>Spend chakra. Returns false (and spends nothing) if you can't afford it.</summary>
    public bool Spend(float cost)
    {
        if (cost <= 0f) return true;
        if (!CanAfford(cost)) return false;
        SetChakra(_current - cost);
        OnChakraSpent?.Invoke(cost);
        return true;
    }

    public void Restore(float amount) => SetChakra(_current + amount);
    public void RestoreFull() => SetChakra(maxChakra);
    public void Clear() => SetChakra(0f);

    private void SetChakra(float value)
    {
        float clamped = Mathf.Clamp(value, 0f, maxChakra);
        if (Mathf.Approximately(clamped, _current)) return;

        _current = clamped;
        OnChakraChanged?.Invoke(_current, maxChakra);

        bool isEmpty = _current <= 0f;
        bool isFull = _current >= maxChakra;
        if (isEmpty && !_wasEmpty) OnChakraEmpty?.Invoke();
        if (isFull && !_wasFull) OnChakraFull?.Invoke();
        _wasEmpty = isEmpty;
        _wasFull = isFull;
    }
}