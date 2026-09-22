using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using Oculus.Interaction;
using Oculus.Interaction.PoseDetection;
using Oculus.Interaction.Input;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class HandPoseCaptureTool : MonoBehaviour
{
    [Header("Capture Toggles")]
    [Tooltip("Uncheck to skip this hand entirely during capture — no logging, no auto-write, no prefab copy. Use this when you're reusing an already-captured pose for that hand and only want to (re)capture the other one.")]
    [SerializeField] private bool captureLeftHand = true;
    [SerializeField] private bool captureRightHand = true;

    [Header("Left Hand")]
    [SerializeField] private Transform leftHandRoot;
    [SerializeField, Interface(typeof(IFingerFeatureStateProvider))] private UnityEngine.Object _leftFingerProvider;
    [SerializeField, Interface(typeof(ITransformFeatureStateProvider))] private UnityEngine.Object _leftTransformProvider;
    [SerializeField] private TransformRecognizerActiveState leftTransformConfigSource;

    [Header("Right Hand")]
    [SerializeField] private Transform rightHandRoot;
    [SerializeField, Interface(typeof(IFingerFeatureStateProvider))] private UnityEngine.Object _rightFingerProvider;
    [SerializeField, Interface(typeof(ITransformFeatureStateProvider))] private UnityEngine.Object _rightTransformProvider;
    [SerializeField] private TransformRecognizerActiveState rightTransformConfigSource;

    [Header("Auto-write targets (optional — leave empty to skip auto-writing that asset)")]
    [Tooltip("The ShapeRecognizer asset to overwrite with captured finger states, e.g. ChakraChargeLHandShape.")]
    [SerializeField] private ShapeRecognizer leftShapeRecognizer;
    [SerializeField] private ShapeRecognizer rightShapeRecognizer;

    [Header("Capture Settings")]
    [SerializeField] private int countdownSeconds = 3;
    [SerializeField] private string savedPrefabFolder = "Assets/CapturedPoses";
    [SerializeField] private string poseName = "CapturedPose";

    [Header("Events — wire to your DebugArea/Logger, same as your other table buttons")]
    public UnityEvent<string> OnCountdownTick;   // fires "3", "2", "1", "Capturing..."
    public UnityEvent OnCaptured;

    private IFingerFeatureStateProvider LeftFingerProvider, RightFingerProvider;
    private ITransformFeatureStateProvider LeftTransformProvider, RightTransformProvider;
    private bool _capturing;

    private static readonly HandFinger[] FINGERS = { HandFinger.Thumb, HandFinger.Index, HandFinger.Middle, HandFinger.Ring, HandFinger.Pinky };
    private static readonly FingerFeature[] FEATURES = { FingerFeature.Curl, FingerFeature.Flexion, FingerFeature.Abduction, FingerFeature.Opposition };
    private static readonly TransformFeature[] XFORM_FEATURES = {
        TransformFeature.WristUp, TransformFeature.WristDown, TransformFeature.PalmDown, TransformFeature.PalmUp,
        TransformFeature.PalmTowardsFace, TransformFeature.PalmAwayFromFace, TransformFeature.FingersUp,
        TransformFeature.FingersDown, TransformFeature.PinchClear
    };

    private void Awake()
    {
        LeftFingerProvider = _leftFingerProvider as IFingerFeatureStateProvider;
        RightFingerProvider = _rightFingerProvider as IFingerFeatureStateProvider;
        LeftTransformProvider = _leftTransformProvider as ITransformFeatureStateProvider;
        RightTransformProvider = _rightTransformProvider as ITransformFeatureStateProvider;
    }

    /// <summary>Hook this to your table button's UnityEvent, same as Table Button Fire Jutsu etc.</summary>
    public void BeginCapture()
    {
        if (_capturing) return;
        StartCoroutine(CaptureRoutine());
    }

    private IEnumerator CaptureRoutine()
    {
        _capturing = true;

        for (int i = countdownSeconds; i > 0; i--)
        {
            OnCountdownTick?.Invoke(i.ToString());
            yield return new WaitForSeconds(1f);
        }
        OnCountdownTick?.Invoke("Capturing...");

        LogPoseStates();

#if UNITY_EDITOR
        if (captureLeftHand)
        {
            AutoWriteShapeRecognizer("L", LeftFingerProvider, leftShapeRecognizer);
            AutoWriteTransformConfig(LeftTransformProvider, leftTransformConfigSource);
        }
        if (captureRightHand)
        {
            AutoWriteShapeRecognizer("R", RightFingerProvider, rightShapeRecognizer);
            AutoWriteTransformConfig(RightTransformProvider, rightTransformConfigSource);
        }
#endif

        SpawnHandCopies();

        OnCaptured?.Invoke();
        _capturing = false;
    }

    private void LogPoseStates()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"=== Captured Pose: {poseName} ===");
        if (captureLeftHand)
            AppendHandStates(sb, "L", LeftFingerProvider, LeftTransformProvider, leftTransformConfigSource);
        if (captureRightHand)
            AppendHandStates(sb, "R", RightFingerProvider, RightTransformProvider, rightTransformConfigSource);
        Debug.Log(sb.ToString());
    }

    private void AppendHandStates(StringBuilder sb, string side, IFingerFeatureStateProvider fingerProvider,
        ITransformFeatureStateProvider transformProvider, TransformRecognizerActiveState configSource)
    {
        if (fingerProvider == null || transformProvider == null || configSource == null) return;

        sb.AppendLine($"-- {side} Hand --");
        foreach (var finger in FINGERS)
        {
            foreach (var feature in FEATURES)
            {
                if (finger == HandFinger.Thumb && feature == FingerFeature.Opposition) continue;
                if (finger == HandFinger.Pinky && feature == FingerFeature.Abduction) continue;
                if (fingerProvider.GetCurrentState(finger, feature, out string state))
                    sb.AppendLine($"{finger} / {feature}: {state}");
            }
        }
        var config = configSource.TransformConfig;
        foreach (var feat in XFORM_FEATURES)
        {
            if (transformProvider.GetCurrentState(config, feat, out string state))
                sb.AppendLine($"Transform / {feat}: {state}");
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// Overwrites the target ShapeRecognizer asset's per-finger configs with the
    /// hand's currently-captured states, using FeatureStateActiveMode.Is (i.e. "this
    /// finger must BE in exactly this state"). Flip individual entries to "Is Not"
    /// manually afterward if you want a looser rule.
    /// </summary>
    private void AutoWriteShapeRecognizer(string side, IFingerFeatureStateProvider fingerProvider, ShapeRecognizer target)
    {
        if (fingerProvider == null || target == null) return;

        var perFinger = new Dictionary<HandFinger, List<ShapeRecognizer.FingerFeatureConfig>>();
        foreach (var finger in FINGERS) perFinger[finger] = new List<ShapeRecognizer.FingerFeatureConfig>();

        foreach (var finger in FINGERS)
        {
            foreach (var feature in FEATURES)
            {
                if (finger == HandFinger.Thumb && feature == FingerFeature.Opposition) continue;
                if (finger == HandFinger.Pinky && feature == FingerFeature.Abduction) continue;

                if (fingerProvider.GetCurrentState(finger, feature, out string state))
                {
                    perFinger[finger].Add(new ShapeRecognizer.FingerFeatureConfig
                    {
                        Feature = feature,
                        Mode = FeatureStateActiveMode.Is,
                        State = state
                    });
                }
            }
        }

        target.InjectShapeName(poseName + "_" + side);
        target.InjectThumbFeatureConfigs(perFinger[HandFinger.Thumb].ToArray());
        target.InjectIndexFeatureConfigs(perFinger[HandFinger.Index].ToArray());
        target.InjectMiddleFeatureConfigs(perFinger[HandFinger.Middle].ToArray());
        target.InjectRingFeatureConfigs(perFinger[HandFinger.Ring].ToArray());
        target.InjectPinkyFeatureConfigs(perFinger[HandFinger.Pinky].ToArray());

        EditorUtility.SetDirty(target);
        AssetDatabase.SaveAssets();
        Debug.Log($"Wrote captured pose into ShapeRecognizer asset: {target.name}");
    }

    /// <summary>
    /// Overwrites the target TransformRecognizerActiveState's feature config list with
    /// the hand's currently-captured transform states (WristUp, PalmDown, etc.), again
    /// using FeatureStateActiveMode.Is by default.
    /// </summary>
    private void AutoWriteTransformConfig(ITransformFeatureStateProvider transformProvider, TransformRecognizerActiveState target)
    {
        if (transformProvider == null || target == null) return;

        var config = target.TransformConfig;
        var configs = new List<TransformFeatureConfig>();

        foreach (var feat in XFORM_FEATURES)
        {
            if (transformProvider.GetCurrentState(config, feat, out string state))
            {
                configs.Add(new TransformFeatureConfig
                {
                    Feature = feat,
                    Mode = FeatureStateActiveMode.Is,
                    State = state
                });
            }
        }

        target.InjectTransformFeatureList(TransformFeatureConfigList.Create(configs));

        EditorUtility.SetDirty(target);
        Debug.Log($"Wrote captured pose into TransformRecognizerActiveState: {target.name}. " +
                   "If this is a prefab instance, remember to Apply the override, or it stays a scene-only change.");
    }
#endif

    private void SpawnHandCopies()
    {
        if (captureLeftHand)
            SpawnHandCopy(leftHandRoot, poseName + "_L");
        if (captureRightHand)
            SpawnHandCopy(rightHandRoot, poseName + "_R");
    }

    private void SpawnHandCopy(Transform source, string name)
    {
        if (source == null) return;

        GameObject copy = Instantiate(source.gameObject);
        copy.name = name;
        copy.transform.SetPositionAndRotation(source.position, source.rotation);
        copy.transform.localScale = source.lossyScale;

#if UNITY_EDITOR
        if (!System.IO.Directory.Exists(savedPrefabFolder))
        {
            System.IO.Directory.CreateDirectory(savedPrefabFolder);
            AssetDatabase.Refresh();
        }
        string path = $"{savedPrefabFolder}/{name}.prefab";
        PrefabUtility.SaveAsPrefabAsset(copy, path);
        Debug.Log($"Saved prefab: {path}");
#else
        Debug.LogWarning("Saving as a .prefab asset only works in the Editor. In a build, this stays a scene object only.");
#endif
    }
}