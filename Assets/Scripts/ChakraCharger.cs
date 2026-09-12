using UnityEngine;
using Oculus.Interaction;

/// <summary>
/// Polls the two-hand charge pose (an ActiveStateGroup) and tells ChakraSystem
/// whether the player is charging. Includes a short hold/release debounce so
/// recognizer flicker doesn't stutter the charge.
/// </summary>
public class ChakraCharger : MonoBehaviour
{
    [SerializeField] private ChakraSystem chakra;

    [Tooltip("Sign_ChakraCharger — the ActiveStateGroup ANDing ChakraChargeL + ChakraChargeR.")]
    [SerializeField] private ActiveStateGroup chargeSign;

    [Tooltip("Seconds the pose must be held before charging starts.")]
    [SerializeField] private float holdToStart = 0.15f;

    [Tooltip("Seconds the pose can drop out before charging stops (hides recognizer flicker).")]
    [SerializeField] private float graceToStop = 0.2f;

    private float _heldTimer;
    private float _lostTimer;
    private bool _charging;

    private void Update()
    {
        if (chakra == null || chargeSign == null) return;

        bool poseActive = chargeSign.Active;

        if (poseActive)
        {
            _lostTimer = 0f;
            if (!_charging)
            {
                _heldTimer += Time.deltaTime;
                if (_heldTimer >= holdToStart)
                {
                    _charging = true;
                    chakra.SetCharging(true);
                }
            }
        }
        else
        {
            _heldTimer = 0f;
            if (_charging)
            {
                _lostTimer += Time.deltaTime;
                if (_lostTimer >= graceToStop)
                {
                    _charging = false;
                    chakra.SetCharging(false);
                }
            }
        }
    }

    private void OnDisable()
    {
        if (_charging && chakra != null)
        {
            _charging = false;
            chakra.SetCharging(false);
        }
        _heldTimer = 0f;
        _lostTimer = 0f;
    }
}