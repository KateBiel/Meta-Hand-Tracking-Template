using System.Collections;
using UnityEngine;
using Oculus.Interaction;

public class ChakraHandGlow : MonoBehaviour
{
    [SerializeField] private ChakraSystem chakra;
    [SerializeField] private JutsuManager jutsuManager;
    [Tooltip("Used to gate the empty-chakra warning to specific levels only.")]
    [SerializeField] private LevelManager levelManager;

    [Header("Hands")]
    [SerializeField] private SkinnedMeshRenderer leftHandRenderer;
    [SerializeField] private SkinnedMeshRenderer rightHandRenderer;
    [Tooltip("ChakraChargeL — the left hand's own ActiveStateGroup for the charge sign.")]
    [SerializeField] private ActiveStateGroup leftChargeSign;
    [Tooltip("ChakraChargeR — the right hand's own ActiveStateGroup for the charge sign.")]
    [SerializeField] private ActiveStateGroup rightChargeSign;

    [Header("Shader properties (Meta OculusHand)")]
    [SerializeField] private string outlineColorProperty = "_OutlineColor";
    [SerializeField] private string outlineWidthProperty = "_OutlineWidth";

    [Header("Colors (HDR)")]
    [ColorUsage(true, true)][SerializeField] private Color idleColor = new Color(0f, 0f, 0f, 0f);
    [Tooltip("Shown on a hand that holds the sign while the other doesn't.")]
    [ColorUsage(true, true)][SerializeField] private Color singleHandColor = new Color(0.6f, 0.9f, 1f);
    [ColorUsage(true, true)][SerializeField] private Color fullColor = new Color(0f, 2f, 3f);
    [ColorUsage(true, true)][SerializeField] private Color emptyColor = new Color(0.1f, 0.3f, 0.4f);
    [ColorUsage(true, true)][SerializeField] private Color castFlashColor = new Color(2f, 4f, 6f);
    [ColorUsage(true, true)][SerializeField] private Color deniedColor = new Color(4f, 0.2f, 0.2f);

    [Header("Sign Success Flash")]
    [Tooltip("Flashed briefly on both hands whenever a single sign in a jutsu sequence is completed correctly.")]
    [ColorUsage(true, true)][SerializeField] private Color signSuccessColor = new Color(0f, 3f, 0.5f);
    [SerializeField] private float signSuccessFlashDuration = 0.4f;

    [Header("Empty Chakra Warning (gated to specific levels)")]
    [Tooltip("Warning shows whenever chakra is at or below this value (not just exactly 0).")]
    [SerializeField] private float emptyWarningThreshold = 20f;
    [Tooltip("Which LevelManager.ActiveSetIndex values this warning is allowed to show in — e.g. 2 = Level3, 3 = Level4, matching the order of targetSets in LevelManager.")]
    [SerializeField] private int[] emptyWarningLevelIndices = { 2, 3 };
    [ColorUsage(true, true)][SerializeField] private Color emptyWarningColor = new Color(4f, 0f, 0f);
    [SerializeField] private float emptyWarningWidth = 0.0015f;
    [SerializeField] private float emptyWarningPulseSpeed = 6f;
    [Range(0f, 1f)][SerializeField] private float emptyWarningPulseAmount = 0.6f;

    [Header("Width")]
    [SerializeField] private float widthIdle = 0.0f;
    [SerializeField] private float widthSingleHand = 0.0012f;
    [SerializeField] private float widthAtEmpty = 0.0008f;
    [SerializeField] private float widthAtFull = 0.0025f;

    [Header("Transitions")]
    [Tooltip("Seconds to fade between idle / single / charging states.")]
    [SerializeField] private float fadeTime = 0.2f;

    [Header("Low-chakra flicker (while charging)")]
    [Range(0f, 1f)][SerializeField] private float flickerBelow = 0.25f;
    [SerializeField] private float flickerSpeed = 12f;
    [Range(0f, 1f)][SerializeField] private float flickerAmount = 0.5f;

    [Header("Flash timing")]
    [SerializeField] private float castFlashDuration = 0.6f;
    [SerializeField] private float deniedFlashDuration = 0.35f;

    private MaterialPropertyBlock _mpb;
    private int _colorId, _widthId;
    private float _fill;

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
            jutsuManager.OnSignCompleted.AddListener(HandleSignCompleted);
        }
    }

    private void OnDisable()
    {
        if (chakra != null) chakra.OnChakraChanged.RemoveListener(HandleChakraChanged);
        if (jutsuManager != null)
        {
            jutsuManager.OnJutsuCompleted.RemoveListener(HandleCast);
            jutsuManager.OnNotEnoughChakra.RemoveListener(HandleDenied);
            jutsuManager.OnSignCompleted.RemoveListener(HandleSignCompleted);
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

        Color chargeColor = Color.Lerp(emptyColor, fullColor, _fill);
        float chargeWidth = Mathf.Lerp(widthAtEmpty, widthAtFull, _fill);
        if (_fill < flickerBelow)
        {
            float n = Mathf.PerlinNoise(Time.time * flickerSpeed, 0.37f);
            chargeColor *= 1f - flickerAmount * n;
        }

        Color idleColorNow = idleColor;
        float idleWidthNow = widthIdle;
        if (ShouldShowEmptyWarning())
        {
            float pulse = Mathf.Sin(Time.time * emptyWarningPulseSpeed) * 0.5f + 0.5f;
            float k = 1f - emptyWarningPulseAmount * (1f - pulse);
            idleColorNow = emptyWarningColor * k;
            idleWidthNow = emptyWarningWidth * k;
        }

        Evaluate(_leftState, chargeColor, chargeWidth, idleColorNow, idleWidthNow, out _leftColor, out _leftWidth);
        Evaluate(_rightState, chargeColor, chargeWidth, idleColorNow, idleWidthNow, out _rightColor, out _rightWidth);
    }

    private bool ShouldShowEmptyWarning()
    {
        if (chakra == null || chakra.Current > emptyWarningThreshold) return false;

        if (levelManager == null || emptyWarningLevelIndices == null || emptyWarningLevelIndices.Length == 0)
            return true; // no gating configured — show everywhere

        int active = levelManager.ActiveSetIndex;
        foreach (int idx in emptyWarningLevelIndices)
        {
            if (idx == active) return true;
        }
        return false;
    }

    private void Evaluate(float state, Color chargeColor, float chargeWidth, Color idleColorNow, float idleWidthNow, out Color color, out float width)
    {
        if (state <= 1f)
        {
            color = Color.Lerp(idleColorNow, singleHandColor, state);
            width = Mathf.Lerp(idleWidthNow, widthSingleHand, state);
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
    private void HandleSignCompleted(string jutsuName, int stepIndex) => StartFlash(signSuccessColor, signSuccessFlashDuration);

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