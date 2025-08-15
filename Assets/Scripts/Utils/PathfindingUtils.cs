namespace GameObjectToolkit
{
    using UnityEngine;
    using UnityEngine.AI;
    using System.Collections.Generic;

    /// <summary>
    /// Unity 路径查找工具类
    /// 提供基于 NavMesh 和 A* 算法的路径查找功能
    /// </summary>
    public static class PathfindingUtils
    {
        #region NavMesh 路径查找

        /// <summary>
        /// 计算 NavMesh 路径
        /// </summary>
        /// <param name="start">起始位置</param>
        /// <param name="end">目标位置</param>
        /// <param name="areaMask">可通行区域掩码</param>
        /// <returns>路径点列表，如果失败返回null</returns>
        public static List<Vector3> CalculateNavMeshPath(Vector3 start, Vector3 end, int areaMask = NavMesh.AllAreas)
        {
            NavMeshPath path = new NavMeshPath();
            if (NavMesh.CalculatePath(start, end, areaMask, path))
            {
                return new List<Vector3>(path.corners);
            }

            Debug.LogWarning("PathfindingUtils: 无法计算 NavMesh 路径");
            return null;
        }

        /// <summary>
        /// 检查两点间是否有直达的 NavMesh 路径
        /// </summary>
        /// <param name="start">起始位置</param>
        /// <param name="end">目标位置</param>
        /// <param name="maxDistance">最大检查距离</param>
        /// <param name="areaMask">可通行区域掩码</param>
        /// <returns>是否存在直达路径</returns>
        public static bool HasDirectPath(Vector3 start, Vector3 end, float maxDistance = Mathf.Infinity,
            int areaMask = NavMesh.AllAreas)
        {
            if (Vector3.Distance(start, end) > maxDistance)
            {
                return false;
            }

            NavMeshHit hit;
            if (!NavMesh.Raycast(start, end, out hit, areaMask))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 获取随机可到达的 NavMesh 点
        /// </summary>
        /// <param name="center">中心点</param>
        /// <param name="radius">搜索半径</param>
        /// <param name="areaMask">可通行区域掩码</param>
        /// <returns>随机点坐标</returns>
        public static Vector3 GetRandomReachablePoint(Vector3 center, float radius, int areaMask = NavMesh.AllAreas)
        {
            // 使用NavMeshHit作为输出参数
            NavMeshHit hit;
    
            // 最多尝试10次
            for (int i = 0; i < 10; i++)
            {
                // 在半径范围内生成随机点
                Vector3 randomPos = center + Random.insideUnitSphere * radius;
        
                // 使用正确的SamplePosition方法签名
                if (NavMesh.SamplePosition(randomPos, out hit, radius, areaMask))
                {
                    return hit.position;  // 返回找到的NavMesh上的位置
                }
            }

            Debug.LogWarning("PathfindingUtils: 无法找到可到达的随机点，返回中心点");
            return center;
        }

        #endregion

        #region A* 路径查找

        /// <summary>
        /// A* 节点类
        /// </summary>
        public class AStarNode
        {
            public Vector3 Position { get; set; }
            public float GCost { get; set; } // 从起点到当前节点的实际代价
            public float HCost { get; set; } // 从当前节点到终点的启发式估计代价
            public float FCost => GCost + HCost; // 总代价
            public AStarNode Parent { get; set; }

            public AStarNode(Vector3 position)
            {
                Position = position;
            }
        }

        /// <summary>
        /// 使用 A* 算法计算路径
        /// </summary>
        /// <param name="start">起始位置</param>
        /// <param name="end">目标位置</param>
        /// <param name="grid">寻路网格</param>
        /// <param name="maxIterations">最大迭代次数</param>
        /// <returns>路径点列表，如果失败返回null</returns>
        public static List<Vector3> CalculateAStarPath(Vector3 start, Vector3 end, PathfindingGrid grid,
            int maxIterations = 1000)
        {
            if (grid == null)
            {
                Debug.LogError("PathfindingUtils: 寻路网格为空");
                return null;
            }

            // 转换世界坐标到网格坐标
            if (!grid.WorldToGrid(start, out int startX, out int startY) ||
                !grid.WorldToGrid(end, out int endX, out int endY))
            {
                Debug.LogError("PathfindingUtils: 起点或终点不在网格内");
                return null;
            }

            // 检查起点和终点是否可通行
            if (!grid.IsWalkable(startX, startY) || !grid.IsWalkable(endX, endY))
            {
                Debug.LogWarning("PathfindingUtils: 起点或终点不可通行");
                return null;
            }

            // 初始化开放列表和关闭列表
            List<AStarNode> openList = new List<AStarNode>();
            HashSet<AStarNode> closedList = new HashSet<AStarNode>();

            // 创建起点节点
            AStarNode startNode = new AStarNode(start)
            {
                GCost = 0,
                HCost = CalculateHeuristic(start, end)
            };

            openList.Add(startNode);

            int iterations = 0;
            while (openList.Count > 0 && iterations < maxIterations)
            {
                iterations++;

                // 获取FCost最小的节点
                AStarNode currentNode = openList[0];
                for (int i = 1; i < openList.Count; i++)
                {
                    if (openList[i].FCost < currentNode.FCost ||
                        (openList[i].FCost == currentNode.FCost && openList[i].HCost < currentNode.HCost))
                    {
                        currentNode = openList[i];
                    }
                }

                // 如果当前节点是终点，重构路径
                if (grid.WorldToGrid(currentNode.Position, out int currentX, out int currentY) &&
                    currentX == endX && currentY == endY)
                {
                    return ReconstructPath(currentNode);
                }

                // 将当前节点移到关闭列表
                openList.Remove(currentNode);
                closedList.Add(currentNode);

                // 检查相邻节点
                foreach (var neighbor in GetNeighbors(currentNode, grid))
                {
                    if (closedList.Contains(neighbor))
                    {
                        continue;
                    }

                    float newGCost = currentNode.GCost + CalculateDistance(currentNode.Position, neighbor.Position);
                    if (newGCost < neighbor.GCost || !openList.Contains(neighbor))
                    {
                        neighbor.GCost = newGCost;
                        neighbor.HCost = CalculateHeuristic(neighbor.Position, end);
                        neighbor.Parent = currentNode;

                        if (!openList.Contains(neighbor))
                        {
                            openList.Add(neighbor);
                        }
                    }
                }
            }

            Debug.LogWarning("PathfindingUtils: A* 路径查找失败");
            return null;
        }

        // 获取相邻节点
        private static List<AStarNode> GetNeighbors(AStarNode node, PathfindingGrid grid)
        {
            List<AStarNode> neighbors = new List<AStarNode>();

            if (grid.WorldToGrid(node.Position, out int x, out int y))
            {
                // 检查8个方向
                for (int i = -1; i <= 1; i++)
                {
                    for (int j = -1; j <= 1; j++)
                    {
                        if (i == 0 && j == 0) continue; // 跳过自己

                        int newX = x + i;
                        int newY = y + j;

                        if (grid.IsInBounds(newX, newY) && grid.IsWalkable(newX, newY))
                        {
                            neighbors.Add(new AStarNode(grid.GridToWorld(newX, newY)));
                        }
                    }
                }
            }

            return neighbors;
        }

        // 重构路径
        private static List<Vector3> ReconstructPath(AStarNode endNode)
        {
            List<Vector3> path = new List<Vector3>();
            AStarNode currentNode = endNode;

            while (currentNode != null)
            {
                path.Add(currentNode.Position);
                currentNode = currentNode.Parent;
            }

            path.Reverse();
            return path;
        }

        // 计算启发式代价（欧几里得距离）
        private static float CalculateHeuristic(Vector3 a, Vector3 b)
        {
            return Vector3.Distance(a, b);
        }

        // 计算实际代价
        private static float CalculateDistance(Vector3 a, Vector3 b)
        {
            return Vector3.Distance(a, b);
        }

        #endregion

        #region 路径平滑与优化

        /// <summary>
        /// 平滑路径（去除不必要的拐点）
        /// </summary>
        /// <param name="path">原始路径</param>
        /// <param name="areaMask">可通行区域掩码</param>
        /// <returns>平滑后的路径</returns>
        public static List<Vector3> SmoothPath(List<Vector3> path, int areaMask = NavMesh.AllAreas)
        {
            if (path == null || path.Count < 3)
            {
                return path;
            }

            List<Vector3> smoothedPath = new List<Vector3> { path[0] };
            int lastAddedIndex = 0;

            for (int i = 1; i < path.Count - 1; i++)
            {
                if (!HasDirectPath(smoothedPath[smoothedPath.Count - 1], path[i + 1], Mathf.Infinity, areaMask))
                {
                    smoothedPath.Add(path[i]);
                    lastAddedIndex = i;
                }
            }

            smoothedPath.Add(path[path.Count - 1]);
            return smoothedPath;
        }

        /// <summary>
        /// 简化路径（减少路径点数量）
        /// </summary>
        /// <param name="path">原始路径</param>
        /// <param name="maxDistance">最大简化距离</param>
        /// <param name="areaMask">可通行区域掩码</param>
        /// <returns>简化后的路径</returns>
        public static List<Vector3> SimplifyPath(List<Vector3> path, float maxDistance = 1.0f,
            int areaMask = NavMesh.AllAreas)
        {
            if (path == null || path.Count < 3)
            {
                return path;
            }

            List<Vector3> simplifiedPath = new List<Vector3> { path[0] };

            for (int i = 1; i < path.Count - 1; i++)
            {
                if (Vector3.Distance(simplifiedPath[simplifiedPath.Count - 1], path[i + 1]) > maxDistance ||
                    !HasDirectPath(simplifiedPath[simplifiedPath.Count - 1], path[i + 1], maxDistance, areaMask))
                {
                    simplifiedPath.Add(path[i]);
                }
            }

            simplifiedPath.Add(path[path.Count - 1]);
            return simplifiedPath;
        }

        #endregion

        #region 寻路网格

        /// <summary>
        /// 寻路网格类
        /// </summary>
        public class PathfindingGrid
        {
            public float CellSize { get; private set; }
            public int Width { get; private set; }
            public int Height { get; private set; }
            public Vector3 Origin { get; private set; }
            public bool[,] Walkable { get; private set; }

            public PathfindingGrid(Vector3 origin, int width, int height, float cellSize)
            {
                Origin = origin;
                Width = width;
                Height = height;
                CellSize = cellSize;
                Walkable = new bool[width, height];

                // 默认所有格子都可通行
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        Walkable[x, y] = true;
                    }
                }
            }

            /// <summary>
            /// 检查坐标是否在网格范围内
            /// </summary>
            public bool IsInBounds(int x, int y)
            {
                return x >= 0 && x < Width && y >= 0 && y < Height;
            }

            /// <summary>
            /// 检查格子是否可通行
            /// </summary>
            public bool IsWalkable(int x, int y)
            {
                return IsInBounds(x, y) && Walkable[x, y];
            }

            /// <summary>
            /// 世界坐标转网格坐标
            /// </summary>
            public bool WorldToGrid(Vector3 worldPosition, out int x, out int y)
            {
                Vector3 localPos = worldPosition - Origin;
                x = Mathf.FloorToInt(localPos.x / CellSize);
                y = Mathf.FloorToInt(localPos.z / CellSize); // 假设使用XZ平面

                return IsInBounds(x, y);
            }

            /// <summary>
            /// 网格坐标转世界坐标
            /// </summary>
            public Vector3 GridToWorld(int x, int y)
            {
                return Origin + new Vector3(x * CellSize + CellSize * 0.5f, 0, y * CellSize + CellSize * 0.5f);
            }

            /// <summary>
            /// 从NavMesh生成寻路网格
            /// </summary>
            public void GenerateFromNavMesh(Vector3 center, float width, float height, float cellSize,
                int areaMask = NavMesh.AllAreas)
            {
                CellSize = cellSize;
                Width = Mathf.CeilToInt(width / cellSize);
                Height = Mathf.CeilToInt(height / cellSize);
                Origin = center - new Vector3(width * 0.5f, 0, height * 0.5f);
                Walkable = new bool[Width, Height];

                for (int x = 0; x < Width; x++)
                {
                    for (int y = 0; y < Height; y++)
                    {
                        Vector3 worldPos = GridToWorld(x, y);
                        Walkable[x, y] =
                            NavMesh.SamplePosition(worldPos, out NavMeshHit hit, cellSize * 0.5f, areaMask);
                    }
                }
            }
        }

        #endregion
    }
}