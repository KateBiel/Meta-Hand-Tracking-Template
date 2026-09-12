using System.Collections;
using UnityEngine;

/// <summary>
/// Drives the Meta hand material's outline from chakra level.
/// Put one on each hand (or one with both renderers assigned).
/// Uses MaterialPropertyBlock so the shared hand material asset is never edited.
///
/// Writes happen in LateUpdate (not Update) so this always wins against other
/// scripts that write their own property block during Update() - e.g. Meta's
/// MaterialPropertyBlockEditor with "Update Every Frame" checked, which
/// otherwise silently overwrites these values later in the same frame.
/// </summary>
public class ChakraHandGlow : MonoBehaviour
{
    [SerializeField] private ChakraSystem chakra;
    [SerializeField] private JutsuManager jutsuManager;

    [Tooltip("The SkinnedMeshRenderers of the hand visuals (l_handMeshNode / r_handMeshNode).")]
    [SerializeField] private SkinnedMeshRenderer[] handRenderers;

    [Header("Shader properties (Meta OculusHand)")]
    [SerializeField] private string outlineColorProperty = "_OutlineColor";
    [SerializeField] private string outlineWidthProperty = "_OutlineWidth";

    [Header("Visibility")]
    [Tooltip("If true, the outline only shows while charging (and briefly on cast/denied flashes). Otherwise it always reflects chakra level.")]
    [SerializeField] private bool showOnlyWhileCharging = true;

    [Tooltip("Seconds to fade the outline in/out when charging starts/stops.")]
    [SerializeField] private float chargeFadeTime = 0.25f;

    [Header("Colors (HDR)")]
    [ColorUsage(true, true)] [SerializeField] private Color idleColor = new Color(0f, 0f, 0f, 0f);
    [ColorUsage(true, true)] [SerializeField] private Color fullColor = new Color(0f, 2f, 3f);
    [ColorUsage(true, true)] [SerializeField] private Color emptyColor = new Color(0.1f, 0.3f, 0.4f);
    [ColorUsage(true, true)] [SerializeField] private Color castFlashColor = new Color(2f, 4f, 6f);
    [ColorUsage(true, true)] [SerializeField] private Color deniedColor = new Color(4f, 0.2f, 0.2f);

    [Header("Width")]
    [SerializeField] private float widthAtEmpty = 0.0008f;
    [SerializeField] private float widthAtFull = 0.0025f;

    [Header("Low-chakra flicker")]
    [Range(0f, 1f)] [SerializeField] private float flickerBelow = 0.25f;
    [SerializeField] private float flickerSpeed = 12f;
    [Range(0f, 1f)] [SerializeField] private float flickerAmount = 0.5f;

    [Header("Flash timing")]
    [SerializeField] private float castFlashDuration = 0.6f;
    [SerializeField] private float deniedFlashDuration = 0.35f;

    private MaterialPropertyBlock _mpb;
    private int _colorId;
    private int _widthId;
    private float _fill = 1f;
    private Coroutine _flash;

    // Computed each frame in Update()/FlashRoutine(), written to renderers in LateUpdate().
    private bool _flashing;
    private float _chargeBlend;     // 0 = idle, 1 = charging (smoothed)
    private Color _restColor;
    private float _restWidth;
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

    // Computes the resting (non-flash) color/width for this frame. Does NOT write to the renderer.
    private void Update()
    {
        Color c = Color.Lerp(emptyColor, fullColor, _fill);
        float w = Mathf.Lerp(widthAtEmpty, widthAtFull, _fill);

        if (_fill < flickerBelow)
        {
            // dip the intensity with a fast noise so low chakra looks unstable
            float n = Mathf.PerlinNoise(Time.time * flickerSpeed, 0.37f);
            float dip = 1f - flickerAmount * n;
            c *= dip;
        }

        if (showOnlyWhileCharging)
        {
            bool charging = chakra != null && chakra.IsCharging;
            float target = charging ? 1f : 0f;
            float step = chargeFadeTime > 0f ? Time.deltaTime / chargeFadeTime : 1f;
            _chargeBlend = Mathf.MoveTowards(_chargeBlend, target, step);

            c = Color.Lerp(idleColor, c, _chargeBlend);
            w = Mathf.Lerp(widthAtEmpty, w, _chargeBlend);
        }

        _restColor = c;
        _restWidth = w;
    }

    private void LateUpdate()
    {
        if (_flashing)
            Apply(_flashColor, _flashWidth);
        else
            Apply(_restColor, _restWidth);
    }

    private void HandleChakraChanged(float current, float max)
    {
        _fill = max > 0f ? current / max : 0f;
    }

    private void HandleCast(string jutsuName)
    {
        StartFlash(castFlashColor, castFlashDuration);
    }

    private void HandleDenied(string jutsuName, float cost, float current)
    {
        StartFlash(deniedColor, deniedFlashDuration);
    }

    private void StartFlash(Color color, float duration)
    {
        if (_flash != null) StopCoroutine(_flash);
        _flash = StartCoroutine(FlashRoutine(color, duration));
    }

    // Only computes the flash color/width per frame - LateUpdate does the actual write.
    private IEnumerator FlashRoutine(Color color, float duration)
    {
        _flashing = true;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = 1f - (t / duration);      // 1 -> 0
            k = k * k;                           // ease out
            Color rest = Color.Lerp(emptyColor, fullColor, _fill);
            _flashColor = Color.Lerp(rest, color, k);
            _flashWidth = widthAtFull;
            yield return null;
        }
        _flashing = false;
        _flash = null;
    }

    private void Apply(Color color, float width)
    {
        for (int i = 0; i < handRenderers.Length; i++)
        {
            var r = handRenderers[i];
            if (r == null) continue;
            r.GetPropertyBlock(_mpb);
            _mpb.SetColor(_colorId, color);
            _mpb.SetFloat(_widthId, width);
            r.SetPropertyBlock(_mpb);
        }
    }
}