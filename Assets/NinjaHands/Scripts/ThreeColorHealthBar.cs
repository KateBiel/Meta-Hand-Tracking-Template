using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Single-segment health bar that recolors across three bands as it depletes:
/// Green (full) -> Yellow (mid) -> Red (critical). Same background/trail/current
/// 3-layer stack and delayed trail-chase mechanic as TwoTierHealthBar, just without
/// the tier split.
/// </summary>
public class ThreeColorHealthBar : MonoBehaviour
{
    [Header("Bar Layers")]
    public Image background;  // Static fillAmount = 1
    public Image trail;       // Chases current, delayed
    public Image current;     // Snaps instantly, recolors by threshold

    [Header("Color Thresholds")]
    [Range(0f, 1f)]
    [Tooltip("Above this fraction, bar is green.")]
    public float greenThreshold = 0.6f;
    [Range(0f, 1f)]
    [Tooltip("Above this fraction (but below greenThreshold), bar is yellow. Below this, red.")]
    public float redThreshold = 0.3f;

    [Header("Colors")]
    public Color greenColor = new Color(0.2f, 0.85f, 0.3f);
    public Color yellowColor = new Color(1f, 0.76f, 0.13f);
    public Color redColor = new Color(0.85f, 0.1f, 0.1f);

    [Header("Trail Tuning")]
    public float trailCatchUpSpeed = 0.3f; // fraction per second
    public float trailDelay = 0.5f;        // seconds before trail starts chasing after a hit

    private float _target = 1f;
    private bool _initialized = false;
    private float _trailDelayTimer = 0f;

    void Awake()
    {
        SetHealth(1f, 1f); // start full; caller should immediately call SetHealth with real values
    }

    /// <summary>
    /// Main entry point. current/max can be any units - only the ratio matters.
    /// </summary>
    public void SetHealth(float currentValue, float max)
    {
        if (max <= 0f) { currentValue = 0f; max = 1f; }
        SetHealthFraction(Mathf.Clamp01(currentValue / max));
    }

    public void SetHealthFraction(float fraction01)
    {
        fraction01 = Mathf.Clamp01(fraction01);

        float previousTarget = _target;
        _target = fraction01;

        if (!Mathf.Approximately(_target, previousTarget))
        {
            _trailDelayTimer = trailDelay;
        }

        if (current != null) current.fillAmount = _target;

        UpdateColor();
        _initialized = true;
    }

    void UpdateColor()
    {
        if (current == null) return;

        if (_target >= greenThreshold)
        {
            current.color = greenColor;
        }
        else if (_target >= redThreshold)
        {
            current.color = yellowColor;
        }
        else
        {
            current.color = redColor;
        }
    }

    void Update()
    {
        if (!_initialized) return;

        if (_trailDelayTimer > 0f)
        {
            _trailDelayTimer -= Time.deltaTime;
            return; // hold trail in place until delay expires
        }

        if (trail != null)
        {
            trail.fillAmount = Mathf.MoveTowards(
                trail.fillAmount, _target, trailCatchUpSpeed * Time.deltaTime);
        }
    }
}