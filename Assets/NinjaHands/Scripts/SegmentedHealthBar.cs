using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Naruto-Storm-style two-tier health bar. Total health is split into a green
/// "upper" tier and a yellow/red "lower" tier (50/50 by default). Each tier is
/// its own 3-layer stack (background / trail / current) using the same fill
/// mechanic. The green tier must fully deplete before the yellow/red tier
/// becomes visible. Attach to the Health Bar root and wire up all six Images.
/// </summary>
public class TwoTierHealthBar : MonoBehaviour
{
    [Header("Tier 1 - Green (upper health range)")]
    public Image tier1Background;  // Yellow, static fillAmount = 1 while tier1 active
    public Image tier1Trail;       // Dark green, chases tier1Current
    public Image tier1Current;     // Bright green, snaps instantly

    [Header("Tier 2 - Yellow/Red (lower health range)")]
    public Image tier2Background;  // Gray, static fillAmount = 1 while tier2 active
    public Image tier2Trail;       // Dark red/brown, chases tier2Current
    public Image tier2Current;     // Yellow above threshold, red below

    [Header("Tier Split")]
    [Range(0.01f, 0.99f)]
    [Tooltip("Fraction of total health belonging to tier 1 (green). 0.5 = even split.")]
    public float tierSplitFraction = 0.5f;

    [Header("Tier 2 Recolor")]
    [Range(0f, 1f)]
    [Tooltip("When tier2's own fraction drops below this, tier2Current recolors to red.")]
    public float redThreshold = 0.3f;
    public Color tier2NormalColor = new Color(1f, 0.76f, 0.13f); // yellow
    public Color tier2CriticalColor = new Color(0.85f, 0.1f, 0.1f); // red

    [Header("Trail Tuning")]
    public float trailCatchUpSpeed = 0.3f; // fraction per second, applies to both trails
    public float trailDelay = 0.5f;        // seconds to wait before trail starts chasing after a hit

    float tier1Target = 1f;
    float tier2Target = 1f;
    bool tier1Active = true;
    bool initialized = false;
    float trailDelayTimer = 0f;

    void Awake()
    {
        SetHealth(1f, 1f); // start full; caller should immediately call SetHealth with real values
    }

    /// <summary>
    /// Main entry point. current/max can be any units - only the ratio matters.
    /// </summary>
    public void SetHealth(float current, float max)
    {
        if (max <= 0f) { current = 0f; max = 1f; }
        float overallFraction = Mathf.Clamp01(current / max);
        SetHealthFraction(overallFraction);
    }

    public void SetHealthFraction(float fraction01)
    {
        fraction01 = Mathf.Clamp01(fraction01);

        float tier2Max = tierSplitFraction;
        float tier1Max = 1f - tierSplitFraction;

        bool wasTier1Active = tier1Active;
        float previousTier1Target = tier1Target;
        float previousTier2Target = tier2Target;

        if (fraction01 > tier2Max)
        {
            tier1Active = true;
            tier1Target = tier1Max > 0f ? (fraction01 - tier2Max) / tier1Max : 0f;
            tier2Target = 1f;
        }
        else
        {
            tier1Active = false;
            tier1Target = 0f;
            tier2Target = tier2Max > 0f ? fraction01 / tier2Max : 0f;
        }

        // Any real change to either tier's target restarts the delay before trails chase.
        if (!Mathf.Approximately(tier1Target, previousTier1Target) ||
            !Mathf.Approximately(tier2Target, previousTier2Target))
        {
            trailDelayTimer = trailDelay;
        }

        if (tier1Current != null) tier1Current.fillAmount = tier1Target;
        if (tier2Current != null) tier2Current.fillAmount = tier1Active ? 1f : tier2Target;

        if (wasTier1Active && !tier1Active && tier2Trail != null)
        {
            tier2Trail.fillAmount = 1f;
        }

        UpdateTierVisibility();
        UpdateTier2Color();
        initialized = true;
    }
    void UpdateTierVisibility()
    {
        // Tier1's background sits on top of tier2 and is opaque at fillAmount 1,
        // so tier2 is naturally hidden while tier1 is active. Explicitly toggling
        // tier2's GameObjects keeps it cheap and avoids any edge-case bleed-through.
        if (tier1Background != null) tier1Background.gameObject.SetActive(tier1Active);
        if (tier1Trail != null) tier1Trail.gameObject.SetActive(tier1Active);
        if (tier1Current != null) tier1Current.gameObject.SetActive(tier1Active);

        if (tier2Background != null) tier2Background.gameObject.SetActive(!tier1Active);
        if (tier2Trail != null) tier2Trail.gameObject.SetActive(!tier1Active);
        if (tier2Current != null) tier2Current.gameObject.SetActive(!tier1Active);
    }

    void UpdateTier2Color()
    {
        if (tier2Current == null) return;
        tier2Current.color = (tier2Target < redThreshold) ? tier2CriticalColor : tier2NormalColor;
    }

    
    void Update()
    {
        if (!initialized) return;

        if (trailDelayTimer > 0f)
        {
            trailDelayTimer -= Time.deltaTime;
            return; // hold trail in place until delay expires
        }

        if (tier1Active && tier1Trail != null)
        {
            tier1Trail.fillAmount = Mathf.MoveTowards(
                tier1Trail.fillAmount, tier1Target, trailCatchUpSpeed * Time.deltaTime);
        }
        else if (!tier1Active && tier2Trail != null)
        {
            tier2Trail.fillAmount = Mathf.MoveTowards(
                tier2Trail.fillAmount, tier2Target, trailCatchUpSpeed * Time.deltaTime);
        }
    }
}