using System.Collections;
using UnityEngine;

/// <summary>
/// Keeps exactly one ungrabbed shuriken sitting at spawnPoint at all times.
/// The moment it's grabbed, spawns the next one - unless the global active
/// cap (ShurikenManager.maxActive) is reached, in which case it retries
/// periodically until a slot frees up.
/// </summary>
public class ShurikenSpawner : MonoBehaviour
{
    public Transform spawnPoint;
    public GameObject shurikenPrefab;
    public float retryInterval = 0.5f;

    GameObject currentWaiting;
    Coroutine retryRoutine;

    void Start()
    {
        TrySpawn();
    }

    public void NotifyGrabbed()
    {
        currentWaiting = null;
        TrySpawn();
    }

    void TrySpawn()
    {
        if (currentWaiting != null) return;

        if (StuckShurikenManager.Instance != null &&
            StuckShurikenManager.Instance.ActiveCount >= StuckShurikenManager.Instance.maxActive)
        {
            if (retryRoutine == null)
            {
                retryRoutine = StartCoroutine(RetrySpawnLoop());
            }
            return;
        }

        SpawnNow();
    }

    IEnumerator RetrySpawnLoop()
    {
        while (currentWaiting == null)
        {
            yield return new WaitForSeconds(retryInterval);

            if (StuckShurikenManager.Instance == null ||
                StuckShurikenManager.Instance.ActiveCount < StuckShurikenManager.Instance.maxActive)
            {
                SpawnNow();
                break;
            }
        }
        retryRoutine = null;
    }

    void SpawnNow()
    {
        GameObject instance = Instantiate(shurikenPrefab, spawnPoint.position, spawnPoint.rotation);
        currentWaiting = instance;

        ShurikenGrabNotifier notifier = instance.GetComponent<ShurikenGrabNotifier>();
        if (notifier == null)
        {
            notifier = instance.AddComponent<ShurikenGrabNotifier>();
        }
        notifier.spawner = this;
    }
}