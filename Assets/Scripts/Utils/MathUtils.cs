using UnityEngine;
using System.Collections.Generic;
using Random = UnityEngine.Random;

namespace GameObjectToolkit
{
    /// <summary>
    /// 数学计算工具类
    /// 提供常用的数学运算、几何计算和随机数生成方法
    /// </summary>
    public static class MathUtils
    {
        #region 基础数学运算

        /// <summary>
        /// 将值限制在指定范围内
        /// </summary>
        /// <param name="value">输入值</param>
        /// <param name="min">最小值</param>
        /// <param name="max">最大值</param>
        /// <returns>限制后的值</returns>
        public static float Clamp(float value, float min, float max)
        {
            return Mathf.Clamp(value, min, max);
        }

        /// <summary>
        /// 将角度标准化到0-360度范围内
        /// </summary>
        /// <param name="angle">输入角度</param>
        /// <returns>标准化后的角度</returns>
        public static float NormalizeAngle(float angle)
        {
            angle %= 360f;
            if (angle < 0) angle += 360f;
            return angle;
        }

        /// <summary>
        /// 计算两个角度的最小差值（考虑360度环绕）
        /// </summary>
        /// <param name="a">角度A</param>
        /// <param name="b">角度B</param>
        /// <returns>最小差值</returns>
        public static float DeltaAngle(float a, float b)
        {
            float delta = b - a;
            delta = (delta + 180f) % 360f - 180f;
            return delta;
        }


        /// <summary>
        /// 计算平方反比衰减值
        /// </summary>
        /// <param name="distance">当前距离</param>
        /// <param name="maxDistance">最大有效距离</param>
        /// <param name="minValue">最小值限制</param>
        /// <returns>衰减后的值[0-1]</returns>
        public static float InverseSquareFalloff(float distance, float maxDistance, float minValue = 0.01f)
        {
            float normalizedDist = Mathf.Clamp01(distance / maxDistance);
            float falloff = 1f / (1f + 25f * normalizedDist * normalizedDist);
            return Mathf.Max(falloff, minValue);
        }

        /// <summary>
        /// 计算斐波那契数列的第n项
        /// </summary>
        public static int Fibonacci(int n)
        {
            if (n < 0) return 0;
            if (n == 0 || n == 1) return n;

            int a = 0;
            int b = 1;
            for (int i = 2; i <= n; i++)
            {
                int temp = a + b;
                a = b;
                b = temp;
            }

            return b;
        }

        /// <summary>
        /// 计算两个整数的最大公约数(GCD)
        /// </summary>
        public static int GreatestCommonDivisor(int a, int b)
        {
            while (b != 0)
            {
                int temp = b;
                b = a % b;
                a = temp;
            }

            return a;
        }

        /// <summary>
        /// 计算两个整数的最小公倍数(LCM)
        /// </summary>
        public static int LeastCommonMultiple(int a, int b)
        {
            return a / GreatestCommonDivisor(a, b) * b;
        }

        /// <summary>
        /// 计算两个向量的有符号角度（考虑法线方向）
        /// </summary>
        /// <param name="from">起始向量</param>
        /// <param name="to">目标向量</param>
        /// <param name="normal">法线方向</param>
        public static float SignedAngle(Vector3 from, Vector3 to, Vector3 normal)
        {
            float angle = Vector3.Angle(from, to);
            float sign = Mathf.Sign(Vector3.Dot(normal, Vector3.Cross(from, to)));
            return angle * sign;
        }

        #endregion

        #region 几何计算

        /// <summary>
        /// 计算两点之间的距离
        /// </summary>
        public static float Distance(Vector3 a, Vector3 b)
        {
            return Vector3.Distance(a, b);
        }

        /// <summary>
        /// 计算点到线段的最近距离
        /// </summary>
        /// <param name="point">目标点</param>
        /// <param name="lineStart">线段起点</param>
        /// <param name="lineEnd">线段终点</param>
        /// <returns>最近距离</returns>
        public static float DistanceToLine(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
        {
            Vector3 lineVec = lineEnd - lineStart;
            Vector3 pointVec = point - lineStart;

            float lineLength = lineVec.magnitude;
            Vector3 lineDir = lineVec.normalized;

            float projection = Vector3.Dot(pointVec, lineDir);
            projection = Mathf.Clamp(projection, 0f, lineLength);

            Vector3 closestPoint = lineStart + lineDir * projection;
            return Vector3.Distance(point, closestPoint);
        }

        /// <summary>
        /// 计算点到平面的距离
        /// </summary>
        /// <param name="point">目标点</param>
        /// <param name="planeNormal">平面法线</param>
        /// <param name="planePoint">平面上的一点</param>
        /// <returns>距离</returns>
        public static float DistanceToPlane(Vector3 point, Vector3 planeNormal, Vector3 planePoint)
        {
            return Mathf.Abs(Vector3.Dot(planeNormal, point - planePoint)) / planeNormal.magnitude;
        }

        /// <summary>
        /// 判断点是否在三角形内（2D空间）
        /// </summary>
        public static bool IsPointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Sign(p, a, b);
            float d2 = Sign(p, b, c);
            float d3 = Sign(p, c, a);

            bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
            bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);

            return !(hasNeg && hasPos);
        }

        private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
        }

        /// <summary>
        /// 计算多边形面积（2D）
        /// </summary>
        /// <param name="points">按顺时针或逆时针排列的顶点列表</param>
        public static float CalculatePolygonArea(List<Vector2> points)
        {
            if (points == null || points.Count < 3)
                return 0f;

            float area = 0f;
            for (int i = 0; i < points.Count; i++)
            {
                Vector2 current = points[i];
                Vector2 next = points[(i + 1) % points.Count];
                area += current.x * next.y - next.x * current.y;
            }

            return Mathf.Abs(area / 2f);
        }

        /// <summary>
        /// 计算多边形的质心（2D）
        /// </summary>
        public static Vector2 CalculatePolygonCentroid(List<Vector2> points)
        {
            if (points == null || points.Count == 0)
                return Vector2.zero;

            if (points.Count == 1)
                return points[0];

            Vector2 centroid = Vector2.zero;
            float area = 0f;

            for (int i = 0; i < points.Count; i++)
            {
                Vector2 current = points[i];
                Vector2 next = points[(i + 1) % points.Count];

                float cross = current.x * next.y - next.x * current.y;
                area += cross;
                centroid.x += (current.x + next.x) * cross;
                centroid.y += (current.y + next.y) * cross;
            }

            area *= 0.5f;
            centroid /= (6f * area);
            return centroid;
        }

        /// <summary>
        /// 判断点是否在凸多边形内（2D）
        /// </summary>
        public static bool IsPointInConvexPolygon(Vector2 point, List<Vector2> polygon)
        {
            if (polygon == null || polygon.Count < 3)
                return false;

            bool sign = false;
            for (int i = 0; i < polygon.Count; i++)
            {
                var a = polygon[i];
                var b = polygon[(i + 1) % polygon.Count];
                var edge = b - a;
                var toPoint = point - a;

                float cross = edge.x * toPoint.y - edge.y * toPoint.x;
                if (i == 0)
                {
                    sign = cross > 0;
                }
                else if ((cross > 0) != sign)
                {
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region 随机数生成

        /// <summary>
        /// 生成指定范围内的随机浮点数
        /// </summary>
        public static float RandomRange(float min, float max)
        {
            return Random.Range(min, max);
        }

        /// <summary>
        /// 生成指定范围内的随机整数
        /// </summary>
        public static int RandomRange(int min, int max)
        {
            return Random.Range(min, max);
        }

        /// <summary>
        /// 生成单位圆内的随机点
        /// </summary>
        public static Vector2 RandomInsideUnitCircle()
        {
            return Random.insideUnitCircle;
        }

        /// <summary>
        /// 生成单位球体内的随机点
        /// </summary>
        public static Vector3 RandomInsideUnitSphere()
        {
            return Random.insideUnitSphere;
        }

        /// <summary>
        /// 生成球体表面的随机点
        /// </summary>
        public static Vector3 RandomOnUnitSphere()
        {
            return Random.onUnitSphere;
        }


        /// <summary>
        /// 从权重列表中随机选择索引
        /// </summary>
        /// <param name="weights">权重列表</param>
        /// <returns>被选中的索引</returns>
        public static int WeightedRandomIndex(List<float> weights)
        {
            if (weights == null || weights.Count == 0)
                return -1;

            float totalWeight = 0f;
            foreach (float w in weights)
                totalWeight += w;

            float randomValue = UnityEngine.Random.Range(0f, totalWeight);
            float cumulativeWeight = 0f;

            for (int i = 0; i < weights.Count; i++)
            {
                cumulativeWeight += weights[i];
                if (randomValue <= cumulativeWeight)
                    return i;
            }

            return weights.Count - 1;
        }

        /// <summary>
        /// 生成服从正态分布(Gaussian)的随机数
        /// </summary>
        /// <param name="mean">均值</param>
        /// <param name="stdDev">标准差</param>
        public static float GaussianRandom(float mean = 0f, float stdDev = 1f)
        {
            float u1 = 1f - UnityEngine.Random.value;
            float u2 = 1f - UnityEngine.Random.value;
            float randStdNormal = Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Sin(2f * Mathf.PI * u2);
            return mean + stdDev * randStdNormal;
        }

        /// <summary>
        /// 从列表中随机选择不重复的多个元素
        /// </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="list">源列表</param>
        /// <param name="count">要选择的数量</param>
        public static List<T> RandomSelectUnique<T>(List<T> list, int count)
        {
            if (list == null || count <= 0 || list.Count == 0)
                return new List<T>();

            count = Mathf.Min(count, list.Count);
            List<T> tempList = new List<T>(list);
            List<T> result = new List<T>(count);

            for (int i = 0; i < count; i++)
            {
                int index = UnityEngine.Random.Range(0, tempList.Count);
                result.Add(tempList[index]);
                tempList.RemoveAt(index);
            }

            return result;
        }

        #endregion

        #region 插值方法

        /// <summary>
        /// 线性插值
        /// </summary>
        public static float Lerp(float a, float b, float t)
        {
            return Mathf.Lerp(a, b, t);
        }

        /// <summary>
        /// 角度插值（考虑360度环绕）
        /// </summary>
        public static float LerpAngle(float a, float b, float t)
        {
            return Mathf.LerpAngle(a, b, t);
        }

        /// <summary>
        /// 平滑阻尼插值
        /// </summary>
        /// <param name="current">当前值</param>
        /// <param name="target">目标值</param>
        /// <param name="currentVelocity">当前速度（引用）</param>
        /// <param name="smoothTime">平滑时间</param>
        /// <param name="maxSpeed">最大速度</param>
        /// <param name="deltaTime">时间增量</param>
        public static float SmoothDamp(float current, float target, ref float currentVelocity,
            float smoothTime, float maxSpeed = Mathf.Infinity, float deltaTime = 0f)
        {
            return Mathf.SmoothDamp(current, target, ref currentVelocity, smoothTime, maxSpeed,
                deltaTime > 0f ? deltaTime : Time.deltaTime);
        }

        /// <summary>
        /// 双线性插值
        /// </summary>
        /// <param name="q11">左下点值</param>
        /// <param name="q12">右下点值</param>
        /// <param name="q21">左上点值</param>
        /// <param name="q22">右上点值</param>
        /// <param name="x">x方向插值位置[0-1]</param>
        /// <param name="y">y方向插值位置[0-1]</param>
        public static float BilinearInterpolation(float q11, float q12, float q21, float q22, float x, float y)
        {
            float r1 = Mathf.Lerp(q11, q12, x);
            float r2 = Mathf.Lerp(q21, q22, x);
            return Mathf.Lerp(r1, r2, y);
        }

        #endregion

        #region 曲线计算

        /// <summary>
        /// 计算贝塞尔曲线点（二次）
        /// </summary>
        /// <param name="p0">起点</param>
        /// <param name="p1">控制点</param>
        /// <param name="p2">终点</param>
        /// <param name="t">插值参数[0,1]</param>
        public static Vector3 CalculateBezierPoint(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            float u = 1 - t;
            return u * u * p0 + 2 * u * t * p1 + t * t * p2;
        }

        /// <summary>
        /// 计算贝塞尔曲线点（三次）
        /// </summary>
        public static Vector3 CalculateBezierPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float u = 1 - t;
            return u * u * u * p0 + 3 * u * u * t * p1 + 3 * u * t * t * p2 + t * t * t * p3;
        }

        /// <summary>
        /// 计算Catmull-Rom样条曲线点
        /// </summary>
        /// <param name="p0">前一个控制点</param>
        /// <param name="p1">起点</param>
        /// <param name="p2">终点</param>
        /// <param name="p3">后一个控制点</param>
        /// <param name="t">插值参数[0-1]</param>
        public static Vector3 CatmullRomInterpolation(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;

            return 0.5f * (
                (2f * p1) +
                (-p0 + p2) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                (-p0 + 3f * p1 - 3f * p2 + p3) * t3
            );
        }

        /// <summary>
        /// 计算Hermite样条曲线点
        /// </summary>
        /// <param name="p0">起点</param>
        /// <param name="m0">起点切线</param>
        /// <param name="p1">终点</param>
        /// <param name="m1">终点切线</param>
        /// <param name="t">插值参数[0-1]</param>
        public static Vector3 HermiteInterpolation(Vector3 p0, Vector3 m0, Vector3 p1, Vector3 m1, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;

            return (2f * t3 - 3f * t2 + 1f) * p0 +
                   (t3 - 2f * t2 + t) * m0 +
                   (-2f * t3 + 3f * t2) * p1 +
                   (t3 - t2) * m1;
        }

        #endregion

        #region 坐标系转换

        /// <summary>
        /// 将世界坐标转换为本地坐标
        /// </summary>
        /// <param name="worldPos">世界坐标</param>
        /// <param name="localSpace">本地空间变换</param>
        public static Vector3 WorldToLocalPosition(Vector3 worldPos, Transform localSpace)
        {
            return localSpace.InverseTransformPoint(worldPos);
        }

        /// <summary>
        /// 将本地坐标转换为世界坐标
        /// </summary>
        public static Vector3 LocalToWorldPosition(Vector3 localPos, Transform localSpace)
        {
            return localSpace.TransformPoint(localPos);
        }

        #endregion

        #region 物理计算

        /// <summary>
        /// 计算抛物线初速度
        /// </summary>
        /// <param name="origin">起点</param>
        /// <param name="target">目标点</param>
        /// <param name="angle">发射角度（度）</param>
        /// <returns>初速度向量</returns>
        public static Vector3 CalculateProjectileVelocity(Vector3 origin, Vector3 target, float angle)
        {
            Vector3 direction = target - origin;
            float height = direction.y;
            direction.y = 0;
            float distance = direction.magnitude;
            float radians = angle * Mathf.Deg2Rad;

            direction.y = distance * Mathf.Tan(radians);
            distance += height / Mathf.Tan(radians);

            if (distance <= 0) return Vector3.zero;

            float velocity = Mathf.Sqrt(distance * Physics.gravity.magnitude / Mathf.Sin(2 * radians));
            return velocity * direction.normalized;
        }

        /// <summary>
        /// 计算弹性碰撞后的速度
        /// </summary>
        /// <param name="m1">物体1质量</param>
        /// <param name="v1">物体1速度</param>
        /// <param name="m2">物体2质量</param>
        /// <param name="v2">物体2速度</param>
        /// <returns>(物体1新速度, 物体2新速度)</returns>
        public static (Vector3, Vector3) ElasticCollision(float m1, Vector3 v1, float m2, Vector3 v2)
        {
            Vector3 v1New = (m1 - m2) / (m1 + m2) * v1 + (2 * m2) / (m1 + m2) * v2;
            Vector3 v2New = (2 * m1) / (m1 + m2) * v1 + (m2 - m1) / (m1 + m2) * v2;
            return (v1New, v2New);
        }

        /// <summary>
        /// 计算空气阻力影响下的速度衰减
        /// </summary>
        /// <param name="currentVelocity">当前速度</param>
        /// <param name="dragCoefficient">阻力系数</param>
        /// <param name="deltaTime">时间增量</param>
        public static Vector3 ApplyAirResistance(Vector3 currentVelocity, float dragCoefficient, float deltaTime)
        {
            float speed = currentVelocity.magnitude;
            if (speed <= 0.001f) return Vector3.zero;

            Vector3 dragForce = -dragCoefficient * speed * speed * currentVelocity.normalized;
            return currentVelocity + dragForce * deltaTime;
        }

        #endregion

        #region 其他实用方法

        /// <summary>
        /// 将数值映射到新的范围
        /// </summary>
        /// <param name="value">输入值</param>
        /// <param name="fromMin">原范围最小值</param>
        /// <param name="fromMax">原范围最大值</param>
        /// <param name="toMin">目标范围最小值</param>
        /// <param name="toMax">目标范围最大值</param>
        public static float Remap(float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            return (value - fromMin) / (fromMax - fromMin) * (toMax - toMin) + toMin;
        }

        /// <summary>
        /// 检查两个浮点数是否近似相等
        /// </summary>
        /// <param name="a">值A</param>
        /// <param name="b">值B</param>
        /// <param name="threshold">容差阈值</param>
        public static bool Approximately(float a, float b, float threshold = 0.0001f)
        {
            return Mathf.Abs(a - b) < threshold;
        }


        /// <summary>
        /// 将HSV颜色转换为RGB颜色
        /// </summary>
        /// <param name="h">色相[0-1]</param>
        /// <param name="s">饱和度[0-1]</param>
        /// <param name="v">明度[0-1]</param>
        public static Color HsvToRgb(float h, float s, float v)
        {
            h = Mathf.Repeat(h, 1f);
            s = Mathf.Clamp01(s);
            v = Mathf.Clamp01(v);

            if (s <= 0f)
                return new Color(v, v, v);

            float hue = h * 6f;
            int sector = Mathf.FloorToInt(hue);
            float fraction = hue - sector;
            float p = v * (1f - s);
            float q = v * (1f - s * fraction);
            float t = v * (1f - s * (1f - fraction));

            switch (sector % 6)
            {
                case 0: return new Color(v, t, p);
                case 1: return new Color(q, v, p);
                case 2: return new Color(p, v, t);
                case 3: return new Color(p, q, v);
                case 4: return new Color(t, p, v);
                default: return new Color(v, p, q);
            }
        }

        #endregion


        #region 噪声与随机分布

        /// <summary>
        /// 生成Perlin噪声值
        /// </summary>
        /// <param name="x">x坐标</param>
        /// <param name="y">y坐标</param>
        /// <param name="scale">缩放系数</param>
        /// <param name="octaves">噪声层数</param>
        /// <param name="persistence">持久度</param>
        /// <param name="lacunarity">间隙度</param>
        public static float PerlinNoise(float x, float y, float scale = 1f,
            int octaves = 1, float persistence = 0.5f, float lacunarity = 2f)
        {
            if (scale <= 0) scale = 0.0001f;

            float amplitude = 1f;
            float frequency = 1f;
            float noiseValue = 0f;
            float maxValue = 0f;

            for (int i = 0; i < octaves; i++)
            {
                float sampleX = x / scale * frequency;
                float sampleY = y / scale * frequency;

                float perlin = Mathf.PerlinNoise(sampleX, sampleY) * 2f - 1f;
                noiseValue += perlin * amplitude;

                maxValue += amplitude;
                amplitude *= persistence;
                frequency *= lacunarity;
            }

            return noiseValue / maxValue;
        }

        /// <summary>
        /// 生成泊松圆盘采样点
        /// </summary>
        /// <param name="radius">点之间的最小距离</param>
        /// <param name="sampleRegionSize">采样区域大小</param>
        /// <param name="rejectionLimit">采样失败次数限制</param>
        public static List<Vector2> PoissonDiscSampling(float radius, Vector2 sampleRegionSize, int rejectionLimit = 30)
        {
            float cellSize = radius / Mathf.Sqrt(2);
            int[,] grid = new int[
                Mathf.CeilToInt(sampleRegionSize.x / cellSize),
                Mathf.CeilToInt(sampleRegionSize.y / cellSize)
            ];

            List<Vector2> points = new List<Vector2>();
            List<Vector2> spawnPoints = new List<Vector2> { sampleRegionSize / 2 };

            while (spawnPoints.Count > 0)
            {
                int spawnIndex = UnityEngine.Random.Range(0, spawnPoints.Count);
                Vector2 spawnCenter = spawnPoints[spawnIndex];
                bool candidateAccepted = false;

                for (int i = 0; i < rejectionLimit; i++)
                {
                    float angle = UnityEngine.Random.value * Mathf.PI * 2;
                    Vector2 dir = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
                    Vector2 candidate = spawnCenter + dir * UnityEngine.Random.Range(radius, 2 * radius);

                    if (IsValidCandidate(candidate, sampleRegionSize, cellSize, radius, points, grid))
                    {
                        points.Add(candidate);
                        spawnPoints.Add(candidate);
                        grid[(int)(candidate.x / cellSize), (int)(candidate.y / cellSize)] = points.Count;
                        candidateAccepted = true;
                        break;
                    }
                }

                if (!candidateAccepted)
                {
                    spawnPoints.RemoveAt(spawnIndex);
                }
            }

            return points;
        }

        private static bool IsValidCandidate(Vector2 candidate, Vector2 sampleRegionSize,
            float cellSize, float radius,
            List<Vector2> points, int[,] grid)
        {
            if (candidate.x < 0 || candidate.x >= sampleRegionSize.x ||
                candidate.y < 0 || candidate.y >= sampleRegionSize.y)
                return false;

            int cellX = (int)(candidate.x / cellSize);
            int cellY = (int)(candidate.y / cellSize);
            int searchStartX = Mathf.Max(0, cellX - 2);
            int searchEndX = Mathf.Min(cellX + 2, grid.GetLength(0) - 1);
            int searchStartY = Mathf.Max(0, cellY - 2);
            int searchEndY = Mathf.Min(cellY + 2, grid.GetLength(1) - 1);

            for (int x = searchStartX; x <= searchEndX; x++)
            {
                for (int y = searchStartY; y <= searchEndY; y++)
                {
                    int pointIndex = grid[x, y] - 1;
                    if (pointIndex != -1)
                    {
                        float sqrDist = (candidate - points[pointIndex]).sqrMagnitude;
                        if (sqrDist < radius * radius)
                            return false;
                    }
                }
            }

            return true;
        }

        #endregion
    }
}