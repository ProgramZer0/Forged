using UnityEngine;

/// <summary>
/// Attached to an item the first time Normal mode condensing begins.
/// Persists the target bar vertices so re-placing the item on the anvil
/// never triggers a recalculation.
/// </summary>
public class CondensingData : MonoBehaviour
{
    public Vector3[] targetVertices;
}