// ==================== Unity 可序列化类型 ====================
// 注意：这些类型提供了 Unity 内置类型的可序列化替代方案

using System;
using UnityEngine;

namespace DataSerialization
{
     /// <summary>
        /// 可序列化的 Vector2 替代品
        /// </summary>
        [Serializable]

    public struct SerializableVector2
    {
        public float x, y;

        public SerializableVector2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public static implicit operator Vector2(SerializableVector2 s) => new Vector2(s.x, s.y);
        public static implicit operator SerializableVector2(Vector2 v) => new SerializableVector2(v.x, v.y);
        public override string ToString() => $"({x:F2}, {y:F2})";
    }

    /// <summary>
    /// 可序列化的 Vector3 替代品
    /// </summary>
    [Serializable]
    public struct SerializableVector3
    {
        public float x, y, z;

        public SerializableVector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static implicit operator Vector3(SerializableVector3 s) => new Vector3(s.x, s.y, s.z);
        public static implicit operator SerializableVector3(Vector3 v) => new SerializableVector3(v.x, v.y, v.z);
        public override string ToString() => $"({x:F2}, {y:F2}, {z:F2})";
    }

    /// <summary>
    /// 可序列化的 Vector2Int 替代品
    /// </summary>
    [Serializable]
    public struct SerializableVector2Int
    {
        public int x, y;

        public SerializableVector2Int(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public static implicit operator Vector2Int(SerializableVector2Int s) => new Vector2Int(s.x, s.y);
        public static implicit operator SerializableVector2Int(Vector2Int v) => new SerializableVector2Int(v.x, v.y);
        public override string ToString() => $"({x}, {y})";
    }

    /// <summary>
    /// 可序列化的 Quaternion 替代品
    /// </summary>
    [Serializable]
    public struct SerializableQuaternion
    {
        public float x, y, z, w;

        public SerializableQuaternion(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public static implicit operator Quaternion(SerializableQuaternion s) => new Quaternion(s.x, s.y, s.z, s.w);

        public static implicit operator SerializableQuaternion(Quaternion q) =>
            new SerializableQuaternion(q.x, q.y, q.z, q.w);

        public override string ToString() => $"({x:F3}, {y:F3}, {z:F3}, {w:F3})";
    }

    /// <summary>
    /// 可序列化的 Color 替代品
    /// </summary>
    [Serializable]
    public struct SerializableColor
    {
        public float r, g, b, a;

        public SerializableColor(float r, float g, float b, float a = 1f)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }

        public static implicit operator Color(SerializableColor s) => new Color(s.r, s.g, s.b, s.a);
        public static implicit operator SerializableColor(Color c) => new SerializableColor(c.r, c.g, c.b, c.a);
        public override string ToString() => $"RGBA({r:F2}, {g:F2}, {b:F2}, {a:F2})";
    }

    /// <summary>
    /// 可序列化的 Vector4 替代品
    /// </summary>
    [Serializable]
    public struct SerializableVector4
    {
        public float x, y, z, w;

        public SerializableVector4(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public static implicit operator Vector4(SerializableVector4 s) => new Vector4(s.x, s.y, s.z, s.w);
        public static implicit operator SerializableVector4(Vector4 v) => new SerializableVector4(v.x, v.y, v.z, v.w);
        public override string ToString() => $"({x:F2}, {y:F2}, {z:F2}, {w:F2})";
    }

    /// <summary>
    /// 可序列化的 Vector3Int 替代品
    /// </summary>
    [Serializable]
    public struct SerializableVector3Int
    {
        public int x, y, z;

        public SerializableVector3Int(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static implicit operator Vector3Int(SerializableVector3Int s) => new Vector3Int(s.x, s.y, s.z);

        public static implicit operator SerializableVector3Int(Vector3Int v) =>
            new SerializableVector3Int(v.x, v.y, v.z);

        public override string ToString() => $"({x}, {y}, {z})";
    }

    /// <summary>
    /// 可序列化的 Rect 替代品
    /// </summary>
    [Serializable]
    public struct SerializableRect
    {
        public float x, y, width, height;

        public SerializableRect(float x, float y, float width, float height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }

        public static implicit operator Rect(SerializableRect s) => new Rect(s.x, s.y, s.width, s.height);
        public static implicit operator SerializableRect(Rect r) => new SerializableRect(r.x, r.y, r.width, r.height);
        public override string ToString() => $"(x:{x:F2}, y:{y:F2}, w:{width:F2}, h:{height:F2})";
    }

    /// <summary>
    /// 可序列化的 RectInt 替代品
    /// </summary>
    [Serializable]
    public struct SerializableRectInt
    {
        public int x, y, width, height;

        public SerializableRectInt(int x, int y, int width, int height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }

        public static implicit operator RectInt(SerializableRectInt s) => new RectInt(s.x, s.y, s.width, s.height);

        public static implicit operator SerializableRectInt(RectInt r) =>
            new SerializableRectInt(r.x, r.y, r.width, r.height);

        public override string ToString() => $"(x:{x}, y:{y}, w:{width}, h:{height})";
    }

    /// <summary>
    /// 可序列化的 Bounds 替代品
    /// </summary>
    [Serializable]
    public struct SerializableBounds
    {
        public SerializableVector3 center;
        public SerializableVector3 size;

        public SerializableBounds(SerializableVector3 center, SerializableVector3 size)
        {
            this.center = center;
            this.size = size;
        }

        public static implicit operator Bounds(SerializableBounds s) => new Bounds(s.center, s.size);
        public static implicit operator SerializableBounds(Bounds b) => new SerializableBounds(b.center, b.size);
        public override string ToString() => $"Center: {center}, Size: {size}";
    }

    /// <summary>
    /// 可序列化的 BoundsInt 替代品
    /// </summary>
    [Serializable]
    public struct SerializableBoundsInt
    {
        public SerializableVector3Int position;
        public SerializableVector3Int size;

        public SerializableBoundsInt(SerializableVector3Int position, SerializableVector3Int size)
        {
            this.position = position;
            this.size = size;
        }

        public static implicit operator BoundsInt(SerializableBoundsInt s) => new BoundsInt(s.position, s.size);

        public static implicit operator SerializableBoundsInt(BoundsInt b) =>
            new SerializableBoundsInt(b.position, b.size);

        public override string ToString() => $"Position: {position}, Size: {size}";
    }

    /// <summary>
    /// 可序列化的 Color32 替代品
    /// </summary>
    [Serializable]
    public struct SerializableColor32
    {
        public byte r, g, b, a;

        public SerializableColor32(byte r, byte g, byte b, byte a = 255)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }

        public static implicit operator Color32(SerializableColor32 s) => new Color32(s.r, s.g, s.b, s.a);
        public static implicit operator SerializableColor32(Color32 c) => new SerializableColor32(c.r, c.g, c.b, c.a);
        public override string ToString() => $"RGBA({r}, {g}, {b}, {a})";
    }

    /// <summary>
    /// 可序列化的 Matrix4x4 替代品
    /// </summary>
    [Serializable]
    public struct SerializableMatrix4x4
    {
        public float m00, m01, m02, m03;
        public float m10, m11, m12, m13;
        public float m20, m21, m22, m23;
        public float m30, m31, m32, m33;

        public SerializableMatrix4x4(Matrix4x4 matrix)
        {
            m00 = matrix.m00;
            m01 = matrix.m01;
            m02 = matrix.m02;
            m03 = matrix.m03;
            m10 = matrix.m10;
            m11 = matrix.m11;
            m12 = matrix.m12;
            m13 = matrix.m13;
            m20 = matrix.m20;
            m21 = matrix.m21;
            m22 = matrix.m22;
            m23 = matrix.m23;
            m30 = matrix.m30;
            m31 = matrix.m31;
            m32 = matrix.m32;
            m33 = matrix.m33;
        }

        public static implicit operator Matrix4x4(SerializableMatrix4x4 s)
        {
            Matrix4x4 matrix = new Matrix4x4();
            matrix.m00 = s.m00;
            matrix.m01 = s.m01;
            matrix.m02 = s.m02;
            matrix.m03 = s.m03;
            matrix.m10 = s.m10;
            matrix.m11 = s.m11;
            matrix.m12 = s.m12;
            matrix.m13 = s.m13;
            matrix.m20 = s.m20;
            matrix.m21 = s.m21;
            matrix.m22 = s.m22;
            matrix.m23 = s.m23;
            matrix.m30 = s.m30;
            matrix.m31 = s.m31;
            matrix.m32 = s.m32;
            matrix.m33 = s.m33;
            return matrix;
        }

        public static implicit operator SerializableMatrix4x4(Matrix4x4 matrix) => new SerializableMatrix4x4(matrix);
        public override string ToString() => $"Matrix4x4({m00:F2}, {m01:F2}, {m02:F2}, {m03:F2}...)";
    }

    /// <summary>
    /// 可序列化的 AnimationCurve 替代品
    /// </summary>
    [Serializable]
    public class SerializableAnimationCurve
    {
        public SerializableKeyframe[] keys;
        public WrapMode preWrapMode;
        public WrapMode postWrapMode;

        public SerializableAnimationCurve(AnimationCurve curve)
        {
            if (curve != null)
            {
                keys = new SerializableKeyframe[curve.length];
                for (int i = 0; i < curve.length; i++)
                {
                    keys[i] = curve[i];
                }

                preWrapMode = curve.preWrapMode;
                postWrapMode = curve.postWrapMode;
            }
            else
            {
                keys = new SerializableKeyframe[0];
                preWrapMode = WrapMode.Once;
                postWrapMode = WrapMode.Once;
            }
        }

        public static implicit operator AnimationCurve(SerializableAnimationCurve s)
        {
            if (s?.keys == null) return new AnimationCurve();

            Keyframe[] keyframes = new Keyframe[s.keys.Length];
            for (int i = 0; i < s.keys.Length; i++)
            {
                keyframes[i] = s.keys[i];
            }

            AnimationCurve curve = new AnimationCurve(keyframes);
            curve.preWrapMode = s.preWrapMode;
            curve.postWrapMode = s.postWrapMode;
            return curve;
        }

        public static implicit operator SerializableAnimationCurve(AnimationCurve curve) =>
            new SerializableAnimationCurve(curve);
    }

    /// <summary>
    /// 可序列化的 Keyframe 替代品
    /// </summary>
    [Serializable]
    public struct SerializableKeyframe
    {
        public float time;
        public float value;
        public float inTangent;
        public float outTangent;
        public int tangentMode;
        public int weightedMode;
        public float inWeight;
        public float outWeight;

        public SerializableKeyframe(Keyframe keyframe)
        {
            time = keyframe.time;
            value = keyframe.value;
            inTangent = keyframe.inTangent;
            outTangent = keyframe.outTangent;
            tangentMode = (int)keyframe.tangentMode;
            weightedMode = (int)keyframe.weightedMode;
            inWeight = keyframe.inWeight;
            outWeight = keyframe.outWeight;
        }

        public static implicit operator Keyframe(SerializableKeyframe s)
        {
            return new Keyframe(s.time, s.value, s.inTangent, s.outTangent)
            {
                tangentMode = s.tangentMode,
                weightedMode = (WeightedMode)s.weightedMode,
                inWeight = s.inWeight,
                outWeight = s.outWeight
            };
        }

        public static implicit operator SerializableKeyframe(Keyframe keyframe) => new SerializableKeyframe(keyframe);
        public override string ToString() => $"Keyframe(time:{time:F2}, value:{value:F2})";
    }

    /// <summary>
    /// 可序列化的 Gradient 替代品
    /// </summary>
    [Serializable]
    public class SerializableGradient
    {
        public SerializableGradientColorKey[] colorKeys;
        public SerializableGradientAlphaKey[] alphaKeys;
        public GradientMode mode;

        public SerializableGradient(Gradient gradient)
        {
            if (gradient != null)
            {
                colorKeys = new SerializableGradientColorKey[gradient.colorKeys.Length];
                for (int i = 0; i < gradient.colorKeys.Length; i++)
                {
                    colorKeys[i] = gradient.colorKeys[i];
                }

                alphaKeys = new SerializableGradientAlphaKey[gradient.alphaKeys.Length];
                for (int i = 0; i < gradient.alphaKeys.Length; i++)
                {
                    alphaKeys[i] = gradient.alphaKeys[i];
                }

                mode = gradient.mode;
            }
            else
            {
                colorKeys = new SerializableGradientColorKey[0];
                alphaKeys = new SerializableGradientAlphaKey[0];
                mode = GradientMode.Blend;
            }
        }

        public static implicit operator Gradient(SerializableGradient s)
        {
            if (s == null) return new Gradient();

            Gradient gradient = new Gradient();

            if (s.colorKeys != null)
            {
                GradientColorKey[] colorKeys = new GradientColorKey[s.colorKeys.Length];
                for (int i = 0; i < s.colorKeys.Length; i++)
                {
                    colorKeys[i] = s.colorKeys[i];
                }

                gradient.colorKeys = colorKeys;
            }

            if (s.alphaKeys != null)
            {
                GradientAlphaKey[] alphaKeys = new GradientAlphaKey[s.alphaKeys.Length];
                for (int i = 0; i < s.alphaKeys.Length; i++)
                {
                    alphaKeys[i] = s.alphaKeys[i];
                }

                gradient.alphaKeys = alphaKeys;
            }

            gradient.mode = s.mode;
            return gradient;
        }

        public static implicit operator SerializableGradient(Gradient gradient) => new SerializableGradient(gradient);
    }

    /// <summary>
    /// 可序列化的 GradientColorKey 替代品
    /// </summary>
    [Serializable]
    public struct SerializableGradientColorKey
    {
        public SerializableColor color;
        public float time;

        public SerializableGradientColorKey(GradientColorKey key)
        {
            color = key.color;
            time = key.time;
        }

        public static implicit operator GradientColorKey(SerializableGradientColorKey s) =>
            new GradientColorKey(s.color, s.time);

        public static implicit operator SerializableGradientColorKey(GradientColorKey key) =>
            new SerializableGradientColorKey(key);
    }

    /// <summary>
    /// 可序列化的 GradientAlphaKey 替代品
    /// </summary>
    [Serializable]
    public struct SerializableGradientAlphaKey
    {
        public float alpha;
        public float time;

        public SerializableGradientAlphaKey(GradientAlphaKey key)
        {
            alpha = key.alpha;
            time = key.time;
        }

        public static implicit operator GradientAlphaKey(SerializableGradientAlphaKey s) =>
            new GradientAlphaKey(s.alpha, s.time);

        public static implicit operator SerializableGradientAlphaKey(GradientAlphaKey key) =>
            new SerializableGradientAlphaKey(key);
    }
}