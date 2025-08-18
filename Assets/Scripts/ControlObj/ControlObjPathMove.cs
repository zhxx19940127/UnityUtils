using UnityEngine;

public class ControlObjPathMove : MonoBehaviour
{
    public bool IsMoving = true; // 是否允许移动

    public Transform Target;

    [Header("路径设置")] public Transform[] waypoints; // 路径点数组（通过Inspector拖入）
    public float moveSpeed = 5f; // 移动速度
    public float rotationSpeed = 5f; // 旋转速度
    public float arrivalThreshold = 0.1f; // 到达判定阈值

    [Header("调试")] public bool drawGizmos = true; // 是否绘制路径
    public Color gizmoColor = Color.green; // 路径颜色

    private int currentWaypointIndex = 0; // 当前目标点索引

    void Start()
    {
    }

    void Update()
    {
        if (Target == null)
        {
            Debug.LogWarning("目标对象未设置！");
            return;
        }

        if (waypoints == null || waypoints.Length == 0)
        {
            Debug.LogWarning("未设置路径点！");
        }
        else
        {
            if (IsMoving)
            {
                MoveTowardsWaypoint();
                RotateTowardsWaypoint();
                CheckWaypointArrival();
            }
        }
    }

    void MoveTowardsWaypoint()
    {
        // 计算移动方向和距离
        Vector3 direction = waypoints[currentWaypointIndex].position - Target.position;
        Target.Translate(direction.normalized * moveSpeed * Time.deltaTime, Space.World);
    }

    void RotateTowardsWaypoint()
    {
        // 平滑旋转朝向目标点
        if (waypoints[currentWaypointIndex] != null)
        {
            Vector3 lookDirection = waypoints[currentWaypointIndex].position - Target.position;
            if (lookDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                Target.rotation = Quaternion.Slerp(
                    Target.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
            }
        }
    }

    void CheckWaypointArrival()
    {
        // 检查是否到达当前目标点
        float distanceToTarget = Vector3.Distance(
            Target.position,
            waypoints[currentWaypointIndex].position
        );

        if (distanceToTarget < arrivalThreshold)
        {
            // 更新到下一个目标点（循环）
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
        }
    }

    // 绘制路径Gizmos（仅在编辑器中可见）
    void OnDrawGizmos()
    {
        if (!drawGizmos || waypoints == null || waypoints.Length < 2) return;

        Gizmos.color = gizmoColor;

        // 绘制路径线
        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            if (waypoints[i] != null && waypoints[i + 1] != null)
            {
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
            }
        }

        // 连接最后一个点和第一个点形成循环
        if (waypoints.Length > 1 && waypoints[0] != null && waypoints[waypoints.Length - 1] != null)
        {
            Gizmos.DrawLine(waypoints[waypoints.Length - 1].position, waypoints[0].position);
        }

        // 绘制路径点
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] != null)
            {
                Gizmos.DrawSphere(waypoints[i].position, 0.2f);
            }
        }
    }
}