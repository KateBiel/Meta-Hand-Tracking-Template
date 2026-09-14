using System.Collections;
using UnityEngine;

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

    void Update()
    {
        // Self-healing: if the waiting shuriken vanished for any reason
        // (grabbed via NotifyGrabbed, or destroyed externally e.g. by
        // StuckShurikenManager.ClearAll on a level switch), notice it here
        // and spawn a fresh one, instead of relying only on NotifyGrabbed.
        if (currentWaiting == null && retryRoutine == null)
        {
            TrySpawn();
        }
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