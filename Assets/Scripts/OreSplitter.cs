using System.Collections.Generic;
using UnityEngine;

public class OreSplitter : MonoBehaviour, IPickup
{
    [Header("Chunk Settings")]
    public int chunkCount = 5;
    public GameObject chunkPrefab;
    public float spawnRadius = 0.05f;
    public float springForce = 50f;
    public float springDamper = 5f;
    public float maxDistance = 0.1f;

    [Header("Layers")]
    public LayerMask pickupLayer;
    public LayerMask debrisLayer;

    private MeshFilter originalMeshFilter;
    private Rigidbody rootRb;
    private List<GameObject> chunks = new List<GameObject>();

    void Awake()
    {
        originalMeshFilter = GetComponentInChildren<MeshFilter>();
        rootRb = GetComponent<Rigidbody>();
    }

    private void OnDestroy()
    {
        foreach (var chunk in chunks)
        {
            Destroy(chunk);
        }
    }
    public void SplitOre()
    {
        Debug.Log("Now splitting ore: " + gameObject.name);
        if (originalMeshFilter == null || chunkPrefab == null) return;

        Mesh originalMesh = originalMeshFilter.mesh;
        Vector3[] verts = originalMesh.vertices;
        int[] tris = originalMesh.triangles;

        Vector3 boundsMin = originalMesh.bounds.min;
        Vector3 boundsMax = originalMesh.bounds.max;

        Vector3[] chunkCenters = new Vector3[chunkCount];
        for (int i = 0; i < chunkCount; i++)
        {
            chunkCenters[i] = new Vector3(
                Random.Range(boundsMin.x, boundsMax.x),
                Random.Range(boundsMin.y, boundsMax.y),
                Random.Range(boundsMin.z, boundsMax.z)
            );
        }

        List<Vector3>[] vertsLists = new List<Vector3>[chunkCount];
        List<int>[] trisLists = new List<int>[chunkCount];
        for (int i = 0; i < chunkCount; i++)
        {
            vertsLists[i] = new List<Vector3>();
            trisLists[i] = new List<int>();
        }

        // Assign triangles to nearest chunk center
        for (int t = 0; t < tris.Length; t += 3)
        {
            Vector3 v0 = verts[tris[t]];
            Vector3 v1 = verts[tris[t + 1]];
            Vector3 v2 = verts[tris[t + 2]];

            Vector3 centroid = (v0 + v1 + v2) / 3f;

            int nearest = 0;
            float minDist = Vector3.Distance(centroid, chunkCenters[0]);
            for (int i = 1; i < chunkCount; i++)
            {
                float d = Vector3.Distance(centroid, chunkCenters[i]);
                if (d < minDist)
                {
                    minDist = d;
                    nearest = i;
                }
            }

            List<Vector3> vList = vertsLists[nearest];
            List<int> tList = trisLists[nearest];

            int baseIndex = vList.Count;
            vList.Add(v0);
            vList.Add(v1);
            vList.Add(v2);

            tList.Add(baseIndex);
            tList.Add(baseIndex + 1);
            tList.Add(baseIndex + 2);
        }

        // Spawn chunks
        for (int i = 0; i < chunkCount; i++)
        {
            GameObject chunk = Instantiate(chunkPrefab, transform.position, transform.rotation, transform);

            Mesh chunkMesh = new Mesh();
            chunkMesh.vertices = vertsLists[i].ToArray();
            chunkMesh.triangles = trisLists[i].ToArray();

            Vector3 solidCenter = originalMesh.bounds.center;
            Debug.Log($"Mesh UV count: {chunkMesh.uv.Length}, Vertex count: {chunkMesh.vertexCount}");
            if (chunkMesh.uv.Length > 0)
                Debug.Log($"First UV: {chunkMesh.uv[0]}, Last UV: {chunkMesh.uv[chunkMesh.uv.Length - 1]}");
            else
                Debug.Log("NO UVs on split mesh!");

            MakeMeshSolid(chunkMesh, solidCenter);

            chunkMesh.RecalculateNormals();
            chunkMesh.RecalculateBounds();


            MeshFilter mf;
            MeshCollider col;

            Rigidbody rb;

            if (i == 0)
            {
                rb = gameObject.GetComponent<Rigidbody>();
                Destroy(gameObject.GetComponent<Collider>());
                mf = gameObject.AddComponent<MeshFilter>();
                col = gameObject.AddComponent<MeshCollider>();
                rb.mass = 1;
                Destroy(chunk);
            }
            else
            {
                mf = chunk.GetComponent<MeshFilter>();
                rb = chunk.GetComponent<Rigidbody>();
                col = chunk.GetComponent<MeshCollider>();
                if (col == null) col = chunk.AddComponent<MeshCollider>();

                rb.mass = 0.2f;

                SpringJoint sj = chunk.AddComponent<SpringJoint>();
                sj.connectedBody = rootRb;
                sj.spring = springForce;
                sj.damper = springDamper;
                sj.maxDistance = maxDistance;
                sj.autoConfigureConnectedAnchor = true;
                chunk.transform.localPosition += Random.insideUnitSphere * spawnRadius;
                chunks.Add(chunk);
            }


            mf.mesh = chunkMesh;
            chunk.GetComponent<MeshRenderer>().material =
                originalMeshFilter.GetComponent<MeshRenderer>().material;

            rb.linearDamping = 2f;
            rb.angularDamping = 5f;
            
            col.sharedMesh = chunkMesh;
            col.convex = true;
        }

        originalMeshFilter.gameObject.SetActive(false);
    }
    public void Pickup()
    {
        CompressChunks();
    }

    public void Drop()
    {
        DecompressChunks();
    }

    private void CompressChunks()
    {
        Debug.Log("compressing");
        foreach (var chunk in chunks)
        {
            SpringJoint sj = chunk.GetComponent<SpringJoint>();
            if (sj != null)
            {
                sj.spring = springForce * 15f;
                sj.damper = 0;
                sj.maxDistance = 0;
            }
        }
    }
    private void DecompressChunks()
    {
        Debug.Log("decompressing");
        foreach (var chunk in chunks)
        {
            SpringJoint sj = chunk.GetComponent<SpringJoint>();
            if (sj != null)
            {
                sj.spring = springForce;
                sj.damper = springDamper;
                sj.maxDistance = maxDistance;
            }
        }
    }
    struct Edge
    {
        public int v1;
        public int v2;

        public Edge(int a, int b)
        {
            v1 = Mathf.Min(a, b);
            v2 = Mathf.Max(a, b);
        }

        public override bool Equals(object obj)
        {
            if (obj is Edge other)
            {
                return v1 == other.v1 && v2 == other.v2;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return v1 ^ (v2 << 16);
        }
    }
    public void MakeMeshSolid(Mesh mesh, Vector3 centerPoint)
    {
        Vector3[] originalVerts = mesh.vertices;
        int[] originalTris = mesh.triangles;

        if (originalTris.Length < 3) return;

        // ============================================================
        // Step 1: Aggressive vertex welding
        // ============================================================
        float weldThreshold = 0.005f;
        float weldThresholdSq = weldThreshold * weldThreshold;

        int vertCount = originalVerts.Length;
        int[] vertexRemap = new int[vertCount];
        List<Vector3> uniqueVerts = new List<Vector3>();

        for (int i = 0; i < vertCount; i++)
        {
            int foundIndex = -1;
            for (int j = 0; j < uniqueVerts.Count; j++)
            {
                if ((originalVerts[i] - uniqueVerts[j]).sqrMagnitude < weldThresholdSq)
                {
                    foundIndex = j;
                    break;
                }
            }

            if (foundIndex >= 0)
            {
                vertexRemap[i] = foundIndex;
            }
            else
            {
                vertexRemap[i] = uniqueVerts.Count;
                uniqueVerts.Add(originalVerts[i]);
            }
        }

        // Remap and filter degenerate triangles
        List<int> cleanTris = new List<int>();
        for (int t = 0; t < originalTris.Length; t += 3)
        {
            int v0 = vertexRemap[originalTris[t]];
            int v1 = vertexRemap[originalTris[t + 1]];
            int v2 = vertexRemap[originalTris[t + 2]];

            if (v0 == v1 || v1 == v2 || v2 == v0) continue;

            cleanTris.Add(v0);
            cleanTris.Add(v1);
            cleanTris.Add(v2);
        }

        // ============================================================
        // Step 2: Find boundary edges using directed edge counting
        // ============================================================
        HashSet<long> directedEdges = new HashSet<long>();
        Dictionary<long, (int v1, int v2)> edgeData = new Dictionary<long, (int, int)>();

        for (int t = 0; t < cleanTris.Count; t += 3)
        {
            int v0 = cleanTris[t];
            int v1 = cleanTris[t + 1];
            int v2 = cleanTris[t + 2];

            AddDirectedEdge(directedEdges, edgeData, v0, v1);
            AddDirectedEdge(directedEdges, edgeData, v1, v2);
            AddDirectedEdge(directedEdges, edgeData, v2, v0);
        }

        List<(int v1, int v2)> boundaryEdges = new List<(int, int)>();
        foreach (var kvp in edgeData)
        {
            long key = kvp.Key;
            int a = kvp.Value.v1;
            int b = kvp.Value.v2;

            long reverseKey = DirectedEdgeKey(b, a);
            if (!directedEdges.Contains(reverseKey))
            {
                boundaryEdges.Add((a, b));
            }
        }

        // ============================================================
        // Step 3: Build final mesh — flat shaded
        // ============================================================
        int origTriCount = cleanTris.Count / 3;
        int fanTriCount = boundaryEdges.Count * 2;
        int totalTriCount = origTriCount + fanTriCount;

        Vector3[] flatVerts = new Vector3[totalTriCount * 3];
        int[] flatTris = new int[totalTriCount * 3];
        int vi = 0;

        for (int t = 0; t < cleanTris.Count; t += 3)
        {
            flatVerts[vi] = uniqueVerts[cleanTris[t]];
            flatVerts[vi + 1] = uniqueVerts[cleanTris[t + 1]];
            flatVerts[vi + 2] = uniqueVerts[cleanTris[t + 2]];

            flatTris[vi] = vi;
            flatTris[vi + 1] = vi + 1;
            flatTris[vi + 2] = vi + 2;
            vi += 3;
        }

        foreach (var edge in boundaryEdges)
        {
            Vector3 a = uniqueVerts[edge.v1];
            Vector3 b = uniqueVerts[edge.v2];
            Vector3 c = centerPoint;

            flatVerts[vi] = a;
            flatVerts[vi + 1] = b;
            flatVerts[vi + 2] = c;
            flatTris[vi] = vi;
            flatTris[vi + 1] = vi + 1;
            flatTris[vi + 2] = vi + 2;
            vi += 3;

            flatVerts[vi] = b;
            flatVerts[vi + 1] = a;
            flatVerts[vi + 2] = c;
            flatTris[vi] = vi;
            flatTris[vi + 1] = vi + 1;
            flatTris[vi + 2] = vi + 2;
            vi += 3;
        }

        mesh.Clear();
        mesh.vertices = flatVerts;
        mesh.triangles = flatTris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        // Generate UVs via triplanar projection
        Vector3[] normals = mesh.normals;
        Vector2[] projectedUVs = new Vector2[flatVerts.Length];
        float textureScale = 2.3f;

        for (int i = 0; i < flatVerts.Length; i += 3)
        {
            Vector3 normal = normals[i];

            float absX = Mathf.Abs(normal.x);
            float absY = Mathf.Abs(normal.y);
            float absZ = Mathf.Abs(normal.z);

            for (int j = 0; j < 3; j++)
            {
                Vector3 v = flatVerts[i + j];

                if (absX >= absY && absX >= absZ)
                {
                    projectedUVs[i + j] = new Vector2(v.y, v.z) * textureScale;
                }
                else if (absY >= absX && absY >= absZ)
                {
                    projectedUVs[i + j] = new Vector2(v.x, v.z) * textureScale;
                }
                else
                {
                    projectedUVs[i + j] = new Vector2(v.x, v.y) * textureScale;
                }
            }
        }

        mesh.uv = projectedUVs;
    }

    long DirectedEdgeKey(int a, int b)
    {
        return ((long)a << 32) | (long)(uint)b;
    }

    void AddDirectedEdge(
        HashSet<long> edgeSet,
        Dictionary<long, (int, int)> edgeData,
        int a, int b)
    {
        long key = DirectedEdgeKey(a, b);
        edgeSet.Add(key);
        if (!edgeData.ContainsKey(key))
        {
            edgeData[key] = (a, b);
        }
    }

    
}