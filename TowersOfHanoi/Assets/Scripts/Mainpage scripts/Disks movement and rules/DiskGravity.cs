using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TowersOfHanoiController : MonoBehaviour
{
    [Header("Disk References")]
    [SerializeField] private GameObject[] diskObjects;
    [SerializeField] private Transform startTower;
    [SerializeField] private float dropDelay = 0.5f;

    [Header("UI References")]
    [SerializeField] private Slider diskCountSlider;

    [Header("Tower References")]
    [SerializeField] private Transform[] allTowers;

    [Header("Settings")]
    [SerializeField] private int initialDiskCount = 3;
    [SerializeField] private float velocityThreshold = 0.01f;
    [SerializeField] private float towerProximityThreshold = 1.0f;
    [SerializeField] private float checkInterval = 0.1f;
    [SerializeField] private float sliderLockDelay = 0.5f;

    private Dictionary<GameObject, Transform> diskToInitialTower = new Dictionary<GameObject, Transform>();
    private HashSet<GameObject> activeDisks = new HashSet<GameObject>();
    private bool sliderLocked = false;
    private int currentDiskCount;
    private bool gameplayStarted = false;
    private bool diskMoved = false;
    private bool firstDiskMoved = false;

    void Awake()
    {
        currentDiskCount = initialDiskCount;
        ResetAllDisks();
    }

    void Start()
    {
        SetupDiskCountSlider();
        StartCoroutine(InitializeDiskPositions());
    }

    void SetupDiskCountSlider()
    {
        if (diskCountSlider != null)
        {
            diskCountSlider.minValue = initialDiskCount;
            diskCountSlider.maxValue = diskObjects.Length;
            diskCountSlider.value = initialDiskCount;
            diskCountSlider.onValueChanged.AddListener(OnSliderValueChanged);
        }
    }

    void ResetAllDisks()
    {
        foreach (GameObject disk in diskObjects)
        {
            Rigidbody rb = disk.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            Collider[] colliders = disk.GetComponents<Collider>();
            foreach (Collider collider in colliders)
            {
                collider.enabled = false;
            }

            disk.SetActive(false);
        }
    }

    IEnumerator InitializeDiskPositions()
    {
        yield return new WaitForSeconds(0.2f);

        for (int i = 0; i < initialDiskCount && i < diskObjects.Length; i++)
        {
            ActivateDisk(diskObjects[i]);
            activeDisks.Add(diskObjects[i]);

            yield return new WaitForSeconds(dropDelay);
        }

        yield return StartCoroutine(WaitForAllDisksToSettle());

        foreach (GameObject disk in activeDisks)
        {
            Transform tower = FindTowerUnderDisk(disk.transform);
            if (tower != null)
            {
                diskToInitialTower[disk] = tower;
            }
        }

        gameplayStarted = true;
    }

    void ActivateDisk(GameObject disk)
    {
        disk.SetActive(true);

        Rigidbody rb = disk.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        Collider[] colliders = disk.GetComponents<Collider>();
        foreach (Collider collider in colliders)
        {
            collider.enabled = true;
        }
    }

    void OnSliderValueChanged(float value)
    {
        if (sliderLocked) return;

        int newDiskCount = Mathf.RoundToInt(value);

        if (newDiskCount > currentDiskCount)
        {
            StartCoroutine(AddDisks(currentDiskCount, newDiskCount));
        }

        currentDiskCount = newDiskCount;
    }

    IEnumerator AddDisks(int fromCount, int toCount)
    {
        for (int i = fromCount; i < toCount && i < diskObjects.Length; i++)
        {
            ActivateDisk(diskObjects[i]);
            activeDisks.Add(diskObjects[i]);

            yield return new WaitForSeconds(dropDelay);
        }

        yield return StartCoroutine(WaitForAllDisksToSettle());

        foreach (GameObject disk in activeDisks)
        {
            if (!diskToInitialTower.ContainsKey(disk))
            {
                Transform tower = FindTowerUnderDisk(disk.transform);
                if (tower != null)
                {
                    diskToInitialTower[disk] = tower;
                }
            }
        }
    }

    IEnumerator WaitForAllDisksToSettle()
    {
        bool allSettled = false;

        while (!allSettled)
        {
            int settledCount = 0;
            int totalActiveDisks = 0;

            foreach (GameObject disk in activeDisks)
            {
                if (!disk.activeSelf) continue;
                totalActiveDisks++;

                Rigidbody rb = disk.GetComponent<Rigidbody>();
                if (rb != null && rb.linearVelocity.magnitude < velocityThreshold)
                {
                    settledCount++;
                }
            }

            if (settledCount >= totalActiveDisks && totalActiveDisks > 0)
            {
                allSettled = true;
            }
            else
            {
                yield return new WaitForSeconds(checkInterval);
            }
        }
    }

    void Update()
    {
        if (!sliderLocked && gameplayStarted)
        {
            CheckForDiskMovedToNewTower();
        }

        if (diskMoved && !firstDiskMoved)
        {
            firstDiskMoved = true;
            StartCoroutine(LockSliderWithDelay());
        }
    }

    void CheckForDiskMovedToNewTower()
    {
        foreach (GameObject disk in activeDisks)
        {
            if (!diskToInitialTower.ContainsKey(disk))
                continue;

            Transform initialTower = diskToInitialTower[disk];
            Transform currentTower = FindTowerUnderDisk(disk.transform);

            Rigidbody rb = disk.GetComponent<Rigidbody>();
            bool isStable = rb != null && rb.linearVelocity.magnitude < velocityThreshold;

            if (!isStable)
                continue;

            if (currentTower != null && currentTower != initialTower)
            {
                diskMoved = true;
                break;
            }
        }
    }

    Transform FindTowerUnderDisk(Transform diskTransform)
    {
        Transform closestTower = null;
        float closestDistance = towerProximityThreshold;

        foreach (Transform tower in allTowers)
        {
            float horizontalDistance = Vector2.Distance(
                new Vector2(diskTransform.position.x, diskTransform.position.z),
                new Vector2(tower.position.x, tower.position.z)
            );

            if (horizontalDistance < closestDistance)
            {
                closestDistance = horizontalDistance;
                closestTower = tower;
            }
        }

        return closestTower;
    }

    IEnumerator LockSliderWithDelay()
    {
        diskMoved = false;
        yield return new WaitForSeconds(sliderLockDelay);

        sliderLocked = true;
        if (diskCountSlider != null)
        {
            diskCountSlider.interactable = false;
        }
    }
}