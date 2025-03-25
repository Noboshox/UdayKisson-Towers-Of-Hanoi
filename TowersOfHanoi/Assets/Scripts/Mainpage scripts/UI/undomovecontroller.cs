using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class UndoMoveController : MonoBehaviour
{
    [System.Serializable]
    private class DiskMove
    {
        public Transform sourceTower;
        public Transform destinationTower;
        public DiskController disk;
        public Vector3 localPositionOnTower;
    }

    private Stack<DiskMove> moveHistory = new Stack<DiskMove>();

    public void RecordMove(Transform sourceTower, Transform destinationTower, DiskController disk)
    {
        if (sourceTower == null || destinationTower == null || disk == null)
        {
            Debug.LogError("RecordMove: One or more parameters are null!");
            return;
        }

        DiskMove move = new DiskMove
        {
            sourceTower = sourceTower,
            destinationTower = destinationTower,
            disk = disk,
            localPositionOnTower = destinationTower.InverseTransformPoint(disk.transform.position)
        };
        moveHistory.Push(move);
        Debug.Log($"Move Recorded: Disk {disk.diskSize} from {sourceTower.name} to {destinationTower.name} at local position {move.localPositionOnTower}");
    }

    public void UndoLastMove()
    {
        if (moveHistory.Count == 0)
        {
            Debug.Log("No moves to undo.");
            return;
        }

        DiskMove lastMove = moveHistory.Pop();

        if (lastMove.disk == null)
        {
            Debug.LogError("UndoLastMove: Disk is null!");
            return;
        }

        StartCoroutine(UndoMoveCoroutine(lastMove));
    }

    private IEnumerator UndoMoveCoroutine(DiskMove lastMove)
    {
        Rigidbody rb = null;
        Dictionary<Transform, List<DiskController>> disksOnTowers = null;

        try
        {
            rb = lastMove.disk.GetComponent<Rigidbody>();
            rb.isKinematic = true;

            disksOnTowers = DiskController.GetDisksOnTowers();

            if (disksOnTowers.ContainsKey(lastMove.destinationTower))
            {
                disksOnTowers[lastMove.destinationTower].Remove(lastMove.disk);
            }

            lastMove.disk.transform.SetParent(lastMove.sourceTower);

            Vector3 worldPosition = lastMove.sourceTower.TransformPoint(lastMove.localPositionOnTower);
            lastMove.disk.transform.position = worldPosition;

            if (!disksOnTowers.ContainsKey(lastMove.sourceTower))
            {
                disksOnTowers[lastMove.sourceTower] = new List<DiskController>();
            }
            disksOnTowers[lastMove.sourceTower].Add(lastMove.disk);

            disksOnTowers[lastMove.sourceTower].Sort((a, b) => a.diskSize.CompareTo(b.diskSize));
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error during undo move: {e.Message}");
            Debug.LogError(e.StackTrace);
            yield break;
        }

        // Yield outside of try-catch
        yield return null;

        try
        {
            if (rb != null)
            {
                rb.isKinematic = false;
            }

            // Reset any internal state
            System.Reflection.FieldInfo hasLandedField = typeof(DiskController).GetField("hasLanded",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (hasLandedField != null)
            {
                hasLandedField.SetValue(lastMove.disk, false);
            }

            Debug.Log($"Successfully undid move for Disk {lastMove.disk.diskSize}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error finalizing undo move: {e.Message}");
            Debug.LogError(e.StackTrace);
        }
    }

    public void ClearMoveHistory()
    {
        moveHistory.Clear();
        Debug.Log("Move history cleared.");
    }
}   