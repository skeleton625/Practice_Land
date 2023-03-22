using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Erandom = UnityEngine.Random;

public class Land : MonoBehaviour
{
    [Serializable]
    public enum ScheduleType { None = 0, Plowing = 1, Planting = 2, Raising = 3, Seeding = 4, Weeding = 5, Harvest = 6 };

    [Header("Land Collider Setting"), Space(10)]
    [SerializeField] private MeshCollider LandUIMesh = null;
    [SerializeField] private MeshCollider LandColliderMesh = null;

    private int moveDirection = 1;
    private int preCropIndex = 0;

    [Header("Work Setting"), Space(10)]
    [SerializeField] private float[] EachScheduleTime = null;

    private int preCropWorkCount = 0;

    private int workerState = 0;
    private float workerTime = 0f;
    private Vector3 workPosition = Vector3.zero;
    private Worker worker = null;

    private Transform[] landPoleArray = null;
    private GameObject[] cropObjectList = null;

    private List<Vector3> raisingPositionList = null;
    private List<Vector3> allCropPositionList = null;

    private Action<int, int> WorkTimer = null;

    private ScheduleType preScheduleType;

    #region Land Initialize Functions
    public void InitializeData(ref List<Vector3> allCropPositionList, ref List<Vector3> raisingPositionList, Transform[] landPoleArray, Mesh uiMesh, Mesh colliderMesh)
    {
        this.raisingPositionList = raisingPositionList;
        this.allCropPositionList = allCropPositionList;
        this.landPoleArray = landPoleArray;

        cropObjectList = new GameObject[allCropPositionList.Count];
        for (int i = 0; i < allCropPositionList.Count; ++i)
        {
            Vector3 position = allCropPositionList[i];
            position.y = 100;
            if (Physics.Raycast(position, Vector3.down, out RaycastHit hit, 200, 1))
            {
                cropObjectList[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                cropObjectList[i].transform.position = hit.point;
                cropObjectList[i].transform.SetParent(transform);
            }
        }

        for (int i = 0; i < landPoleArray.Length; ++i)
            landPoleArray[i].SetParent(transform);

        LandUIMesh.sharedMesh = uiMesh;
        LandColliderMesh.sharedMesh = colliderMesh;

        preScheduleType = ScheduleType.None;
        moveDirection = 1;
        preCropIndex = 0;
    }

    public void DestroyLand()
    {
        int minX = int.MaxValue, maxX = 0;
        int minZ = int.MaxValue, maxZ = 0;
        for (int i = 0; i < landPoleArray.Length; ++i)
        {
            minX = Mathf.FloorToInt(Mathf.Min(landPoleArray[i].position.x, minX));
            minZ = Mathf.FloorToInt(Mathf.Min(landPoleArray[i].position.z, minZ));
            maxX = Mathf.CeilToInt(Mathf.Max(landPoleArray[i].position.x, maxX));
            maxZ = Mathf.CeilToInt(Mathf.Max(landPoleArray[i].position.z, maxZ));
        }
        TerrainGenerator.instance.PaintTerrainDefault(minX, minZ, maxX - minX, maxZ - minZ, 64, transform.GetHashCode());

        Destroy(gameObject);
    }
    #endregion

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

    private void Update()
    {
        if (worker == null) return;

        switch (workerState)
        {
            case 0:
                if (GetWorkPosition(ref workPosition))
                {
                    workerState = 1;
                    worker.MovePosition(workPosition);
                }
                break;
            case 1:
                if (worker.IsArrived)
                {
                    workerState = 2;
                    worker.StartWaiting(workerTime);
                }
                break;
            case 2:
                if (worker.IsWaiting) break;

                preCropWorkCount++;
                WorkTimer.Invoke(preCropWorkCount, cropObjectList.Length);
                if (preCropWorkCount < cropObjectList.Length)
                    workerState = 0;
                else
                    workerState = 3;
                break;
            case 3:
                worker.SetActiveAgent(false);
                worker = null;
                break;
        }
    }

    public void InitializeSchedule(ScheduleType type, Action<int, int> action)
    {
        WorkTimer = action;

        preScheduleType = type;
        preCropWorkCount = 0;
        workerTime = EachScheduleTime[(int)type];

        switch (type)
        {
            case ScheduleType.Planting:
            case ScheduleType.Plowing:
            case ScheduleType.Seeding:
            case ScheduleType.Harvest:
                if ((Erandom.Range(0, 2) & 1).Equals(0))
                {
                    moveDirection = 1;
                    preCropIndex = 0;
                }
                else
                {
                    moveDirection = -1;
                    preCropIndex = allCropPositionList.Count - 1;
                }
                break;
        }
    }

    public void InsertWorker(Worker agent)
    {
        worker = agent;
        workerState = 0;
    }

    private bool GetWorkPosition(ref Vector3 workPosition)
    {
        switch (preScheduleType)
        {
            case ScheduleType.Weeding:
                workPosition = allCropPositionList[Erandom.Range(0, allCropPositionList.Count)];
                return true;
            case ScheduleType.Planting:
            case ScheduleType.Plowing:
            case ScheduleType.Seeding:
            case ScheduleType.Harvest:
                if (preCropIndex > -1 && preCropIndex < allCropPositionList.Count)
                {
                    preCropIndex += moveDirection;
                    return true;
                }
                return false;
            case ScheduleType.Raising:
                workPosition = raisingPositionList[Erandom.Range(0, raisingPositionList.Count)];
                return true;
        }
        return false;
    }
}
