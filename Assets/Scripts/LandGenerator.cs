using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.Experimental.AssetDatabaseExperimental.AssetDatabaseCounters;

public class LandGenerator : MonoBehaviour
{
    [SerializeField] private Text Test = null;

    [Header("Land Setting"), Space(10)]
    [SerializeField] private Transform[] LandPoles = null;
    [SerializeField] private float PoleLerpScale = 0f;
    [SerializeField] private CubeGenerator LandAllCollider = null; // Crop 전용 Collider도 추가 필요
    [SerializeField] private CubeGenerator LandPartCollider = null;
    [SerializeField] private Land LandObject = null;

    private bool isGeneratingLand = false;
    private Land preSelectedLand = null;

    private void Update()
    {
        if (!isGeneratingLand)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 1000, 64))
                {
                    preSelectedLand = hit.transform.GetComponent<Land>();
                    Debug.Log(preSelectedLand);
                }
                else
                    preSelectedLand = null;
            }

            if (Input.GetKeyDown(KeyCode.Q))
            {
                isGeneratingLand = true;
                StartCoroutine(ConstructLand());
            }

            if (preSelectedLand == null) return;
            if (Input.GetKeyDown(KeyCode.E))
            {
                preSelectedLand.GenerateCrop();
            }
            else if (Input.GetKeyDown(KeyCode.R))
            {
                preSelectedLand.RemoveCrop();
            }
        }
    }

    private IEnumerator ConstructLand()
    {
        byte preGenerateType = 0;

        int layer = -1 - (+ 4 + 8 + 64 + 128);
        int preIndex = 0;

        int minX = 0, maxX = 0;
        int minZ = 0, maxZ = 0;

        Test.text = "Phase : " + preIndex;

        bool isActive = true;
        while (isActive)
        {
            yield return null;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                isActive = false;
            }
            else
            {
                switch (preGenerateType)
                {
                    case 0:
                        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                        if(Physics.Raycast(ray, out RaycastHit hit, 1000, layer))
                        {
                            if (Physics.Raycast(ray, out RaycastHit colliderHit, 1000, 8))
                                LandPoles[preIndex].position = colliderHit.transform.position;
                            else
                                LandPoles[preIndex].position = hit.point;
                        }

                        if (Input.GetMouseButtonDown(0))
                        {
                            preIndex++;
                            Test.text = "Phase : " + preIndex;
                            if (preIndex.Equals(LandPoles.Length - 1))
                                preGenerateType = 1;
                        }
                        else if (Input.GetMouseButtonDown(1) && RemoveLandPole())
                        {
                            isActive = false;
                        }
                        break;
                    case 1:
                        ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                        if (Physics.Raycast(ray, out hit, 1000, layer))
                        {
                            if (Physics.Raycast(ray, out RaycastHit colliderHit, 1000, 8))
                                LandPoles[preIndex].position = colliderHit.transform.position;
                            else
                                LandPoles[preIndex].position = hit.point;

                            float minFloatX = float.MaxValue, maxFloatX = 0;
                            float minFloatZ = float.MaxValue, maxFloatZ = 0;
                            Vector3 center = Vector3.zero;
                            for (int i = 0; i < LandPoles.Length; ++i)
                            {
                                minFloatX = Mathf.Min(LandPoles[i].position.x, minFloatX);
                                minFloatZ = Mathf.Min(LandPoles[i].position.z, minFloatZ);
                                maxFloatX = Mathf.Max(LandPoles[i].position.x, maxFloatX);
                                maxFloatZ = Mathf.Max(LandPoles[i].position.z, maxFloatZ);
                                center += LandPoles[i].position;
                            }
                            center /= LandPoles.Length;

                            Vector3[] allPolePosition = new Vector3[LandPoles.Length];
                            Vector3[] partPolePosition = new Vector3[LandPoles.Length];
                            for (int i = 0; i < partPolePosition.Length; ++i)
                            {
                                allPolePosition[i] = LandPoles[i].position;
                                partPolePosition[i] = Vector3.Lerp(LandPoles[i].position, center, PoleLerpScale);
                            }
                            LandAllCollider.CreateCube(allPolePosition);
                            LandPartCollider.CreateCube(partPolePosition);

                            minX = Mathf.FloorToInt(minFloatX);
                            minZ = Mathf.FloorToInt(minFloatZ);
                            maxX = Mathf.CeilToInt(maxFloatX);
                            maxZ = Mathf.CeilToInt(maxFloatZ);

                            int count = 0;
                            bool isLargeX = maxX - minX > maxZ - minZ;
                            if (isLargeX)
                            {
                                for (int z = minZ; z < maxZ; ++z)
                                {
                                    for (int x = minX; x < maxX; ++x)
                                    {
                                        Vector3Int position = new Vector3Int(x, 0, z);
                                        if (GetGenerateCropPosition(ref position, isLargeX) && (z & 1).Equals(1))
                                            count++;
                                    }
                                }
                            }
                            else
                            {
                                for (int x = minX; x < maxX; ++x)
                                {
                                    for (int z = minZ; z < maxZ; ++z)
                                    {
                                        Vector3Int position = new Vector3Int(x, 0, z);
                                        if (GetGenerateCropPosition(ref position, isLargeX) && (x & 1).Equals(1))
                                            count++;
                                    }
                                }
                            }
                            Test.text = count.ToString();
                        }

                        if (Input.GetMouseButtonDown(0))
                        {
                            CompleteLand(minX, maxX, minZ, maxZ, 1);
                            isActive = false;
                        }
                        else if (Input.GetMouseButtonDown(1))
                        {
                            preGenerateType = 0;
                            RemoveLandPole();
                        }
                        break;
                }
            }
        }

        Test.text = "None";
        isGeneratingLand = false;

        bool RemoveLandPole()
        {
            LandPoles[preIndex--].position = Vector3.zero;
            return preIndex < 0;
        }
    }

    private void CompleteLand(int minX, int maxX, int minZ, int maxZ, int changeIndex)
    {
        Transform[] landPoleArray = new Transform[LandPoles.Length];
        for (int i = 0; i < LandPoles.Length; ++i)
        {
            landPoleArray[i] = Instantiate(LandPoles[i], LandPoles[i].position, LandPoles[i].rotation);
            landPoleArray[i].gameObject.layer = 3;
            LandPoles[i].position = Vector3.zero;
        }

        int scaleX = maxX - minX, scaleZ = maxZ - minZ;
        TerrainGenerator.instance.PaintTerrainDirt(minX, minZ, scaleX, scaleZ, 64);

        bool isLargeX = scaleX > scaleZ;
        List<Vector3[]> cropPositionList = new List<Vector3[]>();
        if (isLargeX)
        {
            for (int z = minZ; z < maxZ; ++z)
            {
                List<Vector3> positionList = new List<Vector3>();
                for (int x = minX; x < maxX; ++x)
                {
                    Vector3Int position = new Vector3Int(x, 0, z);
                    if (GetGenerateCropPosition(ref position, isLargeX) && (z & 1).Equals(1))
                        positionList.Add(position);
                }

                if (positionList.Count.Equals(0)) continue;
                cropPositionList.Add(positionList.ToArray());
            }
        }
        else
        {
            for (int x = minX; x < maxX; ++x)
            {
                List<Vector3> positionList = new List<Vector3>();
                for (int z = minZ; z < maxZ; ++z)
                {
                    Vector3Int position = new Vector3Int(x, 0, z);
                    if (GetGenerateCropPosition(ref position, isLargeX) && (x & 1).Equals(1))
                        positionList.Add(position);
                }

                if (positionList.Count.Equals(0)) continue;
                cropPositionList.Add(positionList.ToArray());
            }
        }

        Land landClone = Instantiate(LandObject, LandAllCollider.transform.position, Quaternion.identity);
        landClone.InitializeObject();
        landClone.InitializeData(ref cropPositionList, landPoleArray, LandAllCollider.GeneratedMseh);

        LandAllCollider.RefreshCube();
        LandPartCollider.RefreshCube();
    }

    private bool GetGenerateCropPosition(ref Vector3Int position, bool isLargeX)
    {
        position.y = 100;
        bool isPossible = Physics.Raycast(position, Vector3.down, out RaycastHit hit, 200, 128);
        position = Vector3Int.RoundToInt(hit.point);
        return isPossible;
    }
}