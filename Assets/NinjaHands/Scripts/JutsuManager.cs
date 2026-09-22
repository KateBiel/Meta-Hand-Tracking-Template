using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Oculus.Interaction;

/// <summary>
/// One sign in a jutsu sequence: the ActiveStateGroup that detects it,
/// the guide visual that shows its ghost hands, and the sound played on success.
/// </summary>
[Serializable]
public class JutsuSignStep
{
    [Tooltip("ActiveStateGroup ANDing the L and R ShapeRecognizerActiveState for this sign.")]
    public ActiveStateGroup activeState;

    [Tooltip("The HandPoseGuideVisual (Drive Externally = true) that shows this sign's ghost hands.")]
    public HandPoseGuideVisual guideVisual;

    [Tooltip("One is picked at random and played when this sign is completed correctly. Leave slots empty if you don't want variety for this step.")]
    public AudioClip[] completionSounds;
}

/// <summary>
/// One jutsu = an ordered list of hand signs.
/// Each sign bundles its detection condition, its guide visual, and its sound.
/// </summary>
[Serializable]
public class JutsuDefinition
{
    public string jutsuName;

    [Tooltip("Ordered signs. Each entry bundles the sign's ActiveStateGroup, its guide visual, and its completion sound.")]
    public List<JutsuSignStep> signs = new List<JutsuSignStep>();

    [Tooltip("Seconds a sign must be held continuously to register.")]
    public float holdTime = 0.2f;

    [Tooltip("Max seconds allowed between signs before the sequence resets. 0 = no timeout.")]
    public float stepTimeout = 4f;

    [Tooltip("Chakra consumed when this jutsu completes. Detection won't start if the player can't afford it.")]
    public float chakraCost = 20f;
}

public class JutsuManager : MonoBehaviour
{
    [SerializeField] private List<JutsuDefinition> jutsus = new List<JutsuDefinition>();

    [Tooltip("If true, detection only runs after BeginDetection() is called (button mode). If false, detection is always on (free mode, current jutsu only).")]
    [SerializeField] private bool requireActivation = true;

    [Tooltip("Button mode only: total seconds allowed from BeginDetection() to finish the whole jutsu. Fires OnSequenceReset and stops detecting when exceeded, even if no sign was completed. 0 = disabled.")]
    [SerializeField] private float detectionTimeout = 15f;

    [Header("Chakra")]
    [Tooltip("Optional. If assigned, jutsus with chakraCost > 0 are gated and consume chakra on completion.")]
    [SerializeField] private ChakraSystem chakra;

    [Tooltip("If true, chakra is taken at BeginDetection (commit up front). If false, it's taken only when the jutsu completes.")]
    [SerializeField] private bool spendOnStart = false;

    [Header("Events")]
    public UnityEvent<string> OnDetectionStarted;
    public UnityEvent<string, int> OnSignCompleted;   // jutsu name, sign index
    public UnityEvent<string> OnJutsuCompleted;
    public UnityEvent<string> OnSequenceReset;
    [Tooltip("jutsu name, cost, current chakra")]
    public UnityEvent<string, float, float> OnNotEnoughChakra;

    private int _currentJutsuIndex = 0;
    private bool _detecting = false;
    private int _stepIndex = 0;
    private float _holdTimer = 0f;
    private float _timeoutTimer = 0f;
    private float _detectionElapsed = 0f;
    private bool _waitingForRelease = false;

    public JutsuDefinition CurrentJutsu =>
        (jutsus.Count > 0) ? jutsus[Mathf.Clamp(_currentJutsuIndex, 0, jutsus.Count - 1)] : null;

    public int CurrentStepIndex => _stepIndex;
    public bool IsDetecting => _detecting;

    private void Start()
    {
        if (!requireActivation)
        {
            BeginDetection();
        }
    }

    // ---- Hook these to your table buttons (PointableUnityEventWrapper etc.) ----

    /// <summary>Select a jutsu by index (one button per jutsu).</summary>
    public void SelectJutsu(int index)
    {
        if (jutsus.Count == 0) return;
        _currentJutsuIndex = Mathf.Clamp(index, 0, jutsus.Count - 1);
        ResetSequence(fireEvent: false);
    }

    /// <summary>Cycle to the next jutsu (single "Next" button).</summary>
    public void SelectNextJutsu()
    {
        if (jutsus.Count == 0) return;
        _currentJutsuIndex = (_currentJutsuIndex + 1) % jutsus.Count;
        ResetSequence(fireEvent: false);
    }

    /// <summary>Start listening for the current jutsu's sign sequence.</summary>
    public void BeginDetection()
    {
        if (CurrentJutsu == null || CurrentJutsu.signs.Count == 0) return;

        JutsuDefinition jutsu = CurrentJutsu;

        // Chakra gate: refuse to start if the player can't pay for it.
        if (chakra != null && jutsu.chakraCost > 0f && !chakra.CanAfford(jutsu.chakraCost))
        {
            OnNotEnoughChakra?.Invoke(jutsu.jutsuName, jutsu.chakraCost, chakra.Current);
            return;
        }

        if (spendOnStart && chakra != null)
        {
            chakra.Spend(jutsu.chakraCost);
        }

        ResetSequence(fireEvent: false);
        _detectionElapsed = 0f;
        _detecting = true;
        OnDetectionStarted?.Invoke(jutsu.jutsuName);
    }

    /// <summary>True if the current jutsu can be started right now (chakra-wise).</summary>
    public bool CanCastCurrent()
    {
        if (CurrentJutsu == null) return false;
        if (chakra == null || CurrentJutsu.chakraCost <= 0f) return true;
        return chakra.CanAfford(CurrentJutsu.chakraCost);
    }

    public void StopDetection()
    {
        _detecting = false;
        ResetSequence(fireEvent: false);
    }

    // ---------------------------------------------------------------------------

    private void Update()
    {
        if (!_detecting || CurrentJutsu == null || CurrentJutsu.signs.Count == 0)
            return;

        JutsuDefinition jutsu = CurrentJutsu;

        // Overall timeout: counts from BeginDetection regardless of progress.
        if (requireActivation && detectionTimeout > 0f)
        {
            _detectionElapsed += Time.deltaTime;
            if (_detectionElapsed >= detectionTimeout)
            {
                OnSequenceReset?.Invoke(jutsu.jutsuName);
                StopDetection();
                return;
            }
        }

        ActiveStateGroup currentSign = jutsu.signs[_stepIndex].activeState;
        if (currentSign == null) return;

        bool signActive = currentSign.Active;

        // If the previous completed sign is the same object as the current one,
        // require the hands to leave the pose once before it can count again.
        if (_waitingForRelease)
        {
            if (!signActive)
            {
                _waitingForRelease = false;
            }
            return;
        }

        if (signActive)
        {
            _holdTimer += Time.deltaTime;
            _timeoutTimer = 0f;

            if (_holdTimer >= jutsu.holdTime)
            {
                CompleteStep(jutsu);
            }
        }
        else
        {
            _holdTimer = 0f;

            // Timeout only counts once the sequence has started.
            if (_stepIndex > 0 && jutsu.stepTimeout > 0f)
            {
                _timeoutTimer += Time.deltaTime;
                if (_timeoutTimer >= jutsu.stepTimeout)
                {
                    ResetSequence(fireEvent: true);
                }
            }
        }
    }

    private void CompleteStep(JutsuDefinition jutsu)
    {
        OnSignCompleted?.Invoke(jutsu.jutsuName, _stepIndex);

        int nextIndex = _stepIndex + 1;

        if (nextIndex >= jutsu.signs.Count)
        {
            // Pay on completion. If chakra ran out mid-sequence (e.g. drained by
            // something else), treat it as a fizzle rather than a free cast.
            if (!spendOnStart && chakra != null && jutsu.chakraCost > 0f)
            {
                if (!chakra.Spend(jutsu.chakraCost))
                {
                    OnNotEnoughChakra?.Invoke(jutsu.jutsuName, jutsu.chakraCost, chakra.Current);
                    ResetSequence(fireEvent: true);
                    if (requireActivation) _detecting = false;
                    return;
                }
            }

            OnJutsuCompleted?.Invoke(jutsu.jutsuName);
            ResetSequence(fireEvent: false);
            if (requireActivation)
            {
                _detecting = false; // one-shot in button mode; press again to retry
            }
            return;
        }

        // If the next sign uses the same ActiveStateGroup, force a release first
        // so holding one pose can't complete two identical consecutive steps.
        _waitingForRelease = jutsu.signs[nextIndex].activeState == jutsu.signs[_stepIndex].activeState;

        _stepIndex = nextIndex;
        _holdTimer = 0f;
        _timeoutTimer = 0f;
    }

    private void ResetSequence(bool fireEvent)
    {
        if (fireEvent && _stepIndex > 0 && CurrentJutsu != null)
        {
            OnSequenceReset?.Invoke(CurrentJutsu.jutsuName);
        }
        _stepIndex = 0;
        _holdTimer = 0f;
        _timeoutTimer = 0f;
        _waitingForRelease = false;
    }
}