using UnityEngine;

public class FireTarget : MonoBehaviour
{
    public GameObject targetPrefab;
    public Transform spawnPoint;
    public float height = 2;
    [Tooltip("1 = real gravity, normal speed. Lower (e.g. 0.3) = slower rise AND fall, while still reaching the exact same 'height' peak.")]
    [Range(0.05f, 1f)] public float gravityScale = 1f;

    private GameObject spawned;
    private Canvas spawnedCanvas;
    private Rigidbody spawnedRb;
    private float scaledGravity;

    public void Spawn()
    {
        spawned = Instantiate(targetPrefab, spawnPoint.position, Quaternion.identity);

        Vector3 targetCamera = Camera.main.transform.position - spawned.transform.position;
        Vector3 lookCamera = Vector3.ProjectOnPlane(targetCamera, Vector3.up);
        spawned.transform.forward = -lookCamera.normalized;

        spawnedRb = spawned.GetComponent<Rigidbody>();
        spawnedRb.useGravity = false; // we apply our own scaled gravity in FixedUpdate instead

        scaledGravity = -Physics.gravity.y * gravityScale;
        spawnedRb.linearVelocity = Vector3.up * Mathf.Sqrt(2f * height * scaledGravity);

        // Find the target's UI canvas (health bar) so we can keep it hidden
        // while the target is still below the portal opening, in sync with
        // the stencil mask hiding the mesh itself.
        spawnedCanvas = spawned.GetComponentInChildren<Canvas>(true);
        if (spawnedCanvas != null)
        {
            spawnedCanvas.gameObject.SetActive(false);
        }
    }

    void FixedUpdate()
    {
        if (spawnedRb == null) return;
        spawnedRb.AddForce(Vector3.down * scaledGravity, ForceMode.Acceleration);
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
        spawnedRb = null;
    }
}