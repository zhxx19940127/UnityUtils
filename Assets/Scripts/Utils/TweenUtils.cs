namespace GameObjectToolkit
{
    using UnityEngine;
    using System.Collections;

    /// <summary>
    /// Unity 补间动画工具类
    /// 提供各种常用的动画效果实现
    /// </summary>
    public static class TweenUtils
    {
        #region 基础补间动画

        /// <summary>
        /// 移动对象到目标位置（带缓动效果）
        /// </summary>
        /// <param name="target">目标对象</param>
        /// <param name="endPos">目标位置</param>
        /// <param name="duration">持续时间（秒）</param>
        /// <param name="easing">缓动函数</param>
        /// <returns>协程</returns>
        public static IEnumerator MoveTo(Transform target, Vector3 endPos, float duration,
            EasingFunction easing = EasingFunction.Linear)
        {
            if (target == null) yield break;

            Vector3 startPos = target.position;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                target.position = Vector3.Lerp(startPos, endPos, ApplyEasing(t, easing));
                yield return null;
            }

            target.position = endPos;
        }

        /// <summary>
        /// 缩放对象到目标大小（带缓动效果）
        /// </summary>
        /// <param name="target">目标对象</param>
        /// <param name="endScale">目标缩放</param>
        /// <param name="duration">持续时间（秒）</param>
        /// <param name="easing">缓动函数</param>
        /// <returns>协程</returns>
        public static IEnumerator ScaleTo( Transform target, Vector3 endScale, float duration,
            EasingFunction easing = EasingFunction.Linear)
        {
            if (target == null) yield break;

            Vector3 startScale = target.localScale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                target.localScale = Vector3.Lerp(startScale, endScale, ApplyEasing(t, easing));
                yield return null;
            }

            target.localScale = endScale;
        }

        /// <summary>
        /// 旋转对象到目标角度（带缓动效果）
        /// </summary>
        /// <param name="target">目标对象</param>
        /// <param name="endRotation">目标旋转</param>
        /// <param name="duration">持续时间（秒）</param>
        /// <param name="easing">缓动函数</param>
        /// <returns>协程</returns>
        public static IEnumerator RotateTo(Transform target, Quaternion endRotation, float duration,
            EasingFunction easing = EasingFunction.Linear)
        {
            if (target == null) yield break;

            Quaternion startRotation = target.rotation;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                target.rotation = Quaternion.Lerp(startRotation, endRotation, ApplyEasing(t, easing));
                yield return null;
            }

            target.rotation = endRotation;
        }

        /// <summary>
        /// 改变颜色（带缓动效果）
        /// </summary>
        /// <param name="material">目标材质</param>
        /// <param name="propertyName">颜色属性名</param>
        /// <param name="endColor">目标颜色</param>
        /// <param name="duration">持续时间（秒）</param>
        /// <param name="easing">缓动函数</param>
        /// <returns>协程</returns>
        public static IEnumerator ChangeColor(Material material, string propertyName, Color endColor, float duration,
            EasingFunction easing = EasingFunction.Linear)
        {
            if (material == null || !material.HasProperty(propertyName)) yield break;

            Color startColor = material.GetColor(propertyName);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                material.SetColor(propertyName, Color.Lerp(startColor, endColor, ApplyEasing(t, easing)));
                yield return null;
            }

            material.SetColor(propertyName, endColor);
        }

        #endregion

        #region 高级补间动画

        /// <summary>
        /// 弹性移动动画
        /// </summary>
        /// <param name="target">目标对象</param>
        /// <param name="endPos">目标位置</param>
        /// <param name="duration">持续时间（秒）</param>
        /// <param name="overshoot">过冲量（弹性效果强度）</param>
        /// <returns>协程</returns>
        public static IEnumerator ElasticMove(Transform target, Vector3 endPos, float duration, float overshoot = 1.2f)
        {
            if (target == null) yield break;

            Vector3 startPos = target.position;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = ApplyEasing(t, EasingFunction.ElasticOut, overshoot);
                target.position = Vector3.Lerp(startPos, endPos, t);
                yield return null;
            }

            target.position = endPos;
        }

        /// <summary>
        /// 弹跳缩放动画
        /// </summary>
        /// <param name="target">目标对象</param>
        /// <param name="endScale">目标缩放</param>
        /// <param name="duration">持续时间（秒）</param>
        /// <param name="bounces">弹跳次数</param>
        /// <returns>协程</returns>
        public static IEnumerator BounceScale(Transform target, Vector3 endScale, float duration, int bounces = 2)
        {
            if (target == null) yield break;

            Vector3 startScale = target.localScale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = ApplyEasing(t, EasingFunction.BounceOut, bounces);
                target.localScale = Vector3.Lerp(startScale, endScale, t);
                yield return null;
            }

            target.localScale = endScale;
        }

        /// <summary>
        /// 路径移动动画
        /// </summary>
        /// <param name="target">目标对象</param>
        /// <param name="path">路径点数组</param>
        /// <param name="duration">持续时间（秒）</param>
        /// <param name="easing">缓动函数</param>
        /// <param name="loop">是否循环</param>
        /// <returns>协程</returns>
        public static IEnumerator MoveAlongPath(Transform target, Vector3[] path, float duration,
            EasingFunction easing = EasingFunction.Linear, bool loop = false)
        {
            if (target == null || path == null || path.Length < 2) yield break;

            do
            {
                for (int i = 0; i < path.Length - 1; i++)
                {
                    Vector3 startPos = path[i];
                    Vector3 endPos = path[i + 1];
                    float segmentDuration = duration / (path.Length - 1);
                    float elapsed = 0f;

                    while (elapsed < segmentDuration)
                    {
                        elapsed += Time.deltaTime;
                        float t = Mathf.Clamp01(elapsed / segmentDuration);
                        target.position = Vector3.Lerp(startPos, endPos, ApplyEasing(t, easing));
                        yield return null;
                    }
                }
            } while (loop);
        }

        #endregion

        #region 缓动函数

        /// <summary>
        /// 缓动函数类型
        /// </summary>
        public enum EasingFunction
        {
            Linear,
            EaseInQuad,
            EaseOutQuad,
            EaseInOutQuad,
            EaseInCubic,
            EaseOutCubic,
            EaseInOutCubic,
            EaseInQuart,
            EaseOutQuart,
            EaseInOutQuart,
            EaseInQuint,
            EaseOutQuint,
            EaseInOutQuint,
            EaseInSine,
            EaseOutSine,
            EaseInOutSine,
            EaseInExpo,
            EaseOutExpo,
            EaseInOutExpo,
            EaseInCirc,
            EaseOutCirc,
            EaseInOutCirc,
            ElasticIn,
            ElasticOut,
            ElasticInOut,
            BounceIn,
            BounceOut,
            BounceInOut
        }

        /// <summary>
        /// 应用缓动函数
        /// </summary>
        /// <param name="t">时间参数（0-1）</param>
        /// <param name="easing">缓动函数类型</param>
        /// <param name="customParam">自定义参数（某些缓动函数需要）</param>
        /// <returns>应用缓动后的值</returns>
        public static float ApplyEasing(float t, EasingFunction easing, float customParam = 1.0f)
        {
            switch (easing)
            {
                case EasingFunction.Linear:
                    return t;
                case EasingFunction.EaseInQuad:
                    return t * t;
                case EasingFunction.EaseOutQuad:
                    return t * (2 - t);
                case EasingFunction.EaseInOutQuad:
                    return t < 0.5f ? 2 * t * t : -1 + (4 - 2 * t) * t;
                case EasingFunction.EaseInCubic:
                    return t * t * t;
                case EasingFunction.EaseOutCubic:
                    return (--t) * t * t + 1;
                case EasingFunction.EaseInOutCubic:
                    return t < 0.5f ? 4 * t * t * t : (t - 1) * (2 * t - 2) * (2 * t - 2) + 1;
                case EasingFunction.EaseInQuart:
                    return t * t * t * t;
                case EasingFunction.EaseOutQuart:
                    return 1 - (--t) * t * t * t;
                case EasingFunction.EaseInOutQuart:
                    return t < 0.5f ? 8 * t * t * t * t : 1 - 8 * (--t) * t * t * t;
                case EasingFunction.EaseInQuint:
                    return t * t * t * t * t;
                case EasingFunction.EaseOutQuint:
                    return 1 + (--t) * t * t * t * t;
                case EasingFunction.EaseInOutQuint:
                    return t < 0.5f ? 16 * t * t * t * t * t : 1 + 16 * (--t) * t * t * t * t;
                case EasingFunction.EaseInSine:
                    return 1 - Mathf.Cos(t * Mathf.PI / 2);
                case EasingFunction.EaseOutSine:
                    return Mathf.Sin(t * Mathf.PI / 2);
                case EasingFunction.EaseInOutSine:
                    return -(Mathf.Cos(Mathf.PI * t) - 1) / 2;
                case EasingFunction.EaseInExpo:
                    return t == 0 ? 0 : Mathf.Pow(2, 10 * (t - 1));
                case EasingFunction.EaseOutExpo:
                    return t == 1 ? 1 : 1 - Mathf.Pow(2, -10 * t);
                case EasingFunction.EaseInOutExpo:
                    if (t == 0) return 0;
                    if (t == 1) return 1;
                    return t < 0.5f ? Mathf.Pow(2, 20 * t - 10) / 2 : (2 - Mathf.Pow(2, -20 * t + 10)) / 2;
                case EasingFunction.EaseInCirc:
                    return 1 - Mathf.Sqrt(1 - t * t);
                case EasingFunction.EaseOutCirc:
                    return Mathf.Sqrt(1 - (--t) * t);
                case EasingFunction.EaseInOutCirc:
                    return t < 0.5f
                        ? (1 - Mathf.Sqrt(1 - 4 * t * t)) / 2
                        : (Mathf.Sqrt(1 - (-2 * t + 2) * (-2 * t + 2)) + 1) / 2;
                case EasingFunction.ElasticIn:
                    return ElasticIn(t, customParam);
                case EasingFunction.ElasticOut:
                    return ElasticOut(t, customParam);
                case EasingFunction.ElasticInOut:
                    return ElasticInOut(t, customParam);
                case EasingFunction.BounceIn:
                    return 1 - BounceOut(1 - t, customParam);
                case EasingFunction.BounceOut:
                    return BounceOut(t, customParam);
                case EasingFunction.BounceInOut:
                    return t < 0.5f
                        ? (1 - BounceOut(1 - 2 * t, customParam)) / 2
                        : (1 + BounceOut(2 * t - 1, customParam)) / 2;
                default:
                    return t;
            }
        }

        // 弹性缓入函数
        private static float ElasticIn(float t, float overshoot)
        {
            if (t == 0) return 0;
            if (t == 1) return 1;
            return -Mathf.Pow(2, 10 * (t -= 1)) * Mathf.Sin((t - overshoot / 4) * (2 * Mathf.PI) / overshoot);
        }

        // 弹性缓出函数
        private static float ElasticOut(float t, float overshoot)
        {
            if (t == 0) return 0;
            if (t == 1) return 1;
            return Mathf.Pow(2, -10 * t) * Mathf.Sin((t - overshoot / 4) * (2 * Mathf.PI) / overshoot) + 1;
        }

        // 弹性缓入缓出函数
        private static float ElasticInOut(float t, float overshoot)
        {
            if (t == 0) return 0;
            if (t == 1) return 1;
            t *= 2;
            if (t < 1)
                return -0.5f * Mathf.Pow(2, 10 * (t -= 1)) *
                       Mathf.Sin((t - overshoot / 4) * (2 * Mathf.PI) / overshoot);
            return Mathf.Pow(2, -10 * (t -= 1)) * Mathf.Sin((t - overshoot / 4) * (2 * Mathf.PI) / overshoot) * 0.5f +
                   1;
        }

        // 弹跳缓出函数
        private static float BounceOut(float t, float bounces)
        {
            if (t < 1 / 2.75f)
            {
                return 7.5625f * t * t;
            }
            else if (t < 2 / 2.75f)
            {
                t -= 1.5f / 2.75f;
                return 7.5625f * t * t + 0.75f;
            }
            else if (t < 2.5f / 2.75f)
            {
                t -= 2.25f / 2.75f;
                return 7.5625f * t * t + 0.9375f;
            }
            else
            {
                t -= 2.625f / 2.75f;
                return 7.5625f * t * t + 0.984375f;
            }
        }

        #endregion

        #region 动画控制

        /// <summary>
        /// 停止所有补间动画
        /// </summary>
        /// <param name="monoBehaviour">启动协程的MonoBehaviour</param>
        public static void StopAllTweens(MonoBehaviour monoBehaviour)
        {
            if (monoBehaviour != null)
            {
                monoBehaviour.StopAllCoroutines();
            }
        }

        /// <summary>
        /// 延迟执行
        /// </summary>
        /// <param name="delay">延迟时间（秒）</param>
        /// <param name="action">要执行的动作</param>
        /// <returns>协程</returns>
        public static IEnumerator DelayedCall(float delay, System.Action action)
        {
            yield return new WaitForSeconds(delay);
            action?.Invoke();
        }

        #endregion
    }
}