namespace GameObjectToolkit
{
    using UnityEngine;
    using System.Collections.Generic;

    /// <summary>
    /// Unity碰撞检测工具类
    /// 提供各种形状的碰撞检测和物理查询功能
    /// </summary>
    public static class CollisionUtils
    {
        #region 基本碰撞检测

        /// <summary>
        /// 检查两个球体是否相交
        /// </summary>
        /// <param name="sphere1Pos">球体1中心位置</param>
        /// <param name="sphere1Radius">球体1半径</param>
        /// <param name="sphere2Pos">球体2中心位置</param>
        /// <param name="sphere2Radius">球体2半径</param>
        /// <returns>是否相交</returns>
        public static bool SphereSphereIntersect(Vector3 sphere1Pos, float sphere1Radius,
            Vector3 sphere2Pos, float sphere2Radius)
        {
            float distanceSqr = (sphere1Pos - sphere2Pos).sqrMagnitude;
            float radiusSum = sphere1Radius + sphere2Radius;
            return distanceSqr <= radiusSum * radiusSum;
        }

        /// <summary>
        /// 检查球体与立方体是否相交
        /// </summary>
        /// <param name="spherePos">球体中心位置</param>
        /// <param name="sphereRadius">球体半径</param>
        /// <param name="boxCenter">立方体中心位置</param>
        /// <param name="boxHalfExtents">立方体半尺寸</param>
        /// <param name="boxRotation">立方体旋转</param>
        /// <returns>是否相交</returns>
        public static bool SphereBoxIntersect(Vector3 spherePos, float sphereRadius,
            Vector3 boxCenter, Vector3 boxHalfExtents,
            Quaternion boxRotation = default)
        {
            // 将球体坐标转换到立方体局部空间
            Vector3 localSpherePos = Quaternion.Inverse(boxRotation) * (spherePos - boxCenter);

            // 计算球体到立方体的最近点
            Vector3 closestPoint = new Vector3(
                Mathf.Clamp(localSpherePos.x, -boxHalfExtents.x, boxHalfExtents.x),
                Mathf.Clamp(localSpherePos.y, -boxHalfExtents.y, boxHalfExtents.y),
                Mathf.Clamp(localSpherePos.z, -boxHalfExtents.z, boxHalfExtents.z)
            );

            // 检查最近点与球体的距离
            float distanceSqr = (localSpherePos - closestPoint).sqrMagnitude;
            return distanceSqr <= sphereRadius * sphereRadius;
        }

        /// <summary>
        /// 检查点是否在立方体内
        /// </summary>
        /// <param name="point">点位置</param>
        /// <param name="boxCenter">立方体中心</param>
        /// <param name="boxHalfExtents">立方体半尺寸</param>
        /// <param name="boxRotation">立方体旋转</param>
        /// <returns>是否包含</returns>
        public static bool PointInBox(Vector3 point, Vector3 boxCenter,
            Vector3 boxHalfExtents, Quaternion boxRotation = default)
        {
            Vector3 localPoint = Quaternion.Inverse(boxRotation) * (point - boxCenter);
            return Mathf.Abs(localPoint.x) <= boxHalfExtents.x &&
                   Mathf.Abs(localPoint.y) <= boxHalfExtents.y &&
                   Mathf.Abs(localPoint.z) <= boxHalfExtents.z;
        }

        #endregion

        #region 高级碰撞检测

        /// <summary>
        /// 检查两个OBB（有向包围盒）是否相交
        /// </summary>
        /// <param name="box1Center">盒子1中心</param>
        /// <param name="box1HalfExtents">盒子1半尺寸</param>
        /// <param name="box1Rotation">盒子1旋转</param>
        /// <param name="box2Center">盒子2中心</param>
        /// <param name="box2HalfExtents">盒子2半尺寸</param>
        /// <param name="box2Rotation">盒子2旋转</param>
        /// <returns>是否相交</returns>
        public static bool OBBIntersect(Vector3 box1Center, Vector3 box1HalfExtents, Quaternion box1Rotation,
            Vector3 box2Center, Vector3 box2HalfExtents, Quaternion box2Rotation)
        {
            // 将box2转换到box1的局部空间
            Matrix4x4 box1ToWorld = Matrix4x4.TRS(box1Center, box1Rotation, Vector3.one);
            Matrix4x4 worldToBox1 = box1ToWorld.inverse;

            Vector3 box2CenterInBox1Space = worldToBox1.MultiplyPoint(box2Center);
            Quaternion box2RotInBox1Space = Quaternion.Inverse(box1Rotation) * box2Rotation;

            // 在box1空间中进行AABB vs OBB检测
            return AABBIntersectOBB(
                Vector3.zero,
                box1HalfExtents,
                Quaternion.identity,
                box2CenterInBox1Space,
                box2HalfExtents,
                box2RotInBox1Space);
        }

        // AABB与OBB相交检测（辅助方法）
        private static bool AABBIntersectOBB(Vector3 aabbCenter, Vector3 aabbHalfExtents, Quaternion aabbRotation,
            Vector3 obbCenter, Vector3 obbHalfExtents,
            Quaternion obbRotation)
        {
            // 将OBB中心转换到AABB局部空间
            Vector3 localObbCenter = Quaternion.Inverse(aabbRotation) * (obbCenter - aabbCenter);

            // 将OBB旋转转换到AABB局部空间
            Quaternion localObbRot = Quaternion.Inverse(aabbRotation) * obbRotation;

            // 在AABB局部空间中进行检测
            Vector3 closestPoint = localObbCenter;
            Vector3 localPoint = Quaternion.Inverse(localObbRot) * (-localObbCenter);

            closestPoint = localObbCenter + localObbRot * new Vector3(
                Mathf.Clamp(localPoint.x, -obbHalfExtents.x, obbHalfExtents.x),
                Mathf.Clamp(localPoint.y, -obbHalfExtents.y, obbHalfExtents.y),
                Mathf.Clamp(localPoint.z, -obbHalfExtents.z, obbHalfExtents.z)
            );

            // 检查最近点是否在AABB内
            return Mathf.Abs(closestPoint.x) <= aabbHalfExtents.x &&
                   Mathf.Abs(closestPoint.y) <= aabbHalfExtents.y &&
                   Mathf.Abs(closestPoint.z) <= aabbHalfExtents.z;
        }

        /// <summary>
        /// 检查胶囊体与球体是否相交
        /// </summary>
        /// <param name="capsuleStart">胶囊体起点</param>
        /// <param name="capsuleEnd">胶囊体终点</param>
        /// <param name="capsuleRadius">胶囊体半径</param>
        /// <param name="spherePos">球体中心</param>
        /// <param name="sphereRadius">球体半径</param>
        /// <returns>是否相交</returns>
        public static bool CapsuleSphereIntersect(Vector3 capsuleStart, Vector3 capsuleEnd,
            float capsuleRadius, Vector3 spherePos,
            float sphereRadius)
        {
            Vector3 closestPoint = ClosestPointOnLineSegment(capsuleStart, capsuleEnd, spherePos);
            float distanceSqr = (closestPoint - spherePos).sqrMagnitude;
            float radiusSum = capsuleRadius + sphereRadius;
            return distanceSqr <= radiusSum * radiusSum;
        }

        // 计算点到线段最近点（辅助方法）
        private static Vector3 ClosestPointOnLineSegment(Vector3 start, Vector3 end, Vector3 point)
        {
            Vector3 line = end - start;
            float lineLength = line.magnitude;
            Vector3 lineDirection = line.normalized;

            float projection = Vector3.Dot(point - start, lineDirection);
            projection = Mathf.Clamp(projection, 0f, lineLength);

            return start + lineDirection * projection;
        }

        #endregion

        #region 射线检测

        /// <summary>
        /// 射线与球体相交检测
        /// </summary>
        /// <param name="rayOrigin">射线起点</param>
        /// <param name="rayDirection">射线方向</param>
        /// <param name="sphereCenter">球体中心</param>
        /// <param name="sphereRadius">球体半径</param>
        /// <param name="hitPoint">相交点（输出）</param>
        /// <returns>是否相交</returns>
        public static bool RaySphereIntersect(Vector3 rayOrigin, Vector3 rayDirection,
            Vector3 sphereCenter, float sphereRadius,
            out Vector3 hitPoint)
        {
            hitPoint = Vector3.zero;
            Vector3 oc = rayOrigin - sphereCenter;
            float a = Vector3.Dot(rayDirection, rayDirection);
            float b = 2f * Vector3.Dot(oc, rayDirection);
            float c = Vector3.Dot(oc, oc) - sphereRadius * sphereRadius;
            float discriminant = b * b - 4 * a * c;

            if (discriminant < 0)
            {
                return false;
            }

            float sqrtDiscriminant = Mathf.Sqrt(discriminant);
            float t1 = (-b - sqrtDiscriminant) / (2f * a);
            float t2 = (-b + sqrtDiscriminant) / (2f * a);

            float t = Mathf.Min(t1, t2);
            if (t < 0)
            {
                t = Mathf.Max(t1, t2);
                if (t < 0)
                {
                    return false;
                }
            }

            hitPoint = rayOrigin + rayDirection * t;
            return true;
        }

        /// <summary>
        /// 射线与OBB相交检测
        /// </summary>
        /// <param name="ray">射线</param>
        /// <param name="boxCenter">盒子中心</param>
        /// <param name="boxHalfExtents">盒子半尺寸</param>
        /// <param name="boxRotation">盒子旋转</param>
        /// <param name="hitPoint">相交点（输出）</param>
        /// <param name="hitDistance">相交距离（输出）</param>
        /// <returns>是否相交</returns>
        public static bool RayOBBIntersect(Ray ray, Vector3 boxCenter, Vector3 boxHalfExtents,
            Quaternion boxRotation, out Vector3 hitPoint,
            out float hitDistance)
        {
            hitPoint = Vector3.zero;
            hitDistance = 0f;

            // 将射线转换到OBB局部空间
            Matrix4x4 worldToLocal = Matrix4x4.TRS(boxCenter, boxRotation, Vector3.one).inverse;
            Vector3 localOrigin = worldToLocal.MultiplyPoint(ray.origin);
            Vector3 localDirection = worldToLocal.MultiplyVector(ray.direction).normalized;

            Ray localRay = new Ray(localOrigin, localDirection);

            // 在局部空间中进行AABB射线检测
            if (RayAABBIntersect(localRay, Vector3.zero, boxHalfExtents,
                    out Vector3 localHitPoint, out hitDistance))
            {
                hitPoint = boxCenter + boxRotation * localHitPoint;
                return true;
            }

            return false;
        }

        // 射线与AABB相交检测（辅助方法）
        private static bool RayAABBIntersect(Ray ray, Vector3 boxCenter, Vector3 boxHalfExtents,
            out Vector3 hitPoint, out float hitDistance)
        {
            hitPoint = Vector3.zero;
            hitDistance = 0f;

            Vector3 min = boxCenter - boxHalfExtents;
            Vector3 max = boxCenter + boxHalfExtents;

            float tMin = float.MinValue;
            float tMax = float.MaxValue;

            for (int i = 0; i < 3; i++)
            {
                if (Mathf.Abs(ray.direction[i]) < Mathf.Epsilon)
                {
                    // 射线平行于这个轴
                    if (ray.origin[i] < min[i] || ray.origin[i] > max[i])
                    {
                        return false;
                    }
                }
                else
                {
                    float invDir = 1f / ray.direction[i];
                    float t1 = (min[i] - ray.origin[i]) * invDir;
                    float t2 = (max[i] - ray.origin[i]) * invDir;

                    if (t1 > t2)
                    {
                        float temp = t1;
                        t1 = t2;
                        t2 = temp;
                    }

                    tMin = Mathf.Max(tMin, t1);
                    tMax = Mathf.Min(tMax, t2);

                    if (tMin > tMax)
                    {
                        return false;
                    }
                }
            }

            hitDistance = tMin;
            hitPoint = ray.origin + ray.direction * hitDistance;
            return true;
        }

        #endregion

        #region 物理查询

        /// <summary>
        /// 获取球体范围内的所有碰撞体
        /// </summary>
        /// <param name="position">球体中心</param>
        /// <param name="radius">球体半径</param>
        /// <param name="layerMask">层级掩码</param>
        /// <param name="queryTriggerInteraction">触发器交互方式</param>
        /// <returns>碰撞体数组</returns>
        public static Collider[] OverlapSphere(Vector3 position, float radius,
            int layerMask = Physics.DefaultRaycastLayers,
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            return Physics.OverlapSphere(position, radius, layerMask, queryTriggerInteraction);
        }

        /// <summary>
        /// 获取球体范围内的所有刚体
        /// </summary>
        /// <param name="position">球体中心</param>
        /// <param name="radius">球体半径</param>
        /// <param name="layerMask">层级掩码</param>
        /// <returns>刚体数组</returns>
        public static Rigidbody[] OverlapSphereRigidbodies(Vector3 position, float radius,
            int layerMask = Physics.DefaultRaycastLayers)
        {
            Collider[] colliders = Physics.OverlapSphere(position, radius, layerMask);
            List<Rigidbody> rigidbodies = new List<Rigidbody>();

            foreach (Collider collider in colliders)
            {
                Rigidbody rb = collider.attachedRigidbody;
                if (rb != null && !rigidbodies.Contains(rb))
                {
                    rigidbodies.Add(rb);
                }
            }

            return rigidbodies.ToArray();
        }

        /// <summary>
        /// 获取胶囊体范围内的所有碰撞体
        /// </summary>
        /// <param name="point1">胶囊体起点</param>
        /// <param name="point2">胶囊体终点</param>
        /// <param name="radius">胶囊体半径</param>
        /// <param name="layerMask">层级掩码</param>
        /// <param name="queryTriggerInteraction">触发器交互方式</param>
        /// <returns>碰撞体数组</returns>
        public static Collider[] OverlapCapsule(Vector3 point1, Vector3 point2, float radius,
            int layerMask = Physics.DefaultRaycastLayers,
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            return Physics.OverlapCapsule(point1, point2, radius, layerMask, queryTriggerInteraction);
        }

        #endregion

        #region Gizmos辅助

        /// <summary>
        /// 绘制碰撞体Gizmos（编辑器中使用）
        /// </summary>
        /// <param name="collider">碰撞体</param>
        /// <param name="color">绘制颜色</param>
        public static void DrawColliderGizmos(Collider collider, Color color)
        {
            if (collider == null) return;

            Gizmos.color = color;

            if (collider is BoxCollider boxCollider)
            {
                DrawBoxGizmos(boxCollider.center, boxCollider.size * 0.5f, collider.transform);
            }
            else if (collider is SphereCollider sphereCollider)
            {
                Vector3 center = collider.transform.TransformPoint(sphereCollider.center);
                Gizmos.DrawWireSphere(center, sphereCollider.radius);
            }
            else if (collider is CapsuleCollider capsuleCollider)
            {
                DrawCapsuleGizmos(capsuleCollider, collider.transform);
            }
            else if (collider is MeshCollider meshCollider)
            {
                Gizmos.DrawWireMesh(meshCollider.sharedMesh,
                    collider.transform.position,
                    collider.transform.rotation,
                    collider.transform.lossyScale);
            }
        }

        // 绘制盒子Gizmos（辅助方法）
        private static void DrawBoxGizmos(Vector3 center, Vector3 halfExtents, Transform transform)
        {
            Vector3[] points = new Vector3[8];

            // 计算局部空间的8个顶点
            points[0] = center + new Vector3(-halfExtents.x, -halfExtents.y, -halfExtents.z);
            points[1] = center + new Vector3(halfExtents.x, -halfExtents.y, -halfExtents.z);
            points[2] = center + new Vector3(halfExtents.x, -halfExtents.y, halfExtents.z);
            points[3] = center + new Vector3(-halfExtents.x, -halfExtents.y, halfExtents.z);

            points[4] = center + new Vector3(-halfExtents.x, halfExtents.y, -halfExtents.z);
            points[5] = center + new Vector3(halfExtents.x, halfExtents.y, -halfExtents.z);
            points[6] = center + new Vector3(halfExtents.x, halfExtents.y, halfExtents.z);
            points[7] = center + new Vector3(-halfExtents.x, halfExtents.y, halfExtents.z);

            // 转换到世界空间
            for (int i = 0; i < 8; i++)
            {
                points[i] = transform.TransformPoint(points[i]);
            }

            // 绘制12条边
            DrawLine(points[0], points[1]);
            DrawLine(points[1], points[2]);
            DrawLine(points[2], points[3]);
            DrawLine(points[3], points[0]);

            DrawLine(points[4], points[5]);
            DrawLine(points[5], points[6]);
            DrawLine(points[6], points[7]);
            DrawLine(points[7], points[4]);

            DrawLine(points[0], points[4]);
            DrawLine(points[1], points[5]);
            DrawLine(points[2], points[6]);
            DrawLine(points[3], points[7]);
        }

        // 绘制胶囊体Gizmos（辅助方法）
        private static void DrawCapsuleGizmos(CapsuleCollider capsule, Transform transform)
        {
            Vector3 center = transform.TransformPoint(capsule.center);
            float radius = capsule.radius * GetMaxScale(transform);
            float height = capsule.height * GetMaxScale(transform);

            Vector3 up = transform.up;
            Vector3 top = center + up * (height * 0.5f - radius);
            Vector3 bottom = center - up * (height * 0.5f - radius);

            // 绘制上下半球
            DrawWireHemisphere(top, -up, radius);
            DrawWireHemisphere(bottom, up, radius);

            // 绘制侧面线
            Vector3 right = transform.right * radius;
            Vector3 forward = transform.forward * radius;

            Gizmos.DrawLine(top + right, bottom + right);
            Gizmos.DrawLine(top - right, bottom - right);
            Gizmos.DrawLine(top + forward, bottom + forward);
            Gizmos.DrawLine(top - forward, bottom - forward);
        }

        // 绘制半球Gizmos（辅助方法）
        private static void DrawWireHemisphere(Vector3 center, Vector3 normal, float radius)
        {
            Vector3 perpendicular = Vector3.Cross(normal, Vector3.up);
            if (perpendicular == Vector3.zero)
            {
                perpendicular = Vector3.Cross(normal, Vector3.right);
            }

            perpendicular = perpendicular.normalized;
            Vector3 binormal = Vector3.Cross(normal, perpendicular);

            int segments = 12;
            float angleStep = Mathf.PI / segments;

            // 绘制半圆
            for (int i = 0; i < segments; i++)
            {
                float angle = i * angleStep;
                float nextAngle = (i + 1) * angleStep;

                Vector3 p1 = center + (perpendicular * Mathf.Sin(angle) + binormal * Mathf.Cos(angle)) * radius;
                Vector3 p2 = center + (perpendicular * Mathf.Sin(nextAngle) + binormal * Mathf.Cos(nextAngle)) * radius;

                Gizmos.DrawLine(p1, p2);
                Gizmos.DrawLine(p1, center + normal * radius * Mathf.Cos(angle));
            }
        }

        // 获取最大缩放值（辅助方法）
        private static float GetMaxScale(Transform transform)
        {
            Vector3 scale = transform.lossyScale;
            return Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z));
        }

        // 绘制线（辅助方法）
        private static void DrawLine(Vector3 from, Vector3 to)
        {
            Gizmos.DrawLine(from, to);
        }

        #endregion
    }
}