namespace GameObjectToolkit
{
    using UnityEngine;
    using System;
    using System.Collections;
    using System.Collections.Generic;

    /// <summary>
    /// 协程控制工具类
    /// 提供全面的协程创建、管理和控制功能
    /// </summary>
    public static class CoroutineUtils
    {
        #region 基础协程控制

        private static MonoBehaviour _coroutineRunner;

        private static MonoBehaviour CoroutineRunner
        {
            get
            {
                if (_coroutineRunner == null)
                {
                    _coroutineRunner = new GameObject("CoroutineRunner").AddComponent<CoroutineRunnerClass>();
                    UnityEngine.Object.DontDestroyOnLoad(_coroutineRunner.gameObject);
                }

                return _coroutineRunner;
            }
        }

        /// <summary>
        /// 启动协程（自动创建运行器如果不存在）
        /// </summary>
        /// <param name="routine">协程方法</param>
        /// <returns>协程对象</returns>
        public static Coroutine StartCoroutine(IEnumerator routine)
        {
            return CoroutineRunner.StartCoroutine(routine);
        }

        /// <summary>
        /// 停止协程
        /// </summary>
        /// <param name="coroutine">要停止的协程</param>
        public static void StopCoroutine(Coroutine coroutine)
        {
            if (coroutine != null)
            {
                CoroutineRunner.StopCoroutine(coroutine);
            }
        }

        /// <summary>
        /// 停止所有由该工具类启动的协程
        /// </summary>
        public static void StopAllCoroutines()
        {
            CoroutineRunner.StopAllCoroutines();
        }

        #endregion

        #region 延迟执行

        /// <summary>
        /// 延迟指定时间后执行动作
        /// </summary>
        /// <param name="action">要执行的动作</param>
        /// <param name="delaySeconds">延迟时间（秒）</param>
        /// <returns>协程对象</returns>
        public static Coroutine DelayedAction(Action action, float delaySeconds)
        {
            return StartCoroutine(DelayedActionRoutine(action, delaySeconds));
        }

        private static IEnumerator DelayedActionRoutine(Action action, float delaySeconds)
        {
            yield return new WaitForSeconds(delaySeconds);
            action?.Invoke();
        }

        /// <summary>
        /// 延迟一帧后执行动作
        /// </summary>
        public static Coroutine DelayedFrameAction(Action action, int frameCount = 1)
        {
            return StartCoroutine(DelayedFrameActionRoutine(action, frameCount));
        }

        private static IEnumerator DelayedFrameActionRoutine(Action action, int frameCount)
        {
            for (int i = 0; i < frameCount; i++)
            {
                yield return null;
            }

            action?.Invoke();
        }

        #endregion

        #region 条件执行

        /// <summary>
        /// 等待条件满足后执行动作
        /// </summary>
        /// <param name="action">要执行的动作</param>
        /// <param name="condition">等待的条件</param>
        /// <param name="timeout">超时时间（秒），0表示无超时</param>
        /// <returns>协程对象</returns>
        public static Coroutine ExecuteWhen(Action action, Func<bool> condition, float timeout = 0f)
        {
            return StartCoroutine(ExecuteWhenRoutine(action, condition, timeout));
        }

        private static IEnumerator ExecuteWhenRoutine(Action action, Func<bool> condition, float timeout)
        {
            float startTime = Time.time;
            while (!condition())
            {
                if (timeout > 0 && Time.time - startTime >= timeout)
                {
                    yield break;
                }

                yield return null;
            }

            action?.Invoke();
        }

        /// <summary>
        /// 等待条件满足后执行协程
        /// </summary>
        public static Coroutine ExecuteCoroutineWhen(IEnumerator routine, Func<bool> condition, float timeout = 0f)
        {
            return StartCoroutine(ExecuteCoroutineWhenRoutine(routine, condition, timeout));
        }

        private static IEnumerator ExecuteCoroutineWhenRoutine(IEnumerator routine, Func<bool> condition, float timeout)
        {
            float startTime = Time.time;
            while (!condition())
            {
                if (timeout > 0 && Time.time - startTime >= timeout)
                {
                    yield break;
                }

                yield return null;
            }

            yield return routine;
        }

        #endregion

        #region 协程链式调用

        /// <summary>
        /// 顺序执行多个协程
        /// </summary>
        /// <param name="routines">要顺序执行的协程列表</param>
        /// <returns>协程对象</returns>
        public static Coroutine Sequence(params IEnumerator[] routines)
        {
            return StartCoroutine(SequenceRoutine(routines));
        }

        private static IEnumerator SequenceRoutine(IEnumerator[] routines)
        {
            foreach (var routine in routines)
            {
                yield return routine;
            }
        }

        /// <summary>
        /// 并行执行多个协程（等待所有完成）
        /// </summary>
        public static Coroutine Parallel(params IEnumerator[] routines)
        {
            return StartCoroutine(ParallelRoutine(routines));
        }

        private static IEnumerator ParallelRoutine(IEnumerator[] routines)
        {
            List<Coroutine> runningCoroutines = new List<Coroutine>();
            foreach (var routine in routines)
            {
                runningCoroutines.Add(StartCoroutine(routine));
            }

            foreach (var coroutine in runningCoroutines)
            {
                yield return coroutine;
            }
        }

        #endregion

        #region 高级控制

        /// <summary>
        /// 带超时控制的协程
        /// </summary>
        /// <param name="routine">要执行的协程</param>
        /// <param name="timeoutSeconds">超时时间（秒）</param>
        /// <param name="onTimeout">超时回调</param>
        /// <returns>协程对象</returns>
        public static Coroutine WithTimeout(IEnumerator routine, float timeoutSeconds, Action onTimeout = null)
        {
            return StartCoroutine(WithTimeoutRoutine(routine, timeoutSeconds, onTimeout));
        }

        private static IEnumerator WithTimeoutRoutine(IEnumerator routine, float timeoutSeconds, Action onTimeout)
        {
            float startTime = Time.time;
            Coroutine nestedCoroutine = StartCoroutine(routine);

            while (nestedCoroutine != null)
            {
                if (Time.time - startTime >= timeoutSeconds)
                {
                    StopCoroutine(nestedCoroutine);
                    onTimeout?.Invoke();
                    yield break;
                }

                yield return null;
            }
        }

        /// <summary>
        /// 可暂停的协程
        /// </summary>
        /// <param name="routine">要执行的协程</param>
        /// <param name="isPaused">暂停状态引用</param>
        /// <returns>协程对象</returns>
        public static Coroutine PausableCoroutine(IEnumerator routine, Func<bool> isPaused)
        {
            return StartCoroutine(PausableCoroutineRoutine(routine, isPaused));
        }

        private static IEnumerator PausableCoroutineRoutine(IEnumerator routine, Func<bool> isPaused)
        {
            while (true)
            {
                if (!isPaused())
                {
                    if (!routine.MoveNext())
                    {
                        yield break;
                    }

                    yield return routine.Current;
                }
                else
                {
                    yield return null;
                }
            }
        }

        #endregion

        #region 实用协程

        /// <summary>
        /// 在指定时间内渐变值
        /// </summary>
        /// <param name="from">起始值</param>
        /// <param name="to">目标值</param>
        /// <param name="duration">持续时间（秒）</param>
        /// <param name="onUpdate">值更新回调</param>
        /// <param name="easing">缓动函数</param>
        /// <returns>协程对象</returns>
        public static Coroutine LerpValue(float from, float to, float duration, Action<float> onUpdate,
            Func<float, float> easing = null)
        {
            return StartCoroutine(LerpValueRoutine(from, to, duration, onUpdate, easing));
        }

        private static IEnumerator LerpValueRoutine(float from, float to, float duration,
            Action<float> onUpdate, Func<float, float> easing)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                t = easing != null ? easing(t) : t;
                onUpdate?.Invoke(Mathf.Lerp(from, to, t));
                elapsed += Time.deltaTime;
                yield return null;
            }

            onUpdate?.Invoke(to);
        }

        /// <summary>
        /// 周期性执行动作
        /// </summary>
        /// <param name="action">要执行的动作</param>
        /// <param name="intervalSeconds">执行间隔（秒）</param>
        /// <param name="count">执行次数，0表示无限</param>
        /// <returns>协程对象</returns>
        public static Coroutine RepeatAction(Action action, float intervalSeconds, int count = 0)
        {
            return StartCoroutine(RepeatActionRoutine(action, intervalSeconds, count));
        }

        private static IEnumerator RepeatActionRoutine(Action action, float intervalSeconds, int count)
        {
            if (count == 0)
            {
                while (true)
                {
                    action?.Invoke();
                    yield return new WaitForSeconds(intervalSeconds);
                }
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    action?.Invoke();
                    yield return new WaitForSeconds(intervalSeconds);
                }
            }
        }

        #endregion

        #region 内部类

        private class CoroutineRunnerClass : MonoBehaviour
        {
        }

        #endregion
    }
}