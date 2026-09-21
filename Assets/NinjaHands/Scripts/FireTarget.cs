using UnityEngine;

public class FireTarget : MonoBehaviour
{
    public GameObject targetPrefab;
    public Transform spawnPoint;
    public float height = 2;
    [Tooltip("1 = real gravity, normal speed. Lower (e.g. 0.3) = slower rise, while still reaching the exact same 'height' peak.")]
    [Range(0.05f, 1f)] public float gravityScale = 1f;

    private GameObject spawned;
    private Canvas spawnedCanvas;
    private Rigidbody spawnedRb;

    private float verticalVelocity;
    private float scaledGravity;
    private bool isRising;

    public void Spawn()
    {
        spawned = Instantiate(targetPrefab, spawnPoint.position, Quaternion.identity);

        Vector3 targetCamera = Camera.main.transform.position - spawned.transform.position;
        Vector3 lookCamera = Vector3.ProjectOnPlane(targetCamera, Vector3.up);
        spawned.transform.forward = -lookCamera.normalized;

        spawnedRb = spawned.GetComponent<Rigidbody>();
        spawnedRb.useGravity = false;
        spawnedRb.isKinematic = true; // immune to any collision force — hits still register, but never push/spin it

        scaledGravity = -Physics.gravity.y * gravityScale;
        verticalVelocity = Mathf.Sqrt(2f * height * scaledGravity);
        isRising = true;

        spawnedCanvas = spawned.GetComponentInChildren<Canvas>(true);
        if (spawnedCanvas != null)
        {
            spawnedCanvas.gameObject.SetActive(false);
        }
    }

    void FixedUpdate()
    {
        if (spawnedRb == null || !isRising) return;

        verticalVelocity -= scaledGravity * Time.fixedDeltaTime;
        Vector3 newPos = spawnedRb.position + Vector3.up * (verticalVelocity * Time.fixedDeltaTime);
        spawnedRb.MovePosition(newPos);
    }

    void Update()
    {
        if (spawned == null || spawnedCanvas == null || !isRising) return;

        bool aboveThreshold = spawned.transform.position.y > spawnPoint.position.y;
        if (spawnedCanvas.gameObject.activeSelf != aboveThreshold)
        {
            spawnedCanvas.gameObject.SetActive(aboveThreshold);
        }
    }

    public void Despawn()
    {
        if (spawned != null) Destroy(spawned);
        spawned = null;
        spawnedCanvas = null;
        spawnedRb = null;
        isRising = false;
    }
}