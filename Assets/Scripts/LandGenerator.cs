using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.UIElements;

public class LandGenerator : MonoBehaviour
{
    [SerializeField] private Text CropCount = null;
    [SerializeField] private Text PreWorkTime = null;
    [SerializeField] private Text FullWorkTime = null;

    [Header("Land Setting"), Space(10)]
    [SerializeField] private Transform[] LandPoles = null;
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
                        Debug.Log(preSelectedLand);
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

        int minX = 0, maxX = 0;
        int minZ = 0, maxZ = 0;
        Vector3 center = Vector3.zero;
        Vector3[] polePosition = new Vector3[LandPoles.Length];
        Vector3[] cropPolePosition = new Vector3[LandPoles.Length];

        CropCount.text = "Phase : " + preIndex;

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
                            CropCount.text = "Phase : " + preIndex;
                            if (preIndex.Equals(LandPoles.Length - 1))
                                preGenerateType = 1;
                        }
                        else if (Input.GetMouseButtonDown(1) && RemoveLandPole())
                        {
                            isActive = false;
                        }
                        break;
                    case 1:
                        int count = 0;

                        ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                        if (Physics.Raycast(ray, out hit, 1000, layer))
                        {
                            if (Physics.Raycast(ray, out RaycastHit colliderHit, 1000, 8))
                                LandPoles[preIndex].position = colliderHit.transform.position;
                            else
                                LandPoles[preIndex].position = hit.point;

                            float sumAngle = 0f;
                            for (int i = 0; i < 4; ++i)
                            {
                                int pole1 = i;
                                int pole2 = (i + 1) % 4;
                                int pole3 = (i + 2) % 4;
                                sumAngle += Vector3.Angle((LandPoles[pole1].position - LandPoles[pole2].position).normalized, (LandPoles[pole3].position - LandPoles[pole2].position).normalized);
                            }
                            if (sumAngle < 356)
                            {
                                CropCount.text = "Not Installable";
                                break;
                            }

                            float minFloatX = float.MaxValue, maxFloatX = 0;
                            float minFloatZ = float.MaxValue, maxFloatZ = 0;
                            center = Vector3.zero;
                            for (int i = 0; i < LandPoles.Length; ++i)
                            {
                                minFloatX = Mathf.Min(LandPoles[i].position.x, minFloatX);
                                minFloatZ = Mathf.Min(LandPoles[i].position.z, minFloatZ);
                                maxFloatX = Mathf.Max(LandPoles[i].position.x, maxFloatX);
                                maxFloatZ = Mathf.Max(LandPoles[i].position.z, maxFloatZ);
                                center += LandPoles[i].position;
                            }
                            center /= LandPoles.Length;

                            for (int i = 0; i < cropPolePosition.Length; ++i)
                            {
                                polePosition[i] = LandPoles[i].position;
                                cropPolePosition[i] = Vector3.Lerp(LandPoles[i].position, center, CropAreaScale);
                            }
                            LandCollider.CreateCube(polePosition);
                            LandCropCollider.CreateCube(cropPolePosition);

                            minX = Mathf.FloorToInt(minFloatX);
                            minZ = Mathf.FloorToInt(minFloatZ);
                            maxX = Mathf.CeilToInt(maxFloatX);
                            maxZ = Mathf.CeilToInt(maxFloatZ);
                            if (maxX - minX > maxZ - minZ)
                            {
                                for (int z = minZ; z < maxZ; ++z)
                                {
                                    for (int x = minX; x < maxX; ++x)
                                    {
                                        Vector3 position = new Vector3Int(x, 0, z);
                                        if (GetCropPosition(ref position) && (z & 1).Equals(1))
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
                                        Vector3 position = new Vector3Int(x, 0, z);
                                        if (GetCropPosition(ref position) && (x & 1).Equals(1))
                                            count++;
                                    }
                                }
                            }

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
                            CompleteLand(minX, maxX, minZ, maxZ, count, center, cropPolePosition);
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

        CropCount.text = "None";
        isGeneratingLand = false;

        bool RemoveLandPole()
        {
            LandPoles[preIndex--].position = Vector3.zero;
            return preIndex < 0;
        }
    }

    private void CompleteLand(int minX, int maxX, int minZ, int maxZ, int cropCount, Vector3 center, Vector3[] raisingPolePosition)
    {
        Transform[] landPoleArray = new Transform[LandPoles.Length];
        InstantiatePole(0);
        for (int i = 1; i < LandPoles.Length; ++i)
        {
            raisingPolePosition[i] = Vector3.Lerp(raisingPolePosition[i], raisingPolePosition[0], RaisingAreaScale); 
            InstantiatePole(i);
        }
        LandRaisingCollider.CreateCube(raisingPolePosition);

        int scaleX = maxX - minX, scaleZ = maxZ - minZ;
        TerrainGenerator.instance.PaintTerrainDirt(minX, minZ, scaleX, scaleZ, 64);

        List<Vector3> raisingPositionList = new List<Vector3>();
        List<Vector3[]> allCropPositionList = new List<Vector3[]>();
        if (scaleX > scaleZ)
        {
            for (int z = minZ; z < maxZ; ++z)
            {
                List<Vector3> cropPositionList = new List<Vector3>();
                for (int x = minX; x < maxX; ++x)
                {
                    Vector3 position = new Vector3(x, 0, z);
                    if (GetCropPosition(ref position) && (z & 1).Equals(1))
                    {
                        cropPositionList.Add(position);
                        if (GetRaisingPosition(ref position)) raisingPositionList.Add(position);
                    }
                }

                if (cropPositionList.Count.Equals(0)) continue;
                allCropPositionList.Add(cropPositionList.ToArray());
            }
        }
        else
        {
            for (int x = minX; x < maxX; ++x)
            {
                List<Vector3> cropPositionList = new List<Vector3>();
                for (int z = minZ; z < maxZ; ++z)
                {
                    Vector3 position = new Vector3(x, 0, z);
                    if (GetCropPosition(ref position) && (x & 1).Equals(1))
                    {
                        cropPositionList.Add(position);
                        if (GetRaisingPosition(ref position)) raisingPositionList.Add(position);
                    }
                }

                if (cropPositionList.Count.Equals(0)) continue;
                allCropPositionList.Add(cropPositionList.ToArray());
            }
        }

        Land landClone = Instantiate(LandObject, LandCollider.transform.position, Quaternion.identity);
        landClone.InitializeObject();
        landClone.InitializeData(ref allCropPositionList, ref raisingPositionList, landPoleArray, LandCollider.GeneratedMseh, cropCount);

        LandCollider.RefreshCube();
        LandCropCollider.RefreshCube();
        LandRaisingCollider.RefreshCube();

        void InstantiatePole(int index)
        {
            landPoleArray[index] = Instantiate(LandPoles[index], LandPoles[index].position, LandPoles[index].rotation);
            landPoleArray[index].gameObject.layer = 3;
            //LandPoles[index].position = Vector3.zero;
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