using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiskController : MonoBehaviour
{
    [Header("References")]
    private Rigidbody rb;
    private BoxCollider mainCollider;
    private Transform currentTower;
    private Camera mainCamera;
    private TowersOfHanoiMoveCounter moveCounter;
    private UndoMoveController undoMoveController;

    [Header("Disk Properties")]
    [SerializeField] public int diskSize;

    private static Dictionary<Transform, List<DiskController>> disksOnTowers = new Dictionary<Transform, List<DiskController>>();

    [Header("Movement Settings")]
    [SerializeField] private float dragHeight = 8f;
    [SerializeField] private float returningSpeed = 8f;
    [SerializeField] private float rotationAllowedHeight = 10f;

    [Header("Game State")]
    private static bool gameplayEnabled = false;
    private static int disksInMotion = 0;
    private bool isDragging = false;
    private bool hasLanded = false;
    private Vector3 lastValidPosition;
    private Quaternion lastValidRotation;
    private Quaternion initialRotation;
    private Transform previousTower;

    public static bool GameplayEnabled
    {
        get { return gameplayEnabled; }
        set { gameplayEnabled = value; }
    }

    public static void ResetDisksInMotionCounter()
    {
        disksInMotion = 0;
    }

    public static void IncrementDisksInMotionCounter()
    {
        disksInMotion++;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        mainCollider = GetComponent<BoxCollider>();
        mainCamera = Camera.main;
        moveCounter = FindObjectOfType<TowersOfHanoiMoveCounter>();
        undoMoveController = FindObjectOfType<UndoMoveController>();

        disksInMotion++;
    }

    private void Start()
    {
        rb.isKinematic = false;
        rb.useGravity = true;

        Debug.Log($"Disk {diskSize} is falling. Disks in motion: {disksInMotion}");
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasLanded) return;

        if (collision.gameObject.CompareTag("Tower") || collision.gameObject.CompareTag("Disk"))
        {
            hasLanded = true;
            Debug.Log($"Disk {diskSize} has landed");
            StartCoroutine(RegisterDiskAfterSettling());
        }
    }

    private IEnumerator RegisterDiskAfterSettling()
    {
        yield return new WaitForSeconds(2f);

        GameObject[] towers = GameObject.FindGameObjectsWithTag("Tower");
        Transform closestTower = FindClosestTower(towers);

        if (closestTower != null)
        {
            currentTower = closestTower;

            if (!disksOnTowers.ContainsKey(currentTower))
            {
                disksOnTowers[currentTower] = new List<DiskController>();
            }
            disksOnTowers[currentTower].Add(this);

            disksOnTowers[currentTower].Sort((a, b) => a.diskSize.CompareTo(b.diskSize));

            lastValidPosition = transform.position;
            lastValidRotation = transform.rotation;

            Debug.Log($"Disk {diskSize} registered to tower");
        }

        disksInMotion--;

        if (!gameplayEnabled)
        {
            gameplayEnabled = true;
            Debug.Log("FIRST DISK HAS LANDED - GAMEPLAY IS NOW ENABLED!");
        }
    }

    private void OnMouseDown()
    {
        if (!gameplayEnabled)
        {
            Debug.Log("Cannot move disks yet - wait for first disk to land");
            return;
        }

        if (IsDiskBlocked())
        {
            Debug.Log($"Disk {diskSize} is blocked by another disk above it");
            return;
        }

        isDragging = true;

        if (moveCounter != null)
        {
            moveCounter.OnDiskDragStart(gameObject);
        }

        initialRotation = transform.rotation;

        if (currentTower != null && disksOnTowers.ContainsKey(currentTower))
        {
            disksOnTowers[currentTower].Remove(this);
        }
    }

    private void OnMouseDrag()
    {
        if (!isDragging) return;

        Vector3 mousePos = GetMouseWorldPosition();
        Vector3 targetPosition;

        transform.rotation = initialRotation;

        if (mousePos.y < dragHeight)
        {
            targetPosition = new Vector3(
                currentTower != null ? currentTower.position.x : transform.position.x,
                mousePos.y,
                currentTower != null ? currentTower.position.z : transform.position.z
            );
        }
        else
        {
            targetPosition = new Vector3(
                mousePos.x,
                mousePos.y,
                currentTower != null ? currentTower.position.z : transform.position.z
            );
        }

        transform.position = targetPosition;
    }

    private void OnMouseUp()
    {
        if (!isDragging) return;
        isDragging = false;

        if (!TryPlaceDiskOnTower())
        {
            StartCoroutine(ReturnToLastValidPosition());
        }
    }

    private bool IsDiskBlocked()
    {
        if (currentTower == null) return false;

        foreach (DiskController disk in disksOnTowers[currentTower])
        {
            if (disk != this && disk.transform.position.y > transform.position.y)
            {
                return true;
            }
        }

        return false;
    }

    private bool TryPlaceDiskOnTower()
    {
        GameObject[] towers = GameObject.FindGameObjectsWithTag("Tower");
        Transform closestTower = FindClosestTower(towers);

        if (closestTower == null)
        {
            Debug.Log("No tower close enough to place disk");
            return false;
        }

        if (!disksOnTowers.ContainsKey(closestTower))
        {
            disksOnTowers[closestTower] = new List<DiskController>();
        }

        previousTower = currentTower;

        if (currentTower != null && disksOnTowers.ContainsKey(currentTower))
        {
            disksOnTowers[currentTower].Remove(this);
        }

        foreach (DiskController disk in disksOnTowers[closestTower])
        {
            if (disk.diskSize > diskSize)
            {
                Debug.Log($"Cannot place larger disk (size {diskSize}) on smaller disk (size {disk.diskSize})");
                return false;
            }
        }

        currentTower = closestTower;

        disksOnTowers[currentTower].Add(this);

        disksOnTowers[currentTower].Sort((a, b) => a.diskSize.CompareTo(b.diskSize));

        lastValidPosition = transform.position;
        lastValidRotation = transform.rotation;

        if (undoMoveController != null && previousTower != null)
        {
            undoMoveController.RecordMove(previousTower, currentTower, this);
        }

        Debug.Log($"Disk {diskSize} placed on tower");
        return true;
    }

    private IEnumerator ReturnToLastValidPosition()
    {
        Debug.Log("Returning disk to previous position");

        if (lastValidPosition == Vector3.zero && currentTower != null)
        {
            if (!disksOnTowers.ContainsKey(currentTower))
            {
                disksOnTowers[currentTower] = new List<DiskController>();
            }

            if (!disksOnTowers[currentTower].Contains(this))
            {
                disksOnTowers[currentTower].Add(this);
                disksOnTowers[currentTower].Sort((a, b) => a.diskSize.CompareTo(b.diskSize));
            }

            yield break;
        }

        float elapsed = 0f;
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        while (elapsed < 1f)
        {
            transform.position = Vector3.Lerp(startPos, lastValidPosition, elapsed * returningSpeed);
            transform.rotation = Quaternion.Slerp(startRot, lastValidRotation, elapsed * returningSpeed);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = lastValidPosition;
        transform.rotation = lastValidRotation;
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = Mathf.Abs(mainCamera.transform.position.z - transform.position.z);
        return mainCamera.ScreenToWorldPoint(mousePos);
    }

    private Transform FindClosestTower(GameObject[] towers)
    {
        Transform closestTower = null;
        float closestDistance = 30f;

        foreach (GameObject tower in towers)
        {
            float distance = Vector2.Distance(
                new Vector2(transform.position.x, transform.position.z),
                new Vector2(tower.transform.position.x, tower.transform.position.z)
            );

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTower = tower.transform;
            }
        }

        return closestTower;
    }

    public static Dictionary<Transform, List<DiskController>> GetDisksOnTowers()
    {
        return disksOnTowers;
    }

    public static bool AreDisksInMotion()
    {
        return disksInMotion > 0;
    }

    public static int GetDisksInMotion()
    {
        return disksInMotion;
    }
}