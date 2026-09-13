using UnityEngine;

/// <summary>
/// Attach to the shuriken prefab. Wire this object's own
/// "Interactable Unity Event Wrapper -> When Select()" to OnGrabbed() in the
/// Inspector - self-referencing wiring like this persists correctly across
/// every instantiated copy of the prefab. Notifies whichever ShurikenSpawner
/// spawned this instance so it can spawn the next one.
/// </summary>
public class ShurikenGrabNotifier : MonoBehaviour
{
    [HideInInspector] public ShurikenSpawner spawner;
    bool notified = false;

    public void OnGrabbed()
    {
        if (notified) return;
        notified = true;

        if (spawner != null)
        {
            spawner.NotifyGrabbed();
        }
    }
}