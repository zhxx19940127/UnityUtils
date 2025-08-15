namespace GameObjectToolkit
{
    using UnityEngine;
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// 随机生成工具类
    /// 提供全面的随机数据生成功能
    /// </summary>
    public static class RandomUtils
    {
        #region 基础随机数

        /// <summary>
        /// 生成指定范围内的随机浮点数
        /// </summary>
        /// <param name="min">最小值</param>
        /// <param name="max">最大值</param>
        /// <param name="precision">小数位数</param>
        public static float Range(float min, float max, int precision = 4)
        {
            float value = UnityEngine.Random.Range(min, max);
            return (float)Math.Round(value, precision);
        }

        /// <summary>
        /// 生成指定范围内的随机整数
        /// </summary>
        public static int Range(int min, int max)
        {
            return UnityEngine.Random.Range(min, max);
        }

        /// <summary>
        /// 生成随机布尔值
        /// </summary>
        /// <param name="probability">为true的概率[0-1]</param>
        public static bool Bool(float probability = 0.5f)
        {
            return UnityEngine.Random.value < probability;
        }

        /// <summary>
        /// 生成服从正态分布的随机数
        /// </summary>
        /// <param name="mean">均值</param>
        /// <param name="stdDev">标准差</param>
        public static float Gaussian(float mean = 0f, float stdDev = 1f)
        {
            float u1 = 1f - UnityEngine.Random.value;
            float u2 = 1f - UnityEngine.Random.value;
            float randStdNormal = Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Sin(2f * Mathf.PI * u2);
            return mean + stdDev * randStdNormal;
        }

        #endregion

        #region 随机集合操作

        /// <summary>
        /// 从列表中随机选择一个元素
        /// </summary>
        public static T Choice<T>(List<T> list)
        {
            if (list == null || list.Count == 0)
                throw new ArgumentException("列表不能为空");

            return list[UnityEngine.Random.Range(0, list.Count)];
        }

        /// <summary>
        /// 从列表中随机选择多个不重复的元素
        /// </summary>
        /// <param name="count">要选择的数量</param>
        public static List<T> Choices<T>(List<T> list, int count)
        {
            if (list == null || list.Count == 0 || count <= 0)
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

        /// <summary>
        /// 根据权重随机选择列表中的元素
        /// </summary>
        /// <param name="items">元素列表</param>
        /// <param name="weights">对应的权重列表</param>
        public static T WeightedChoice<T>(List<T> items, List<float> weights)
        {
            if (items == null || weights == null || items.Count != weights.Count || items.Count == 0)
                throw new ArgumentException("参数无效");

            float totalWeight = 0f;
            foreach (float w in weights)
                totalWeight += w;

            float randomValue = UnityEngine.Random.Range(0f, totalWeight);
            float cumulativeWeight = 0f;

            for (int i = 0; i < items.Count; i++)
            {
                cumulativeWeight += weights[i];
                if (randomValue <= cumulativeWeight)
                    return items[i];
            }

            return items[items.Count - 1];
        }

        /// <summary>
        /// 打乱列表顺序（Fisher-Yates洗牌算法）
        /// </summary>
        public static void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }

        #endregion

        #region 随机几何图形

        /// <summary>
        /// 生成单位圆内的随机点
        /// </summary>
        /// <param name="radius">圆半径</param>
        public static Vector2 InsideUnitCircle(float radius = 1f)
        {
            return UnityEngine.Random.insideUnitCircle * radius;
        }

        /// <summary>
        /// 生成单位球体内的随机点
        /// </summary>
        /// <param name="radius">球半径</param>
        public static Vector3 InsideUnitSphere(float radius = 1f)
        {
            return UnityEngine.Random.insideUnitSphere * radius;
        }

        /// <summary>
        /// 生成球体表面的随机点
        /// </summary>
        /// <param name="radius">球半径</param>
        public static Vector3 OnUnitSphere(float radius = 1f)
        {
            return UnityEngine.Random.onUnitSphere * radius;
        }

        /// <summary>
        /// 生成圆环内的随机点
        /// </summary>
        /// <param name="innerRadius">内半径</param>
        /// <param name="outerRadius">外半径</param>
        public static Vector2 InsideAnnulus(float innerRadius, float outerRadius)
        {
            float angle = UnityEngine.Random.Range(0f, 2f * Mathf.PI);
            float radius = Mathf.Sqrt(UnityEngine.Random.Range(innerRadius * innerRadius, outerRadius * outerRadius));
            return new Vector2(radius * Mathf.Cos(angle), radius * Mathf.Sin(angle));
        }

        /// <summary>
        /// 生成矩形内的随机点
        /// </summary>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        public static Vector2 InsideRectangle(float width, float height)
        {
            return new Vector2(
                UnityEngine.Random.Range(-width / 2f, width / 2f),
                UnityEngine.Random.Range(-height / 2f, height / 2f)
            );
        }

        /// <summary>
        /// 生成立方体内的随机点
        /// </summary>
        public static Vector3 InsideCube(float size)
        {
            return new Vector3(
                UnityEngine.Random.Range(-size / 2f, size / 2f),
                UnityEngine.Random.Range(-size / 2f, size / 2f),
                UnityEngine.Random.Range(-size / 2f, size / 2f)
            );
        }

        #endregion

        #region 随机颜色

        /// <summary>
        /// 生成随机RGB颜色
        /// </summary>
        /// <param name="alpha">透明度[0-1]</param>
        public static Color Color(float alpha = 1f)
        {
            return new Color(
                UnityEngine.Random.value,
                UnityEngine.Random.value,
                UnityEngine.Random.value,
                alpha
            );
        }

        /// <summary>
        /// 生成随机HSV颜色
        /// </summary>
        /// <param name="hueMin">最小色相[0-1]</param>
        /// <param name="hueMax">最大色相[0-1]</param>
        /// <param name="saturationMin">最小饱和度[0-1]</param>
        /// <param name="saturationMax">最大饱和度[0-1]</param>
        /// <param name="valueMin">最小明度[0-1]</param>
        /// <param name="valueMax">最大明度[0-1]</param>
        public static Color ColorHsv(
            float hueMin = 0f, float hueMax = 1f,
            float saturationMin = 0.7f, float saturationMax = 1f,
            float valueMin = 0.7f, float valueMax = 1f)
        {
            float h = UnityEngine.Random.Range(hueMin, hueMax);
            float s = UnityEngine.Random.Range(saturationMin, saturationMax);
            float v = UnityEngine.Random.Range(valueMin, valueMax);
            return HsvToRgb(h, s, v);
        }

        /// <summary>
        /// 生成随机暖色调颜色
        /// </summary>
        public static Color WarmColor()
        {
            return ColorHsv(0f, 0.2f, 0.7f, 1f, 0.7f, 1f);
        }

        /// <summary>
        /// 生成随机冷色调颜色
        /// </summary>
        public static Color CoolColor()
        {
            return ColorHsv(0.5f, 0.7f, 0.7f, 1f, 0.7f, 1f);
        }

        /// <summary>
        /// 生成随机灰度颜色
        /// </summary>
        public static Color Grayscale()
        {
            float value = UnityEngine.Random.value;
            return new Color(value, value, value);
        }

        private static Color HsvToRgb(float h, float s, float v)
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

        #region 随机名称与字符串

        /// <summary>
        /// 生成随机字符串
        /// </summary>
        /// <param name="length">字符串长度</param>
        /// <param name="charset">字符集</param>
        public static string String(int length,
            string charset = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789")
        {
            StringBuilder sb = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                sb.Append(charset[UnityEngine.Random.Range(0, charset.Length)]);
            }

            return sb.ToString();
        }

        #endregion

        #region 随机日期与时间

        /// <summary>
        /// 生成随机日期（修正版）
        /// </summary>
        /// <param name="startYear">起始年份（包含）</param>
        /// <param name="endYear">结束年份（包含）</param>
        /// <returns>随机生成的日期</returns>
        public static DateTime RandomDate(int startYear = 1900, int endYear = 2100)
        {
            // 验证输入范围
            if (startYear > endYear)
            {
                throw new System.ArgumentException("起始年份不能大于结束年份");
            }

            // 生成年份（UnityEngine.Random.Range的max是exclusive，所以要+1）
            int year = UnityEngine.Random.Range(startYear, endYear + 1);

            // 生成月份（1-12）
            int month = UnityEngine.Random.Range(1, 13);

            // 获取该月的实际天数（自动处理闰年）
            int daysInMonth = System.DateTime.DaysInMonth(year, month);

            // 生成日期（注意Random.Range的max是exclusive，所以要+1）
            int day = UnityEngine.Random.Range(1, daysInMonth + 1);

            return new System.DateTime(year, month, day);
        }

        /// <summary>
        /// 生成随机时间
        /// </summary>
        /// <param name="includeSeconds">是否包含秒</param>
        public static TimeSpan Time(bool includeSeconds = true)
        {
            int hour = UnityEngine.Random.Range(0, 24);
            int minute = UnityEngine.Random.Range(0, 60);
            int second = includeSeconds ? UnityEngine.Random.Range(0, 60) : 0;
            return new TimeSpan(hour, minute, second);
        }

        /// <summary>
        /// 生成随机日期时间
        /// </summary>
        public static System.DateTime RandomDateTime()
        {
            return RandomDate() + Time();
        }

        #endregion

        #region 高级随机分布

        /// <summary>
        /// 生成泊松圆盘采样点
        /// </summary>
        /// <param name="radius">点之间的最小距离</param>
        /// <param name="regionSize">采样区域大小</param>
        /// <param name="rejectionLimit">采样失败次数限制</param>
        public static List<Vector2> PoissonDiscSampling(float radius, Vector2 regionSize, int rejectionLimit = 30)
        {
            float cellSize = radius / Mathf.Sqrt(2);
            int[,] grid = new int[
                Mathf.CeilToInt(regionSize.x / cellSize),
                Mathf.CeilToInt(regionSize.y / cellSize)
            ];

            List<Vector2> points = new List<Vector2>();
            List<Vector2> spawnPoints = new List<Vector2> { regionSize / 2 };

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

                    if (IsValidCandidate(candidate, regionSize, cellSize, radius, points, grid))
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

        private static bool IsValidCandidate(Vector2 candidate, Vector2 regionSize,
            float cellSize, float radius,
            List<Vector2> points, int[,] grid)
        {
            if (candidate.x < 0 || candidate.x >= regionSize.x ||
                candidate.y < 0 || candidate.y >= regionSize.y)
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

        /// <summary>
        /// 生成柏林噪声图
        /// </summary>
        /// <param name="width">图宽度</param>
        /// <param name="height">图高度</param>
        /// <param name="scale">缩放系数</param>
        /// <param name="octaves">噪声层数</param>
        /// <param name="persistence">持久度</param>
        /// <param name="lacunarity">间隙度</param>
        public static float[,] PerlinNoiseMap(int width, int height, float scale = 20f,
            int octaves = 4, float persistence = 0.5f, float lacunarity = 2f)
        {
            float[,] noiseMap = new float[width, height];

            if (scale <= 0)
                scale = 0.0001f;

            float maxNoiseHeight = float.MinValue;
            float minNoiseHeight = float.MaxValue;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float amplitude = 1;
                    float frequency = 1;
                    float noiseHeight = 0;

                    for (int i = 0; i < octaves; i++)
                    {
                        float sampleX = x / scale * frequency;
                        float sampleY = y / scale * frequency;

                        float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                        noiseHeight += perlinValue * amplitude;

                        amplitude *= persistence;
                        frequency *= lacunarity;
                    }

                    if (noiseHeight > maxNoiseHeight)
                        maxNoiseHeight = noiseHeight;
                    else if (noiseHeight < minNoiseHeight)
                        minNoiseHeight = noiseHeight;

                    noiseMap[x, y] = noiseHeight;
                }
            }

            // 标准化到0-1范围
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    noiseMap[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]);
                }
            }

            return noiseMap;
        }

        #endregion
    }
}