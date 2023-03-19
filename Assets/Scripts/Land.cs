using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Land : MonoBehaviour
{
    [Header("Land Collider Setting"), Space(10)]
    [SerializeField] private MeshCollider landMeshCollider = null;

    private Transform[] landPoleArray = null;
    private List<Vector3[]> cropPositionList = null;
    private GameObject[] cropObjectList = null;

    public void InitializeObject()
    {
        cropPositionList = new List<Vector3[]>();
    }

    public void InitializeData(ref List<Vector3[]> cropPositionList, Transform[] landPoleArray, Mesh colliderMesh)
    {
        this.cropPositionList.AddRange(cropPositionList);
        this.landPoleArray = landPoleArray;
        for (int i = 0; i < landPoleArray.Length; ++i)
            landPoleArray[i].SetParent(transform);

        int cropCount = 0;
        for (int i = 0; i < cropPositionList.Count; ++i)
            cropCount += cropPositionList[i].Length;
        cropObjectList = new GameObject[cropCount];

        landMeshCollider.sharedMesh = colliderMesh;
    }

    public void GenerateCrop()
    {
        StartCoroutine(GenerateCropCoroutine());
    }

    public void RemoveCrop()
    {
        for (int i = 0; i < cropObjectList.Length; ++i)
        {
            if (cropObjectList[i] != null)
            {
                Destroy(cropObjectList[i]);
                cropObjectList[i] = null;
            }
        }
    }

    private IEnumerator GenerateCropCoroutine()
    {
        int index = 0;
        for (int row = 0; row < cropPositionList.Count; ++row)
        {
            Vector3[] positionList = cropPositionList[row];
            for (int col = 0; col < positionList.Length; ++col)
            {
                Vector3 position = positionList[col];
                position.y = 100;
                if (Physics.Raycast(position, Vector3.down, out RaycastHit hit, 200, 1))
                {
                    GameObject clone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    clone.transform.position = hit.point;
                    cropObjectList[index++] = clone;
                }

                yield return new WaitForSeconds(.1f);
            }
        }
    }
}
