using UnityEngine;

public class CubeGenerator : MonoBehaviour
{
    [Header("Cube Setting"), Space(10)]
    [SerializeField] private MeshCollider meshCollider = null;

    private Mesh generatedMesh = null;
    private Vector3[] preVertices = null;

    private readonly Vector2[] quadUV =
    {
        new Vector2(0, 0),
        new Vector2(1, 0),
        new Vector2(1, 1),
        new Vector2(0, 1)
    };

    public Mesh GeneratedMseh { get => generatedMesh; }

    private void Start()
    {
        preVertices = new Vector3[4];
        generatedMesh = new Mesh();
    }

    public void CreateCube(Vector3[] polePosition)
    {
        Vector3 center = Vector3.zero;
        for (int i = 0; i < polePosition.Length; ++i)
            center += polePosition[i];
        center /= polePosition.Length;

        for (int i = 0; i < polePosition.Length; ++i)
            preVertices[i] = polePosition[i] - center;
        transform.position = center;

        Vector3[] vertices =
        {
            preVertices[0] + Vector3.down,  // 0, 2, 3
            preVertices[1] + Vector3.down,  // 0, 3, 1
            preVertices[0] + Vector3.up,
            preVertices[1] + Vector3.up,

            preVertices[3] + Vector3.up,    // 8, 4, 5
            preVertices[2] + Vector3.up,    // 8, 5, 9
            preVertices[3] + Vector3.down,
            preVertices[2] + Vector3.down,

            preVertices[0] + Vector3.up,    // 10, 6, 7
            preVertices[1] + Vector3.up,    // 10, 7, 11
            preVertices[3] + Vector3.up,
            preVertices[2] + Vector3.up,

            preVertices[3] + Vector3.down,  // 12, 13, 14
            preVertices[0] + Vector3.down,  // 12, 14, 15
            preVertices[1] + Vector3.down,
            preVertices[2] + Vector3.down,

            preVertices[1] + Vector3.down,  // 16, 17, 18
            preVertices[1] + Vector3.up,    // 16, 18, 19
            preVertices[2] + Vector3.up,
            preVertices[2] + Vector3.down,

            preVertices[3] + Vector3.down,  // 20, 21, 22
            preVertices[3] + Vector3.up,    // 20, 22, 23
            preVertices[0] + Vector3.up,
            preVertices[0] + Vector3.down
        };

        int[] triangles =
        {
            0, 2, 3,
            0, 3, 1,

            8, 4, 5,
            8, 5, 9,

            10, 6, 7,
            10, 7, 11,

            12, 13, 14,
            12, 14, 15,

            16, 17, 18,
            16, 18, 19,

            20, 21, 22,
            20, 22, 23
        };

        generatedMesh.Clear();
        generatedMesh.vertices = vertices;
        generatedMesh.triangles = triangles;

        meshCollider.sharedMesh = generatedMesh;
    }

    public void RefreshCube()
    {
        meshCollider.sharedMesh = null;

    }
}

