using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays chakra as a fill. Put this on the bar object parented under the wrist bone.
/// Works with either:
///  - a Renderer whose material has a float property (default "_Fill"), or
///  - a UI Image set to Filled.
/// Assign whichever you're using; the other can stay empty.
/// </summary>
public class ChakraBar : MonoBehaviour
{
    [SerializeField] private ChakraSystem chakra;

    [Header("Material mode")]
    [SerializeField] private Renderer barRenderer;
    [SerializeField] private string fillProperty = "_Fill";
    [SerializeField] private string colorProperty = "_Color";

    [Header("UI Image mode")]
    [SerializeField] private Image fillImage;

    [Header("Look")]
    [SerializeField] private Gradient colorByFill;   // e.g. red at 0 -> cyan at 1
    [SerializeField] private bool useGradient = true;

    [Tooltip("Seconds to ease toward the new value. 0 = snap.")]
    [SerializeField] private float smoothTime = 0.15f;

    private MaterialPropertyBlock _mpb;
    private int _fillId;
    private int _colorId;
    private float _target;
    private float _display;
    private float _velocity;

    private void Awake()
    {
        _mpb = new MaterialPropertyBlock();
        _fillId = Shader.PropertyToID(fillProperty);
        _colorId = Shader.PropertyToID(colorProperty);
    }

    private void OnEnable()
    {
        if (chakra != null)
        {
            chakra.OnChakraChanged.AddListener(HandleChanged);
            _target = _display = chakra.Normalized;
            Apply(_display);
        }
    }

    private void OnDisable()
    {
        if (chakra != null)
            chakra.OnChakraChanged.RemoveListener(HandleChanged);
    }

    private void HandleChanged(float current, float max)
    {
        _target = max > 0f ? current / max : 0f;
        if (smoothTime <= 0f)
        {
            _display = _target;
            Apply(_display);
        }
    }

    private void Update()
    {
        if (smoothTime <= 0f || Mathf.Approximately(_display, _target)) return;
        _display = Mathf.SmoothDamp(_display, _target, ref _velocity, smoothTime);
        Apply(_display);
    }

    private void Apply(float fill)
    {
        Color c = useGradient && colorByFill != null ? colorByFill.Evaluate(fill) : Color.white;

        if (barRenderer != null)
        {
            barRenderer.GetPropertyBlock(_mpb);
            _mpb.SetFloat(_fillId, fill);
            if (useGradient) _mpb.SetColor(_colorId, c);
            barRenderer.SetPropertyBlock(_mpb);
        }

        if (fillImage != null)
        {
            fillImage.fillAmount = fill;
            if (useGradient) fillImage.color = c;
        }
    }
}