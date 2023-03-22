using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;
using UnityEngine;
using static UnityEditor.Experimental.AssetDatabaseExperimental.AssetDatabaseCounters;
using System.Reflection;

public class LandGenerator : MonoBehaviour
{
    [SerializeField] private Text CropCount = null;
    [SerializeField] private Text PreWorkTime = null;
    [SerializeField] private Text FullWorkTime = null;

    [Header("Land Setting"), Space(10)]
    [SerializeField] private Transform[] LandPoles = null;
    [SerializeField] private Transform PoleObject = null;
    [SerializeField] private Transform WallObject = null;
    [SerializeField] private float CropAreaScale = 0f;
    [SerializeField] private float RaisingAreaScale = 0f;
    [SerializeField] private CubeGenerator LandCollider = null; // Crop 전용 Collider도 추가 필요
    [SerializeField] private CubeGenerator LandCropCollider = null;
    [SerializeField] private CubeGenerator LandRaisingCollider = null;
    [SerializeField] private TagCollider[] LandWallCollider = null;
    [SerializeField] private Land LandObject = null;

    private bool isGeneratingLand = false;
    private Land preSelectedLand = null;

    [Header("Work Setting"), Space(10)]
    [SerializeField] private Worker Worker = null;
    [SerializeField] private Land.ScheduleType ScheduleType = default;

    private void Update()
    {
        if (!isGeneratingLand)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                RaycastHit[] hits = Physics.RaycastAll(ray, 1000, 64);
                preSelectedLand = null;
                for (int i = 0; i < hits.Length; ++i)
                {
                    if (hits[i].transform.CompareTag("Land"))
                    {
                        preSelectedLand = hits[i].transform.GetComponent<Land>();
                        break;
                    }
                }
                Debug.Log(preSelectedLand);
            }

            if (Input.GetKeyDown(KeyCode.Q))
            {
                isGeneratingLand = true;
                StartCoroutine(ConstructLand());
            }

            if (preSelectedLand == null) return;
            if (Input.GetKeyDown(KeyCode.E))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 1000, 1))
                    Worker.SetActiveAgent(true, hit.point);

                preSelectedLand.InitializeSchedule(ScheduleType, RefreshTimer);
                preSelectedLand.InsertWorker(Worker);
            }
            else if (Input.GetKeyDown(KeyCode.R))
            {
                preSelectedLand.RemoveCrop();
            }
            else if (Input.GetKeyDown(KeyCode.P))
            {
                preSelectedLand.DestroyLand();
            }
        }
    }

    private void RefreshTimer(int preTimer, int fullTimer)
    {
        PreWorkTime.text = string.Format("{0}", preTimer);
        FullWorkTime.text = string.Format("{0}", fullTimer);
    }

    private IEnumerator ConstructLand()
    {
        byte preGenerateType = 0;

        int layer = -1 - (4 + 8 + 64);
        int preIndex = 0;

        Vector2 rotX = Vector2.zero;
        Vector2 rotZ = Vector2.zero;
        float cropAngle = 0f;

        Vector3 center = Vector3.zero;
        Vector3 prePosition = Vector3.zero;
        Vector3[] polePosition = new Vector3[LandPoles.Length];
        Vector3[] cropPolePosition = new Vector3[LandPoles.Length];
        Vector3[] rotatedPositions = new Vector3[LandPoles.Length];

        CropCount.text = "Phase : " + preIndex;

        bool isActive = true;
        while (isActive)
        {
            yield return null;

            if (Input.GetKeyDown(KeyCode.Escape))
                break;

            switch (preGenerateType)
            {
                case 0:
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    if (Physics.Raycast(ray, out RaycastHit hit, 1000, layer))
                    {
                        if (Physics.Raycast(ray, out RaycastHit colliderHit, 1000, 8))
                        {
                            LandPoles[0].position = colliderHit.transform.position;
                            prePosition = colliderHit.transform.position;
                        }
                        else
                        {
                            LandPoles[0].position = hit.point;
                            prePosition = hit.point;
                        }
                    }

                    if (Input.GetMouseButtonDown(0))
                    {
                        preGenerateType = 1;
                        polePosition[preIndex++] = prePosition;
                        CropCount.text = "Phase : " + preIndex;
                    }
                    else if (Input.GetMouseButtonDown(1))
                    {
                        isActive = false;
                    }
                    break;
                case 1:
                    ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    if (Physics.Raycast(ray, out hit, 1000, layer))
                    {
                        if (Physics.Raycast(ray, out RaycastHit colliderHit, 1000, 8))
                        {
                            prePosition = colliderHit.transform.position;
                        }
                        else
                        {
                            prePosition = hit.point;
                        }
                        ChangeWall(preIndex - 1, polePosition[preIndex - 1], prePosition);
                    }

                    if (Input.GetMouseButtonDown(0))
                    {
                        polePosition[preIndex++] = prePosition;
                        CropCount.text = "Phase : " + preIndex;
                        if (preIndex.Equals(polePosition.Length - 1))
                        {
                            preGenerateType = 2;
                            Vector3 direction = (polePosition[1] - polePosition[0]).normalized;
                            cropAngle = Vector3.Angle(direction, Vector3.right);
                        }
                    }
                    else if (Input.GetMouseButtonDown(1))
                    {
                        RemoveLandPole(preIndex - 1);
                        polePosition[preIndex--] = Vector3.zero;
                        if (preIndex.Equals(0)) preGenerateType = 0;
                    }
                    break;
                case 2:
                    if (Input.GetMouseButtonDown(1))
                    {
                        preGenerateType = 1;
                        RemoveLandPole(preIndex - 1);
                        RemoveLandPole(preIndex);
                        polePosition[preIndex--] = Vector3.zero;

                        LandCropCollider.RefreshCube();
                        break;
                    }

                    int count = 0;
                    ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    if (Physics.Raycast(ray, out hit, 1000, layer))
                    {
                        if (Physics.Raycast(ray, out RaycastHit colliderHit, 1000, 8))
                        {
                            prePosition = colliderHit.transform.position;
                        }
                        else
                        {
                            prePosition = hit.point;
                        }

                        polePosition[preIndex] = prePosition;
                        ChangeWall(preIndex - 1, polePosition[preIndex - 1], prePosition);
                        ChangeWall(preIndex, polePosition[0], prePosition);

                        center = Vector3.zero;
                        for (int i = 0; i < polePosition.Length; ++i) center += polePosition[i];
                        center /= polePosition.Length;

                        for (int i = 0; i < cropPolePosition.Length; ++i)
                            cropPolePosition[i] = Vector3.Lerp(center, polePosition[i], CropAreaScale);
                        for (int i = 0; i < polePosition.Length; ++i)
                            rotatedPositions[i] = Quaternion.Euler(0, cropAngle, 0) * (polePosition[i] - polePosition[0]);
                        LandCropCollider.CreateCube(cropPolePosition);

                        rotX.x = float.MaxValue;
                        rotZ.x = float.MaxValue;
                        rotX.y = 0;
                        rotZ.y = 0;
                        for (int i = 0; i < polePosition.Length; ++i)
                        {
                            rotX.x = Mathf.Min(rotatedPositions[i].x, rotX.x);
                            rotZ.x = Mathf.Min(rotatedPositions[i].z, rotZ.x);
                            rotX.y = Mathf.Max(rotatedPositions[i].x, rotX.y);
                            rotZ.y = Mathf.Max(rotatedPositions[i].z, rotZ.y);
                        }

                        bool isPossible = GetCropCounts(ref count);

                        float sumAngle = 0f;
                        for (int i = 0; i < 4; ++i)
                        {
                            int pole1 = i;
                            int pole2 = (i + 1) % 4;
                            int pole3 = (i + 2) % 4;
                            sumAngle += Vector3.Angle((polePosition[pole1] - polePosition[pole2]).normalized,
                                                      (polePosition[pole3] - polePosition[pole2]).normalized);
                        
                            if (LandWallCollider[i].IsConflict)
                            {
                                isPossible = false;
                                break;
                            }
                        }

                        // if (isPossible && count <= 100 && sumAngle > 356)
                        if (isPossible)
                        {
                            CropCount.text = count.ToString();
                        }
                        else
                        {
                            CropCount.text = "Not Installable";
                            break;
                        }

                    }

                    if (Input.GetMouseButtonDown(0))
                    {
                        Vector3[] largePolePosition = new Vector3[polePosition.Length];
                        for (int i = 0; i < largePolePosition.Length; ++i)
                            largePolePosition[i] = polePosition[i] + (polePosition[i] - center).normalized;
                        LandCollider.CreateCube(largePolePosition);

                        int fixMinX = int.MaxValue, fixMaxX = 0;
                        int fixMinZ = int.MaxValue, fixMaxZ = 0;
                        for (int i = 0; i < polePosition.Length; ++i)
                        {
                            fixMinX = Mathf.FloorToInt(Mathf.Min(polePosition[i].x, fixMinX));
                            fixMinZ = Mathf.FloorToInt(Mathf.Min(polePosition[i].z, fixMinZ));
                            fixMaxX = Mathf.CeilToInt(Mathf.Max(polePosition[i].x, fixMaxX));
                            fixMaxZ = Mathf.CeilToInt(Mathf.Max(polePosition[i].z, fixMaxZ));
                        }
                        TerrainGenerator.instance.PaintTerrainDirt(fixMinX - 2, fixMinZ - 2, fixMaxX - fixMinX + 1, fixMaxZ - fixMinZ + 1, 64);

                        LandCollider.CreateCube(polePosition);
                        CompleteLand(rotX, rotZ, polePosition, cropPolePosition, cropAngle);
                        isActive = false;
                    }
                    break;
            }
        }

        CropCount.text = "None";
        isGeneratingLand = false;

        for (int i = 0; i < LandPoles.Length; ++i)
            RemoveLandPole(i);

        void RemoveLandPole(int poleIndex)
        {
            if (poleIndex > -1)
            {
                LandPoles[poleIndex].position = Vector3.zero;
                LandPoles[poleIndex].localScale = Vector3.one;
            }
        }

        void ChangeWall(int index, Vector3 start, Vector3 end)
        {
            LandPoles[index].position = Vector3.Lerp(start, end, .5f);
            LandPoles[index].localScale = new Vector3(.25f, 1, (end - start).magnitude);
            LandPoles[index].LookAt(prePosition);
        }

        bool GetCropCounts(ref int count)
        {
            bool isEven = true;
            if ((polePosition[1] - polePosition[0]).magnitude > (polePosition[2] - polePosition[1]).magnitude)
            {
                for (float z = rotZ.x; z < rotZ.y; ++z)
                {
                    isEven = !isEven;
                    if (isEven) continue;

                    for (float x = rotX.x; x < rotX.y; ++x)
                    {
                        Vector3 position = Quaternion.Euler(0, -cropAngle, 0) * new Vector3(x, 0, z) + polePosition[0];
                        if (!GetCropPosition(ref position)) continue;

                        count++;
                        position.y = 100;
                        if (Physics.Raycast(position, Vector3.down, out RaycastHit hit, 200, 64) && hit.transform.CompareTag("BuildingCollider")) return false;
                    }
                }
            }
            else
            {
                for (float x = rotX.x; x < rotX.y; ++x)
                {
                    isEven = !isEven;
                    if (isEven) continue;

                    for (float z = rotZ.x; z < rotZ.y; ++z)
                    {
                        Vector3 position = Quaternion.Euler(0, -cropAngle, 0) * new Vector3(x, 0, z) + polePosition[0];
                        if (!GetCropPosition(ref position)) continue;

                        count++;
                        position.y = 100;
                        if (Physics.Raycast(position, Vector3.down, out RaycastHit hit, 200, 64) && hit.transform.CompareTag("BuildingCollider")) return false;
                    }
                }
            }
            return true;
        }
    }

    private void CompleteLand(Vector2 rotX, Vector2 rotZ, Vector3[] polePosition, Vector3[] raisingPolePosition, float cropAngle)
    {
        // GENERATE CROP
        Transform[] landPoleArray = new Transform[polePosition.Length];
        for (int i = 0; i < polePosition.Length; ++i)
        {
            raisingPolePosition[i] = Vector3.Lerp(raisingPolePosition[0], raisingPolePosition[i], RaisingAreaScale);
            landPoleArray[i] = Instantiate(PoleObject, polePosition[i], LandPoles[i].rotation);
            landPoleArray[i].gameObject.layer = 3;
        }
        LandRaisingCollider.CreateCube(raisingPolePosition);

        bool isEven = true;
        List<Vector3> raisingPositionList = new List<Vector3>();
        List<Vector3> allCropPositionList = new List<Vector3>();
        if ((polePosition[1] - polePosition[0]).magnitude > (polePosition[2] - polePosition[1]).magnitude)
        {
            for (float z = rotZ.x; z < rotZ.y; ++z)
            {
                for (float x = rotX.x; x < rotX.y; ++x)
                {
                    Vector3 position = Quaternion.Euler(0, -cropAngle, 0) * new Vector3(x, 0, z) + polePosition[0];
                    GameObject clone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    clone.transform.position = position;

                    //if (!GetCropPosition(ref position)) continue;

                    allCropPositionList.Add(position);
                    if (GetRaisingPosition(ref position)) raisingPositionList.Add(position);
                }
            }
        }
        else
        {
            for (float x = rotX.x; x < rotX.y; ++x)
            {
                for (float z = rotZ.x; z < rotZ.y; ++z)
                {
                    Vector3 position = Quaternion.Euler(0, -cropAngle, 0) * new Vector3(x, 0, z) + polePosition[0];
                    GameObject clone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    clone.transform.position = position;

                    //if (!GetCropPosition(ref position)) continue;

                    allCropPositionList.Add(position);
                    if (GetRaisingPosition(ref position)) raisingPositionList.Add(position);
                }
            }
        }

        // GENERATE LAND
        Land landClone = Instantiate(LandObject, LandCollider.transform.position, Quaternion.identity);
        landClone.InitializeData(ref allCropPositionList, ref raisingPositionList, landPoleArray, LandCollider.GeneratedMseh, LandCropCollider.GeneratedMseh);
        LandCollider.RefreshCube();
        LandCropCollider.RefreshCube();
        LandRaisingCollider.RefreshCube();

        // GENERATE LAND WALL
        for (int i = 0; i < polePosition.Length; ++i)
        {
            int nextIndex = (i + 1) % polePosition.Length;
            float wallLength = WallObject.localScale.z;
            float allLength = (polePosition[nextIndex] - polePosition[i]).magnitude;
            Vector3 direction = (polePosition[nextIndex] - polePosition[i]).normalized;
            Vector3 prePosition = polePosition[i];
            Vector3 nextPosition = polePosition[i] + wallLength * direction;
            while (allLength > 0)
            {
                if (allLength >= wallLength)
                {
                    Transform wall = Instantiate(WallObject, Vector3.Lerp(prePosition, nextPosition, .5f), LandPoles[i].rotation);
                    wall.SetParent(landClone.transform);
                    prePosition = nextPosition;
                    allLength -= wallLength;
                    if (allLength < wallLength) nextPosition = polePosition[nextIndex];
                    else nextPosition += wallLength * direction;
                }
                else
                {
                    Transform wall = Instantiate(WallObject, Vector3.Lerp(prePosition, nextPosition, .5f), LandPoles[i].rotation);
                    wall.transform.localScale = new Vector3(1, 1, allLength);
                    wall.SetParent(landClone.transform);
                    allLength = 0;
                }
            }
        }

        bool GetRaisingPosition(ref Vector3 position)
        {
            position.y = 100;
            RaycastHit[] hits = Physics.RaycastAll(position, Vector3.down, 200, 64);

            for (int i = 0; i < hits.Length; ++i)
            {
                if (hits[i].transform.CompareTag("RaisingArea"))
                {
                    position = hits[i].point;
                    return true;
                }
            }
            return false;
        }
    }

    private bool GetCropPosition(ref Vector3 position)
    {
        position.y = 100;
        RaycastHit[] hits = Physics.RaycastAll(position, Vector3.down, 200, 64);

        for (int i = 0; i < hits.Length; ++i)
        {
            if (hits[i].transform.CompareTag("CropArea"))
            {
                position = hits[i].point;
                return true;
            }
        }
        return false;
    }
}