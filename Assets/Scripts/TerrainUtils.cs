using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainUtils : MonoBehaviour
{
    [SerializeField] private LayerMask TreeMask;

    public LayerMask getTreeMask()
    {
        return TreeMask;
    }
}
