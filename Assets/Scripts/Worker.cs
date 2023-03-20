using UnityEngine;
using UnityEngine.AI;

public class Worker : MonoBehaviour
{
    private NavMeshPath agentPath = null;
    private NavMeshAgent agent = null;

    public bool IsWalkable { get => agent.pathStatus == NavMeshPathStatus.PathComplete; }
    public bool IsArrived { get => agent.remainingDistance < agent.stoppingDistance; }
    public bool IsWaiting { get; private set; }

    public float PreWaitingTime { get; private set; }
    public float FullWaitingTime { get; private set; }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agentPath = new NavMeshPath();
    }


    private void Update()
    {
        if (IsWaiting)
        {
            if (PreWaitingTime < FullWaitingTime)
                PreWaitingTime += Time.deltaTime;
            else
            {
                IsWaiting = false;
                PreWaitingTime = FullWaitingTime;
            }
        }
    }

    public void SetActiveAgent(bool isActive, Vector3 position = default)
    {
        agent.enabled = isActive;
        if (isActive)
            transform.position = position;
        else
            transform.position = Vector3.zero;
    }

    public bool MovePosition(Vector3 position)
    {
        position.y = 100;
        if (!Physics.Raycast(position, Vector3.down, out RaycastHit hit, 200, 1)) return false;

        agent.CalculatePath(hit.point, agentPath);
        if (agentPath.status == NavMeshPathStatus.PathComplete)
        {
            agent.SetPath(agentPath);
            return true;
        }
        return false;
    }

    public void StartWaiting(float time)
    {
        PreWaitingTime = 0;
        FullWaitingTime = time;
        IsWaiting = true;
    }
}
