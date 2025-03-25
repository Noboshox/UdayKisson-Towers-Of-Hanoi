using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class WinConditionController : MonoBehaviour
{
    [Header("Tower References")]
    [SerializeField] private Transform tower1;
    [SerializeField] private Transform tower2;
    [SerializeField] private Transform Tower3;
    [Header("Game Settings")]
    [SerializeField] private Slider diskCountSlider;
    private bool gameWon = false;
    private int totalDiskCount = 0;
    private bool isWaitingForDiskFall = false;

    private void Update()
    {
        // If game is already won, stop checking
        if (gameWon) return;

        // Dynamically get total disk count from slider
        totalDiskCount = Mathf.RoundToInt(diskCountSlider.value);

        // Check win condition every frame
        CheckWinCondition();
    }

    void CheckWinCondition()
    {
        // Get disks on towers
        var disksOnTowers = DiskController.GetDisksOnTowers();

        // Check if target tower has disks
        if (!disksOnTowers.ContainsKey(Tower3))
        {
            return;
        }

        var disksOnTargetTower = disksOnTowers[Tower3];

        // Verify disk stacking order and count
        if (disksOnTargetTower.Count == totalDiskCount && VerifyDiskStackingOrder(disksOnTargetTower))
        {
            if (!isWaitingForDiskFall)
            {
                StartCoroutine(WaitForDiskFallCompletion(disksOnTargetTower));
            }
        }
    }

    IEnumerator WaitForDiskFallCompletion(List<DiskController> disks)
    {
        isWaitingForDiskFall = true;

        // this waits till all the disks have fallen
        yield return new WaitForSeconds(1f);

        // Checks if all disks have come to rest
        bool allDiskSettled = disks.All(disk => disk.GetComponent<Rigidbody>().linearVelocity.magnitude < 0.01f);

        if (allDiskSettled)
        {
            TriggerWinCondition();
        }

        isWaitingForDiskFall = false;
    }

    bool VerifyDiskStackingOrder(List<DiskController> disks)
    {
        // Sorting the disks by diskSize (ascending)
        disks.Sort((a, b) => a.diskSize.CompareTo(b.diskSize));

        // Verifying that disks are in correct order from bottom to top
        for (int i = 0; i < disks.Count - 1; i++)
        {
            if (disks[i].diskSize > disks[i + 1].diskSize)
            {
                return false;
            }
        }
        return true;
    }

    void TriggerWinCondition()
    {
        gameWon = true;
        Debug.Log("===================");
        Debug.Log("  CONGRATULATIONS!  ");
        Debug.Log("    TOWER COMPLETE! ");
        Debug.Log("===================");

        // Load the WinPage scene
        SceneManager.LoadScene("WinPage");
    }
}