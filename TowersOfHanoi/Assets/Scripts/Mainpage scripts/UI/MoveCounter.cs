using UnityEngine;
using TMPro;

public class TowersOfHanoiMoveCounter : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI moveCounterText;
    [SerializeField] private Transform[] towers; 

    private int moveCount = 0;
    private GameObject currentDisk = null;

    void Update()
    {
        // Check if a disk is being dragged
        if (currentDisk != null)
        {
            CheckDiskMovement();
        }
    }

    public void SetCurrentDisk(GameObject disk)
    {
        currentDisk = disk;
    }

    private void CheckDiskMovement()
    {
        if (currentDisk == null) return;

        // Find which tower the disk is closest to
        Transform closestTower = FindClosestTower(currentDisk.transform.position);

        if (closestTower != null)
        {
            IncrementMoveCount();
            currentDisk = null; // Reset to prevent multiple counts
        }
    }

    private Transform FindClosestTower(Vector3 diskPosition)
    {
        Transform closest = null;
        float closestDistance = float.MaxValue;

        foreach (Transform tower in towers)
        {
            float distance = Vector3.Distance(diskPosition, tower.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = tower;
            }
        }

        return closest;
    }

    private void IncrementMoveCount()
    {
        moveCount++;
        UpdateMoveCounterDisplay();
    }

    private void UpdateMoveCounterDisplay()
    {
        if (moveCounterText != null)
        {
            moveCounterText.text = "Moves: " + moveCount.ToString();
        }
    }
    public void OnDiskDragStart(GameObject disk)
    {
        SetCurrentDisk(disk);
    }
}