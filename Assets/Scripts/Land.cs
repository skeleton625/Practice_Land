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
    [SerializeField] private MeshCollider landMeshCollider = null;

    private int rowDirection = 1;
    private int colDirection = 1;
    private int preCropRow = 0;
    private int preCropColumn = 0;

    [Header("Work Setting"), Space(10)]
    [SerializeField] private float RaisingRate = 0f;
    [SerializeField] private float[] EachScheduleTime = null;

    private int preCropWorkCount = 0;

    private int workerState = 0;
    private float workerTime = 0f;
    private Vector3 workPosition = Vector3.zero;
    private Worker worker = null;

    private Transform[] landPoleArray = null;
    private GameObject[] cropObjectList = null;

    private List<Vector3> raisingPositionList = null;
    private List<Vector3[]> cropPositionList = null;

    private Action<int, int> WorkTimer = null;

    private ScheduleType preScheduleType;

    public void InitializeObject()
    {
        cropPositionList = new List<Vector3[]>();
    }

    public void InitializeData(ref List<Vector3[]> cropPositionList, ref List<Vector3> raisingPositionList, Transform[] landPoleArray, Mesh colliderMesh, int cropCount)
    {
        this.raisingPositionList = raisingPositionList;
        this.cropPositionList = cropPositionList;
        this.landPoleArray = landPoleArray;

        int index = 0;
        cropObjectList = new GameObject[cropCount];
        for (int row = 0; row < cropPositionList.Count; ++row)
        {
            Vector3[] positionArray = cropPositionList[row];
            for (int col = 0; col < positionArray.Length; ++col)
            {
                positionArray[col].y = 100;
                if (Physics.Raycast(positionArray[col], Vector3.down, out RaycastHit hit, 200, 1))
                {
                    cropObjectList[index] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    cropObjectList[index++].transform.position = hit.point;
                }
            }    
        }

        for (int i = 0; i < landPoleArray.Length; ++i)
            landPoleArray[i].SetParent(transform);

        landMeshCollider.sharedMesh = colliderMesh;

        preScheduleType = ScheduleType.None;
        rowDirection = 1;
        colDirection = 1;
        preCropRow = 0;
        preCropColumn = 0;
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
                rowDirection = 1;
                colDirection = 1;
                preCropRow = 0;
                preCropColumn = 0;
                if ((Erandom.Range(0, 2) & 1).Equals(0))
                {
                    rowDirection = 1;
                    colDirection = 1;
                    preCropRow = 0;
                    preCropColumn = 0;
                }
                else
                {
                    rowDirection = -1;
                    colDirection = -1;
                    preCropRow = cropPositionList.Count - 1;
                    preCropColumn = cropPositionList[preCropRow].Length - 1;
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
                int randomRow = Erandom.Range(0, cropPositionList.Count);
                int randomColumn = Erandom.Range(0, cropPositionList[randomRow].Length);
                workPosition = cropPositionList[randomRow][randomColumn];
                return true;
            case ScheduleType.Planting:
            case ScheduleType.Plowing:
            case ScheduleType.Seeding:
            case ScheduleType.Harvest:
                Vector3[] positionArray = cropPositionList[preCropRow];
                workPosition = positionArray[preCropColumn];
                preCropColumn += colDirection;

                if (preCropColumn < 0 || positionArray.Length <= preCropColumn)
                {
                    preCropRow += rowDirection;
                    if (colDirection < 0)
                    {
                        colDirection = 1;
                        preCropColumn = 0;
                    }
                    else if (preCropRow > -1)
                    {
                        colDirection = -1;
                        preCropColumn = cropPositionList[preCropRow].Length - 1;
                    }
                }
                return true;
            case ScheduleType.Raising:
                workPosition = raisingPositionList[Erandom.Range(0, raisingPositionList.Count)];
                return true;
        }
        return false;
    }
}
