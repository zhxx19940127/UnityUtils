
using UnityEngine;

public class Math3d : MonoBehaviour
{
    private static Transform tempChild = null;
    private static Transform tempParent = null;

    // 初始化方法
    public static void Init()
    {
        tempChild = (new GameObject("Math3d_TempChild")).transform;
        tempParent = (new GameObject("Math3d_TempParent")).transform;

        tempChild.gameObject.hideFlags = HideFlags.HideAndDontSave;
        DontDestroyOnLoad(tempChild.gameObject);

        tempParent.gameObject.hideFlags = HideFlags.HideAndDontSave;
        DontDestroyOnLoad(tempParent.gameObject);

        // 设置父子关系
        tempChild.parent = tempParent;
    }

    // 增加或减少向量的长度
    public static Vector3 AddVectorLength(Vector3 vector, float size)
    {
        // 获取向量的长度
        float magnitude = Vector3.Magnitude(vector);

        // 改变长度
        magnitude += size;

        // 归一化向量
        Vector3 vectorNormalized = Vector3.Normalize(vector);

        // 缩放向量
        return Vector3.Scale(vectorNormalized, new Vector3(magnitude, magnitude, magnitude));
    }

    // 创建一个方向为 "vector"、长度为 "size" 的向量
    public static Vector3 SetVectorLength(Vector3 vector, float size)
    {
        // 归一化向量
        Vector3 vectorNormalized = Vector3.Normalize(vector);

        // 缩放向量
        vectorNormalized = vectorNormalized * size;
        return vectorNormalized;
    }

    // 计算从 A 到 B 的旋转差
    public static Quaternion SubtractRotation(Quaternion B, Quaternion A)
    {
        Quaternion C = Quaternion.Inverse(A) * B;
        return C;
    }

    // 计算两个平面的交线。平面由法线和一个平面上的点定义。
    // 输出为交线上的一个点和表示方向的向量。如果平面不平行，函数返回 true，否则返回 false。
    public static bool PlanePlaneIntersection(Vector3 linePoint, Vector3 lineVec, Vector3 plane1Normal, Vector3 plane1Position, Vector3 plane2Normal, Vector3 plane2Position)
    {
        linePoint = Vector3.zero;
        lineVec = Vector3.zero;

        // 通过计算两个平面法线的叉积，可以得到交线的方向向量。注意这只是一个方向，线在空间中尚未固定位置。我们需要一个点来确定它的位置。
        lineVec = Vector3.Cross(plane1Normal, plane2Normal);

        // 接下来需要计算一个点来固定线在空间中的位置。这可以通过找到一个从平面2位置出发、平行于平面2且与平面1相交的向量来实现。为了避免舍入误差，这个向量还必须与交线方向垂直。可以通过计算平面2法线与交线方向的叉积来得到这个向量。
        Vector3 ldir = Vector3.Cross(plane2Normal, lineVec);

        float denominator = Vector3.Dot(plane1Normal, ldir);

        // 防止除以零和舍入误差，要求平面之间的夹角至少约为 5 度。
        if (Mathf.Abs(denominator) > 0.006f)
        {
            Vector3 plane1ToPlane2 = plane1Position - plane2Position;
            float t = Vector3.Dot(plane1Normal, plane1ToPlane2) / denominator;
            linePoint = plane2Position + t * ldir;

            return true;
        }
        else
        {
            // 输出无效
            return false;
        }
    }

    // 计算直线与平面的交点。
    // 如果直线与平面不平行，函数返回 true，否则返回 false。
    public static bool LinePlaneIntersection(Vector3 intersection, Vector3 linePoint, Vector3 lineVec, Vector3 planeNormal, Vector3 planePoint)
    {
        float length;
        float dotNumerator;
        float dotDenominator;
        Vector3 vector;
        intersection = Vector3.zero;

        // 计算从直线上的点到直线与平面交点的距离
        dotNumerator = Vector3.Dot((planePoint - linePoint), planeNormal);
        dotDenominator = Vector3.Dot(lineVec, planeNormal);

        // 直线与平面不平行
        if (dotDenominator != 0.0f)
        {
            length = dotNumerator / dotDenominator;

            // 创建从直线上的点到交点的向量
            vector = SetVectorLength(lineVec, length);

            // 获取直线与平面交点的坐标
            intersection = linePoint + vector;

            return true;
        }
        else
        {
            // 输出无效
            return false;
        }
    }

    // 计算两条直线的交点。如果直线相交，返回 true，否则返回 false。
    // 注意在三维空间中，两条直线大多数情况下不会相交。如果两条直线不在同一平面上，请使用 ClosestPointsOnTwoLines()。
    public static bool LineLineIntersection(Vector3 intersection, Vector3 linePoint1, Vector3 lineVec1, Vector3 linePoint2, Vector3 lineVec2)
    {
        intersection = Vector3.zero;

        Vector3 lineVec3 = linePoint2 - linePoint1;
        Vector3 crossVec1and2 = Vector3.Cross(lineVec1, lineVec2);
        Vector3 crossVec3and2 = Vector3.Cross(lineVec3, lineVec2);

        float planarFactor = Vector3.Dot(lineVec3, crossVec1and2);

        // 直线不在同一平面上。考虑舍入误差。
        if ((planarFactor >= 0.00001f) || (planarFactor <= -0.00001f))
        {
            return false;
        }

        // 注意：sqrMagnitude 对输入向量执行 x*x + y*y + z*z。
        float s = Vector3.Dot(crossVec3and2, crossVec1and2) / crossVec1and2.sqrMagnitude;

        if ((s >= 0.0f) && (s <= 1.0f))
        {
            intersection = linePoint1 + (lineVec1 * s);
            return true;
        }
        else
        {
            return false;
        }
    }

    // 两条不平行的直线（无论是否相交）都有两个点，它们彼此最近。
    // 如果直线不平行，函数返回 true，否则返回 false。
    public static bool ClosestPointsOnTwoLines(Vector3 closestPointLine1, Vector3 closestPointLine2, Vector3 linePoint1, Vector3 lineVec1, Vector3 linePoint2, Vector3 lineVec2)
    {
        closestPointLine1 = Vector3.zero;
        closestPointLine2 = Vector3.zero;

        float a = Vector3.Dot(lineVec1, lineVec1);
        float b = Vector3.Dot(lineVec1, lineVec2);
        float e = Vector3.Dot(lineVec2, lineVec2);

        float d = a * e - b * b;

        // 直线不平行
        if (d != 0.0f)
        {
            Vector3 r = linePoint1 - linePoint2;
            float c = Vector3.Dot(lineVec1, r);
            float f = Vector3.Dot(lineVec2, r);

            float s = (b * f - c * e) / d;
            float t = (a * f - c * b) / d;

            closestPointLine1 = linePoint1 + lineVec1 * s;
            closestPointLine2 = linePoint2 + lineVec2 * t;

            return true;
        }
        else
        {
            return false;
        }
    }

    // 将一个点投影到一条直线上。
    public static Vector3 ProjectPointOnLine(Vector3 linePoint, Vector3 lineVec, Vector3 point)
    {
        // 获取从直线上的点到空间中的点的向量
        Vector3 linePointToPoint = point - linePoint;

        float t = Vector3.Dot(linePointToPoint, lineVec);

        return linePoint + lineVec * t;
    }

    // 将一个点投影到一个平面上。
    public static Vector3 ProjectPointOnPlane(Vector3 planeNormal, Vector3 planePoint, Vector3 point)
    {
        // float distance;
        // Vector3 translationVector;
        // 
        // // 首先计算点到平面的距离：
        // distance = SignedDistancePlanePoint(planeNormal, planePoint, point);
        // 
        // // 反转距离的符号
        // distance *= -1;
        // 
        // // 获取一个平移向量
        // translationVector = SetVectorLength(planeNormal, distance);
        // 
        // // 平移点以形成投影
        // return point + translationVector;
        return new Vector3(0, 0, 0);
    }

    // 将一个向量投影到一个平面上。输出未归一化。
    public static Vector3 ProjectVectorOnPlane(Vector3 planeNormal, Vector3 vector)
    {
        return vector - (Vector3.Dot(vector, planeNormal) * planeNormal);
    }

    // 计算带符号的点积（+ 或 - 符号，而不是模糊不清）。主要用于判断一个向量相对于另一个向量是位于左侧还是右侧。
    // 这是通过计算一个与其中一个向量垂直的向量并将其作为参考来实现的。因为点积只有在角度超过或小于 90 度时才具有符号信息。
    public static float SignedDotProduct(Vector3 vectorA, Vector3 vectorB, Vector3 normal)
    {
        Vector3 perpVector;
        float dot;

        // 使用几何对象的法线和一个输入向量计算垂直向量
        perpVector = Vector3.Cross(normal, vectorA);

        // 现在计算垂直向量（perpVector）与另一个输入向量的点积
        dot = Vector3.Dot(perpVector, vectorB);

        return dot;
    }

    // 将由三个点定义的平面转换为由向量和点定义的平面。
    // 平面点是三个点定义的三角形的中心。
    public static void PlaneFrom3Points(Vector3 planeNormal, Vector3 planePoint, Vector3 pointA, Vector3 pointB, Vector3 pointC)
    {
        planeNormal = Vector3.zero;
        planePoint = Vector3.zero;

        // 从三个输入点中生成两个向量，起点为点 A
        Vector3 AB = pointB - pointA;
        Vector3 AC = pointC - pointA;

        // 计算法线
        planeNormal = Vector3.Normalize(Vector3.Cross(AB, AC));

        // 获取 AB 和 AC 的中点
        Vector3 middleAB = pointA + (AB / 2.0f);
        Vector3 middleAC = pointA + (AC / 2.0f);

        // 获取从 AB 和 AC 的中点到不在该线上的点的向量
        Vector3 middleABtoC = pointC - middleAB;
        Vector3 middleACtoB = pointB - middleAC;

        // 计算两条线的交点。这将是三个点定义的三角形的中心。
        // 我们可以使用 LineLineIntersection 而不是 ClosestPointsOnTwoLines，但由于舍入误差，有时这不起作用。
        Vector3 temp = Vector3.zero;
        ClosestPointsOnTwoLines(planePoint, temp, middleAB, middleABtoC, middleAC, middleACtoB);
    }

    // 返回四元数的向前向量
    public static Vector3 GetForwardVector(Quaternion q)
    {
        return q * Vector3.forward;
    }

    // 返回四元数的向上向量
    public static Vector3 GetUpVector(Quaternion q)
    {
        return q * Vector3.up;
    }

    // 返回四元数的向右向量
    public static Vector3 GetRightVector(Quaternion q)
    {
        return q * Vector3.right;
    }

    // 从矩阵中获取四元数
    public static Quaternion QuaternionFromMatrix(Matrix4x4 m)
    {
        return Quaternion.LookRotation(m.GetColumn(2), m.GetColumn(1));
    }

    // 从矩阵中获取位置
    public static Vector3 PositionFromMatrix(Matrix4x4 m)
    {
        Vector4 vector4Position = m.GetColumn(3);
        return new Vector3(vector4Position.x, vector4Position.y, vector4Position.z);
    }

    // 这是 Quaternion.LookRotation 的替代方法。与其将游戏对象的向前和向上向量与输入向量对齐，可以使用自定义方向代替固定的向前和向上向量。
    // alignWithVector 和 alignWithNormal 是世界空间中的向量。
    // customForward 和 customUp 是对象空间中的向量。
    // 使用方法：将 alignWithVector 和 alignWithNormal 视为使用默认 LookRotation 函数。
    // 将 customForward 和 customUp 设置为你希望使用的向量，而不是默认的向前和向上向量。
    public static void LookRotationExtended(GameObject gameObjectInOut, Vector3 alignWithVector, Vector3 alignWithNormal, Vector3 customForward, Vector3 customUp)
    {
        // 设置目标的旋转
        Quaternion rotationA = Quaternion.LookRotation(alignWithVector, alignWithNormal);

        // 设置自定义法线和向上向量的旋转。
        // 如果使用默认的 LookRotation 函数，这将被硬编码为向前和向上向量。
        Quaternion rotationB = Quaternion.LookRotation(customForward, customUp);

        // 计算旋转
        gameObjectInOut.transform.rotation = rotationA * Quaternion.Inverse(rotationB);
    }

    // trianglePosition 可以位于任意位置，不一定是顶点或三角形的中心。
    public static void PreciseAlign(GameObject gameObjectInOut, Vector3 alignWithVector, Vector3 alignWithNormal, Vector3 alignWithPosition, Vector3 triangleForward, Vector3 triangleNormal, Vector3 trianglePosition)
    {
        // 设置旋转
        LookRotationExtended(gameObjectInOut, alignWithVector, alignWithNormal, triangleForward, triangleNormal);

        // 获取 trianglePosition 的世界空间位置
        Vector3 trianglePositionWorld = gameObjectInOut.transform.TransformPoint(trianglePosition);

        // 获取从 trianglePosition 到 alignWithPosition 的向量
        Vector3 translateVector = alignWithPosition - trianglePositionWorld;

        // 现在平移对象，使三角形正确对齐。
        gameObjectInOut.transform.Translate(translateVector, Space.World);
    }
}