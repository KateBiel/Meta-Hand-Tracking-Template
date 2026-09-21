using UnityEngine;

public class FireTarget : MonoBehaviour
{
    public GameObject targetPrefab;
    public Transform spawnPoint;
    public float height = 2;
    [Tooltip("1 = normal launch speed. Lower = slower rise, but note: reducing speed without changing height means it won't fully reach 'height' anymore, since less initial velocity under the same gravity peaks lower.")]
    public float launchSpeedMultiplier = 1f;

    private GameObject spawned;
    private Canvas spawnedCanvas;

    public void Spawn()
    {
        spawned = Instantiate(targetPrefab, spawnPoint.position, Quaternion.identity);

        Vector3 targetCamera = Camera.main.transform.position - spawned.transform.position;
        Vector3 lookCamera = Vector3.ProjectOnPlane(targetCamera, Vector3.up);
        spawned.transform.forward = -lookCamera.normalized;

        Rigidbody rb = spawned.GetComponent<Rigidbody>();
        rb.linearVelocity = Vector3.up * Mathf.Sqrt(2f * height * -Physics.gravity.y) * launchSpeedMultiplier;

        // Find the target's UI canvas (health bar) so we can keep it hidden
        // while the target is still below the portal opening, in sync with
        // the stencil mask hiding the mesh itself.
        spawnedCanvas = spawned.GetComponentInChildren<Canvas>(true);
        if (spawnedCanvas != null)
        {
            spawnedCanvas.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (spawned == null || spawnedCanvas == null) return;

        // Portal plane = spawnPoint's own height. Show the UI only once the
        // target has actually risen above that point, matching the moment
        // it visually clears the portal mask.
        bool aboveThreshold = spawned.transform.position.y > spawnPoint.position.y;
        if (spawnedCanvas.gameObject.activeSelf != aboveThreshold)
        {
            spawnedCanvas.gameObject.SetActive(aboveThreshold);
        }
    }

    public void Despawn()
    {
        Destroy(spawned);
        spawned = null;
        spawnedCanvas = null;
    }
}