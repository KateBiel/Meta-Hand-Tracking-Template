using System.Collections;
using UnityEngine;
using Oculus.Interaction;

/// <summary>
/// Drives the Meta hand material's outline per hand:
///   - idle: no outline (idleColor)
///   - one hand holding the charge sign: that hand shows singleHandColor
///   - both hands (ChakraSystem.IsCharging): both show the charging color, scaled by chakra level
///   - cast / denied flashes on top
/// Writes happen in LateUpdate so they win against Meta's MaterialPropertyBlockEditor.
/// </summary>
public class ChakraHandGlow : MonoBehaviour
{
    [SerializeField] private ChakraSystem chakra;
    [SerializeField] private JutsuManager jutsuManager;

    [Header("Hands")]
    [SerializeField] private SkinnedMeshRenderer leftHandRenderer;   // LeftHand
    [SerializeField] private SkinnedMeshRenderer rightHandRenderer;  // RightHand
    [Tooltip("ChakraChargeL — the left hand's own ActiveStateGroup for the charge sign.")]
    [SerializeField] private ActiveStateGroup leftChargeSign;
    [Tooltip("ChakraChargeR — the right hand's own ActiveStateGroup for the charge sign.")]
    [SerializeField] private ActiveStateGroup rightChargeSign;

    [Header("Shader properties (Meta OculusHand)")]
    [SerializeField] private string outlineColorProperty = "_OutlineColor";
    [SerializeField] private string outlineWidthProperty = "_OutlineWidth";

    [Header("Colors (HDR)")]
    [ColorUsage(true, true)] [SerializeField] private Color idleColor = new Color(0f, 0f, 0f, 0f);
    [Tooltip("Shown on a hand that holds the sign while the other doesn't.")]
    [ColorUsage(true, true)] [SerializeField] private Color singleHandColor = new Color(0.6f, 0.9f, 1f);
    [ColorUsage(true, true)] [SerializeField] private Color fullColor = new Color(0f, 2f, 3f);
    [ColorUsage(true, true)] [SerializeField] private Color emptyColor = new Color(0.1f, 0.3f, 0.4f);
    [ColorUsage(true, true)] [SerializeField] private Color castFlashColor = new Color(2f, 4f, 6f);
    [ColorUsage(true, true)] [SerializeField] private Color deniedColor = new Color(4f, 0.2f, 0.2f);

    [Header("Width")]
    [SerializeField] private float widthIdle = 0.0f;
    [SerializeField] private float widthSingleHand = 0.0012f;
    [SerializeField] private float widthAtEmpty = 0.0008f;
    [SerializeField] private float widthAtFull = 0.0025f;

    [Header("Transitions")]
    [Tooltip("Seconds to fade between idle / single / charging states.")]
    [SerializeField] private float fadeTime = 0.2f;

    [Header("Low-chakra flicker (while charging)")]
    [Range(0f, 1f)] [SerializeField] private float flickerBelow = 0.25f;
    [SerializeField] private float flickerSpeed = 12f;
    [Range(0f, 1f)] [SerializeField] private float flickerAmount = 0.5f;

    [Header("Flash timing")]
    [SerializeField] private float castFlashDuration = 0.6f;
    [SerializeField] private float deniedFlashDuration = 0.35f;

    private MaterialPropertyBlock _mpb;
    private int _colorId, _widthId;
    private float _fill;

    // per-hand smoothed state: 0 = idle, 1 = single-hand, 2 = charging
    private float _leftState, _rightState;
    private Color _leftColor, _rightColor;
    private float _leftWidth, _rightWidth;

    private Coroutine _flash;
    private bool _flashing;
    private Color _flashColor;
    private float _flashWidth;

    private void Awake()
    {
        _mpb = new MaterialPropertyBlock();
        _colorId = Shader.PropertyToID(outlineColorProperty);
        _widthId = Shader.PropertyToID(outlineWidthProperty);
    }

    private void OnEnable()
    {
        if (chakra != null)
        {
            chakra.OnChakraChanged.AddListener(HandleChakraChanged);
            _fill = chakra.Normalized;
        }
        if (jutsuManager != null)
        {
            jutsuManager.OnJutsuCompleted.AddListener(HandleCast);
            jutsuManager.OnNotEnoughChakra.AddListener(HandleDenied);
        }
    }

    private void OnDisable()
    {
        if (chakra != null) chakra.OnChakraChanged.RemoveListener(HandleChakraChanged);
        if (jutsuManager != null)
        {
            jutsuManager.OnJutsuCompleted.RemoveListener(HandleCast);
            jutsuManager.OnNotEnoughChakra.RemoveListener(HandleDenied);
        }
    }

    private void Update()
    {
        bool bothCharging = chakra != null && chakra.IsCharging;
        bool leftSign = leftChargeSign != null && leftChargeSign.Active;
        bool rightSign = rightChargeSign != null && rightChargeSign.Active;

        float leftTarget = bothCharging ? 2f : (leftSign ? 1f : 0f);
        float rightTarget = bothCharging ? 2f : (rightSign ? 1f : 0f);

        float step = fadeTime > 0f ? Time.deltaTime / fadeTime : 2f;
        _leftState = Mathf.MoveTowards(_leftState, leftTarget, step);
        _rightState = Mathf.MoveTowards(_rightState, rightTarget, step);

        // Charging color for this frame (shared by both hands)
        Color chargeColor = Color.Lerp(emptyColor, fullColor, _fill);
        float chargeWidth = Mathf.Lerp(widthAtEmpty, widthAtFull, _fill);
        if (_fill < flickerBelow)
        {
            float n = Mathf.PerlinNoise(Time.time * flickerSpeed, 0.37f);
            chargeColor *= 1f - flickerAmount * n;
        }

        Evaluate(_leftState, chargeColor, chargeWidth, out _leftColor, out _leftWidth);
        Evaluate(_rightState, chargeColor, chargeWidth, out _rightColor, out _rightWidth);
    }

    // state 0..1 blends idle->single, 1..2 blends single->charging
    private void Evaluate(float state, Color chargeColor, float chargeWidth, out Color color, out float width)
    {
        if (state <= 1f)
        {
            color = Color.Lerp(idleColor, singleHandColor, state);
            width = Mathf.Lerp(widthIdle, widthSingleHand, state);
        }
        else
        {
            float t = state - 1f;
            color = Color.Lerp(singleHandColor, chargeColor, t);
            width = Mathf.Lerp(widthSingleHand, chargeWidth, t);
        }
    }

    private void LateUpdate()
    {
        if (_flashing)
        {
            Apply(leftHandRenderer, _flashColor, _flashWidth);
            Apply(rightHandRenderer, _flashColor, _flashWidth);
        }
        else
        {
            Apply(leftHandRenderer, _leftColor, _leftWidth);
            Apply(rightHandRenderer, _rightColor, _rightWidth);
        }
    }

    private void HandleChakraChanged(float current, float max) => _fill = max > 0f ? current / max : 0f;
    private void HandleCast(string jutsuName) => StartFlash(castFlashColor, castFlashDuration);
    private void HandleDenied(string jutsuName, float cost, float current) => StartFlash(deniedColor, deniedFlashDuration);

    private void StartFlash(Color color, float duration)
    {
        if (_flash != null) StopCoroutine(_flash);
        _flash = StartCoroutine(FlashRoutine(color, duration));
    }

    private IEnumerator FlashRoutine(Color color, float duration)
    {
        _flashing = true;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = 1f - (t / duration);
            k *= k;
            // fade back toward whatever the hands would otherwise show (use left as reference)
            _flashColor = Color.Lerp(_leftColor, color, k);
            _flashWidth = Mathf.Lerp(_leftWidth, widthAtFull, k);
            yield return null;
        }
        _flashing = false;
        _flash = null;
    }

    private void Apply(SkinnedMeshRenderer r, Color color, float width)
    {
        if (r == null) return;
        r.GetPropertyBlock(_mpb);
        _mpb.SetColor(_colorId, color);
        _mpb.SetFloat(_widthId, width);
        r.SetPropertyBlock(_mpb);
    }
}