using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Player chakra pool. One instance per player.
/// JutsuManager checks CanAfford() before detection and calls Spend() on completion.
/// </summary>
public class ChakraSystem : MonoBehaviour
{
    [SerializeField] private float maxChakra = 100f;
    [SerializeField] private float startChakra = 100f;

    [Tooltip("Chakra regained per second while not casting. 0 = no regen.")]
    [SerializeField] private float regenPerSecond = 5f;

    [Tooltip("Seconds after spending before regen resumes.")]
    [SerializeField] private float regenDelay = 2f;

    [Header("Events")]
    [Tooltip("current, max — fires whenever the value changes. Drive bars / outline glow from this.")]
    public UnityEvent<float, float> OnChakraChanged;
    public UnityEvent<float> OnChakraSpent;   // amount spent
    public UnityEvent OnChakraEmpty;
    public UnityEvent OnChakraFull;

    private float _current;
    private float _regenCooldown;
    private bool _wasFull;
    private bool _wasEmpty;

    public float Current => _current;
    public float Max => maxChakra;
    public float Normalized => maxChakra > 0f ? _current / maxChakra : 0f;

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
        if (regenPerSecond <= 0f || _current >= maxChakra) return;

        if (_regenCooldown > 0f)
        {
            _regenCooldown -= Time.deltaTime;
            return;
        }

        SetChakra(_current + regenPerSecond * Time.deltaTime);
    }

    public bool CanAfford(float cost) => _current >= cost;

    /// <summary>Spend chakra. Returns false (and spends nothing) if you can't afford it.</summary>
    public bool Spend(float cost)
    {
        if (cost <= 0f) return true;
        if (!CanAfford(cost)) return false;

        SetChakra(_current - cost);
        _regenCooldown = regenDelay;
        OnChakraSpent?.Invoke(cost);
        return true;
    }

    public void Restore(float amount) => SetChakra(_current + amount);
    public void RestoreFull() => SetChakra(maxChakra);

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