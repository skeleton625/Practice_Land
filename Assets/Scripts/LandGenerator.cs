using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;
using UnityEngine;

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
                            cropAngle = Vector3.Angle((polePosition[1] - polePosition[0]).normalized, Vector3.right);
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

                        float sumAngle = 0f;
                        for (int i = 0; i < 4; ++i)
                        {
                            int pole1 = i;
                            int pole2 = (i + 1) % 4;
                            int pole3 = (i + 2) % 4;
                            sumAngle += Vector3.Angle((polePosition[pole1] - polePosition[pole2]).normalized,
                                                      (polePosition[pole3] - polePosition[pole2]).normalized);
                        }
                        if (sumAngle < 356)
                        {
                            CropCount.text = "Not Installable";
                            break;
                        }

                        Vector3 center = Vector3.zero;
                        for (int i = 0; i < polePosition.Length; ++i) center += polePosition[i];
                        center /= polePosition.Length;

                        for (int i = 0; i < cropPolePosition.Length; ++i)
                        {
                            cropPolePosition[i] = Vector3.Lerp(polePosition[i], center, CropAreaScale);
                        }
                        LandCollider.CreateCube(polePosition);
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

                        if (rotX.y - rotX.x > rotZ.y - rotZ.x)
                        {
                            bool isEven = true;
                            for (float z = rotZ.x; z < rotZ.y; ++z)
                            {
                                isEven = !isEven;
                                for (float x = rotX.x; x < rotX.y; ++x)
                                {
                                    Vector3 position = Quaternion.Euler(0, cropAngle, 0) * new Vector3(x, 0, z) + polePosition[0];
                                    if (GetCropPosition(ref position) && isEven)
                                        count++;
                                }
                            }
                        }
                        else
                        {
                            bool isEven = true;
                            for (float x = rotX.x; x < rotX.y; ++x)
                            {
                                isEven = !isEven;
                                for (float z = rotZ.x; z < rotZ.y; ++z)
                                {
                                    Vector3 position = Quaternion.Euler(0, cropAngle, 0) * new Vector3(x, 0, z) + polePosition[0];
                                    if (GetCropPosition(ref position) && isEven)
                                        count++;
                                }
                            }
                        }

                        for (int i = 0; i < polePosition.Length; ++i)
                            rotatedPositions[i] = Quaternion.Euler(0, -cropAngle, 0) * (polePosition[i] - polePosition[0]);

                        if (count > 100)
                        {
                            CropCount.text = "Not Installable";
                            break;
                        }
                        else
                            CropCount.text = count.ToString();
                    }

                    if (Input.GetMouseButtonDown(0))
                    {
                        int fixMinX = 0, fixMaxX = 0;
                        int fixMinZ = 0, fixMaxZ = 0;
                        for (int i = 0; i < polePosition.Length; ++i)
                        {
                            fixMinX = Mathf.FloorToInt(Mathf.Min(polePosition[i].x, fixMinX));
                            fixMinZ = Mathf.FloorToInt(Mathf.Min(polePosition[i].z, fixMinZ));
                            fixMaxX = Mathf.CeilToInt(Mathf.Max(polePosition[i].x, fixMaxX));
                            fixMaxZ = Mathf.CeilToInt(Mathf.Max(polePosition[i].z, fixMaxZ));
                        }
                        TerrainGenerator.instance.PaintTerrainDirt(fixMinX, fixMinZ, fixMaxX - fixMinX, fixMaxZ - fixMinZ, 64);

                        CompleteLand(rotX, rotZ, polePosition, cropPolePosition, cropAngle, count);
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
    }

    private void CompleteLand(Vector2 rotX, Vector2 rotZ, Vector3[] polePosition, Vector3[] raisingPolePosition, float angle, int cropCount)
    {
        Transform[] landPoleArray = new Transform[polePosition.Length];

        Vector3 poleCenter = polePosition[0];
        InstantiatePole(0);
        for (int i = 1; i < polePosition.Length; ++i)
        {
            raisingPolePosition[i] = Vector3.Lerp(raisingPolePosition[i], raisingPolePosition[0], RaisingAreaScale);
            InstantiatePole(i);
        }
        LandRaisingCollider.CreateCube(raisingPolePosition);

        List<Vector3> raisingPositionList = new List<Vector3>();
        List<Vector3[]> allCropPositionList = new List<Vector3[]>();
        if (rotX.y - rotX.x > rotZ.y - rotZ.x)
        {
            bool isEven = true;
            for (float z = rotZ.x; z < rotZ.y; ++z)
            {
                isEven = !isEven;
                List<Vector3> cropPositionList = new List<Vector3>();
                for (float x = rotX.x; x < rotX.y; ++x)
                {
                    Vector3 position = Quaternion.Euler(0, angle, 0) * new Vector3(x, 0, z) + poleCenter;
                    if (GetCropPosition(ref position) && isEven)
                    {
                        cropPositionList.Add(position);
                        if (GetRaisingPosition(ref position)) raisingPositionList.Add(position);
                    }
                }
                if (cropPositionList.Count > 0) allCropPositionList.Add(cropPositionList.ToArray());
            }
        }
        else
        {
            bool isEven = true;
            for (float x = rotX.x; x < rotX.y; ++x)
            {
                isEven = !isEven;
                List<Vector3> cropPositionList = new List<Vector3>();
                for (float z = rotZ.x; z < rotZ.y; ++z)
                {
                    Vector3 position = Quaternion.Euler(0, angle, 0) * new Vector3(x, 0, z) + poleCenter;
                    if (GetCropPosition(ref position) && isEven)
                    {
                        cropPositionList.Add(position);
                        if (GetRaisingPosition(ref position)) raisingPositionList.Add(position);
                    }
                }
                if (cropPositionList.Count > 0) allCropPositionList.Add(cropPositionList.ToArray());
            }
        }

        Land landClone = Instantiate(LandObject, LandCollider.transform.position, Quaternion.identity);
        landClone.InitializeObject();
        landClone.InitializeData(ref allCropPositionList, ref raisingPositionList, landPoleArray, LandCollider.GeneratedMseh, cropCount);

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
                    Transform pole = Instantiate(PoleObject, nextPosition, LandPoles[i].rotation);
                    wall.SetParent(landClone.transform);
                    pole.SetParent(landClone.transform);
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

        LandCollider.RefreshCube();
        LandCropCollider.RefreshCube();
        LandRaisingCollider.RefreshCube();

        void InstantiatePole(int index)
        {
            landPoleArray[index] = Instantiate(PoleObject, polePosition[index], LandPoles[index].rotation);
            landPoleArray[index].gameObject.layer = 3;
        }
    }

    private bool GetCropPosition(ref Vector3 position)
    {
        position.y = 100;
        RaycastHit[] hits = Physics.RaycastAll(position, Vector3.down, 200, 64);

        bool isPossible = false;
        for (int i = 0; i < hits.Length; ++i)
            if (hits[i].transform.CompareTag("CropArea"))
            {
                isPossible = true;
                position = hits[i].point;
                break;
            }
        return isPossible;
    }

    private bool GetRaisingPosition(ref Vector3 position)
    {
        position.y = 100;
        RaycastHit[] hits = Physics.RaycastAll(position, Vector3.down, 200, 64);

        bool isPossible = false;
        for (int i = 0; i < hits.Length; ++i)
            if (hits[i].transform.CompareTag("RaisingArea"))
            {
                isPossible = true;
                position = hits[i].point;
                break;
            }
        return isPossible;
    }
}