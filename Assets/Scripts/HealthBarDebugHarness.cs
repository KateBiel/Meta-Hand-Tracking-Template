using UnityEngine;

[RequireComponent(typeof(TwoTierHealthBar))]
public class HealthBarDebugHarness : MonoBehaviour
{
    public float max = 100f;
    public float current = 100f;
    public float testDamageAmount = 15f;

    TwoTierHealthBar bar;

    void Awake()
    {
        bar = GetComponent<TwoTierHealthBar>();
        bar.SetHealth(current, max);
    }

    void Update()
    {
        // Right controller trigger = damage
        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
        {
            Debug.Log("Left Triger worked"); 
            current = Mathf.Max(0, current - testDamageAmount);
            bar.SetHealth(current, max);
        }

        // Left controller trigger = heal
        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch))
        {
            Debug.Log("Right Triger worked");
            current = Mathf.Min(max, current + testDamageAmount);
            bar.SetHealth(current, max);
        }

        // Either controller's B/Y button = reset to full
        if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch) ||
            OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.LTouch))
        {
            Debug.Log("Resset to max");

            current = max;
            bar.SetHealth(current, max);
        }

        // Keep keyboard as a fallback for quick testing without the headset on
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            current = Mathf.Max(0, current - testDamageAmount);
            bar.SetHealth(current, max);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            current = Mathf.Min(max, current + testDamageAmount);
            bar.SetHealth(current, max);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            current = max;
            bar.SetHealth(current, max);
        }
    }
}