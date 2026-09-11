using System.Collections;
using UnityEngine;

/// <summary>
/// Drives the Meta hand material's outline from chakra level.
/// Put one on each hand (or one with both renderers assigned).
/// Uses MaterialPropertyBlock so the shared hand material asset is never edited.
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

    [Header("Colors (HDR)")]
    [ColorUsage(true, true)][SerializeField] private Color fullColor = new Color(0f, 2f, 3f);
    [ColorUsage(true, true)][SerializeField] private Color emptyColor = new Color(0.1f, 0.3f, 0.4f);
    [ColorUsage(true, true)][SerializeField] private Color castFlashColor = new Color(2f, 4f, 6f);
    [ColorUsage(true, true)][SerializeField] private Color deniedColor = new Color(4f, 0.2f, 0.2f);

    [Header("Width")]
    [SerializeField] private float widthAtEmpty = 0.0008f;
    [SerializeField] private float widthAtFull = 0.0025f;

    [Header("Low-chakra flicker")]
    [Range(0f, 1f)][SerializeField] private float flickerBelow = 0.25f;
    [SerializeField] private float flickerSpeed = 12f;
    [Range(0f, 1f)][SerializeField] private float flickerAmount = 0.5f;

    [Header("Flash timing")]
    [SerializeField] private float castFlashDuration = 0.6f;
    [SerializeField] private float deniedFlashDuration = 0.35f;

    private MaterialPropertyBlock _mpb;
    private int _colorId;
    private int _widthId;
    private float _fill = 1f;
    private Coroutine _flash;

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
        if (_flash != null) return; // a flash is driving the color right now

        Color c = Color.Lerp(emptyColor, fullColor, _fill);
        float w = Mathf.Lerp(widthAtEmpty, widthAtFull, _fill);

        if (_fill < flickerBelow)
        {
            // dip the intensity with a fast noise so low chakra looks unstable
            float n = Mathf.PerlinNoise(Time.time * flickerSpeed, 0.37f);
            float dip = 1f - flickerAmount * n;
            c *= dip;
        }

        Apply(c, w);
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

    private IEnumerator FlashRoutine(Color color, float duration)
    {
        float t = 0f;
        Color rest = Color.Lerp(emptyColor, fullColor, _fill);
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = 1f - (t / duration);      // 1 -> 0
            k = k * k;                           // ease out
            Apply(Color.Lerp(rest, color, k), widthAtFull);
            yield return null;
        }
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