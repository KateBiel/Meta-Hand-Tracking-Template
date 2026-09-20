using UnityEngine;

public class HandPoseGuideVisual : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ChakraSystem chakra;
    [SerializeField] private Transform realLeftHand;
    [SerializeField] private Transform realRightHand;
    [SerializeField] private Transform guideLeftHand;
    [SerializeField] private Transform guideRightHand;
    [SerializeField] private SkinnedMeshRenderer[] guideRenderers;

    [Header("Trigger Conditions")]
    [SerializeField] private float chakraEmptyThreshold = 5f;
    [SerializeField] private float handDistanceThreshold = 0.2f;
    [Tooltip("Seconds handDistance must stay on one side of the threshold before the show/hide state actually flips. Prevents tracking jitter from snapping the fade back to bright.")]
    [SerializeField] private float debounceTime = 0.15f;

    private bool _debouncedShouldShow;
    private float _debounceTimer;

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
    private float _chakraEmptyTimer;
    private float _currentIntensity; // 0..1, only meaningful while actively pulsing
    private bool _wasShowing;

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

    private void Update()
    {
        if (chakra != null)
        {
            if (chakra.Current <= 0f) _chakraEmptyTimer += Time.deltaTime;
            else _chakraEmptyTimer = 0f;
        }

        float handDistance = ComputeHandDistance();
        bool rawShouldShow = _chakraEmptyTimer >= chakraEmptyThreshold && handDistance > handDistanceThreshold;

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

        // Stay enabled the whole time we're actively pulsing (so the earlier flicker
        // fix still holds), and stay enabled mid-fade-out too — only fully disable
        // once we're both hidden AND the fade has actually settled at 0.
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