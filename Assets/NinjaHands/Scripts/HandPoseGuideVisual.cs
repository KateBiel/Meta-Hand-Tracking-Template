using UnityEngine;

public class HandPoseGuideVisual : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ChakraSystem chakra;

    [Tooltip("Only used when Drive Externally is OFF (i.e. this is the Chakra Charger guide). If assigned, this guide will hide itself while JutsuManager is detecting a jutsu, so it doesn't overlap the jutsu sign guides. Leave empty on the jutsu sign guide instances (Drive Externally = true) — it's ignored there anyway.")]
    [SerializeField] private JutsuManager jutsuManager;

    [SerializeField] private Transform realLeftHand;
    [SerializeField] private Transform realRightHand;
    [SerializeField] private Transform guideLeftHand;
    [SerializeField] private Transform guideRightHand;
    [SerializeField] private SkinnedMeshRenderer[] guideRenderers;

    [Header("Trigger Conditions")]
    [Tooltip("If true, ignore chakra entirely and rely on SetExternalShouldShow(bool) being called by another script (used for jutsu sign guides). If false, uses chakra.IsFull like before (used for the Chakra Charger guide).")]
    [SerializeField] private bool driveExternally = false;
    [SerializeField] private float handDistanceThreshold = 0.2f;
    [Tooltip("Seconds handDistance must stay on one side of the threshold before the show/hide state actually flips. Prevents tracking jitter from snapping the fade back to bright.")]
    [SerializeField] private float debounceTime = 0.15f;

    [Tooltip("Chakra-driven guide only. Before the player has ever charged to full, the guide shows any time chakra isn't 100%. After that first full charge, it only shows again once chakra drops to this value or below (so it doesn't nag on every small dip once the player already knows how to charge).")]
    [SerializeField] private float lowChakraThreshold = 20f;

    private bool _externalShouldShow;
    private bool _debouncedShouldShow;
    private float _debounceTimer;
    private bool _hasReachedFullOnce;

    [Header("Pulse Animation (outline)")]
    [ColorUsage(true, true)][SerializeField] private Color outlineColor = new Color(2f, 2f, 0f);
    [SerializeField] private float minOutlineWidth = 0.0008f;
    [SerializeField] private float maxOutlineWidth = 0.0025f;
    [SerializeField] private float pulseSpeed = 2f;

    [Header("Opacity — base hand fades with the same pulse as the outline")]
    [Range(0f, 1f)][SerializeField] private float maxBaseOpacity = 0.5f;
    [Range(0f, 1f)][SerializeField] private float maxOutlineOpacity = 1f;

    [Header("Shader properties (Interaction/OculusHand)")]
    [SerializeField] private string opacityProperty = "_Opacity";
    [SerializeField] private string outlineColorProperty = "_OutlineColor";
    [SerializeField] private string outlineWidthProperty = "_OutlineWidth";
    [SerializeField] private string outlineOpacityProperty = "_OutlineOpacity";

    [Header("Fade Out")]
    [SerializeField] private float fadeOutDuration = 0.4f;

    private MaterialPropertyBlock _mpb;
    private int _opacityId, _outlineColorId, _outlineWidthId, _outlineOpacityId;
    private float _currentIntensity; // 0..1, only meaningful while actively pulsing

    private void Awake()
    {
        _mpb = new MaterialPropertyBlock();
        _opacityId = Shader.PropertyToID(opacityProperty);
        _outlineColorId = Shader.PropertyToID(outlineColorProperty);
        _outlineWidthId = Shader.PropertyToID(outlineWidthProperty);
        _outlineOpacityId = Shader.PropertyToID(outlineOpacityProperty);

        if (guideRenderers != null)
            foreach (var r in guideRenderers) if (r != null) r.enabled = true; // set once, never toggled per-frame
    }

    /// <summary>Call this from an external controller when driveExternally is true (e.g. a jutsu sign guide controller).</summary>
    public void SetExternalShouldShow(bool value)
    {
        _externalShouldShow = value;
    }

    /// <summary>Debounced show/hide state this frame — lets other scripts (e.g. a prompt canvas) stay in sync with this guide without duplicating the debounce logic.</summary>
    public bool ShouldShow => _debouncedShouldShow;

    private void Update()
    {
        if (!driveExternally && chakra != null && chakra.IsFull)
            _hasReachedFullOnce = true;

        bool chakraCondition;
        if (chakra == null)
        {
            chakraCondition = false;
        }
        else if (!_hasReachedFullOnce)
        {
            chakraCondition = !chakra.IsFull;
        }
        else
        {
            chakraCondition = chakra.Current <= lowChakraThreshold;
        }

        bool baseCondition = driveExternally
            ? _externalShouldShow
            : (chakraCondition && (jutsuManager == null || !jutsuManager.IsDetecting));

        float handDistance = ComputeHandDistance();
        bool rawShouldShow = baseCondition && handDistance > handDistanceThreshold;

        if (rawShouldShow != _debouncedShouldShow)
        {
            _debounceTimer += Time.deltaTime;
            if (_debounceTimer >= debounceTime)
            {
                _debouncedShouldShow = rawShouldShow;
                _debounceTimer = 0f;
            }
        }
        else
        {
            _debounceTimer = 0f;
        }

        if (_debouncedShouldShow)
            _currentIntensity = Mathf.Sin(Time.time * pulseSpeed) * 0.5f + 0.5f;
        else
            _currentIntensity = Mathf.MoveTowards(_currentIntensity, 0f, Time.deltaTime / Mathf.Max(fadeOutDuration, 0.01f));
    }

    private float ComputeHandDistance()
    {
        float dLeft = (realLeftHand != null && guideLeftHand != null)
            ? Vector3.Distance(realLeftHand.position, guideLeftHand.position) : Mathf.Infinity;
        float dRight = (realRightHand != null && guideRightHand != null)
            ? Vector3.Distance(realRightHand.position, guideRightHand.position) : Mathf.Infinity;
        return Mathf.Max(dLeft, dRight);
    }

    private void LateUpdate()
    {
        float t = _currentIntensity;
        bool nearlyInvisible = t <= 0.001f;

        bool visible = _debouncedShouldShow || !nearlyInvisible;

        if (guideRenderers == null) return;
        foreach (var r in guideRenderers)
        {
            if (r == null) continue;
            r.enabled = visible;
            if (!visible) continue;

            float baseOpacity = t * maxBaseOpacity;
            float outlineOpacity = t * maxOutlineOpacity;
            float outlineWidth = Mathf.Lerp(minOutlineWidth, maxOutlineWidth, t);

            r.GetPropertyBlock(_mpb);
            _mpb.SetFloat(_opacityId, baseOpacity);
            _mpb.SetColor(_outlineColorId, outlineColor);
            _mpb.SetFloat(_outlineWidthId, outlineWidth);
            _mpb.SetFloat(_outlineOpacityId, outlineOpacity);
            r.SetPropertyBlock(_mpb);
        }
    }
}