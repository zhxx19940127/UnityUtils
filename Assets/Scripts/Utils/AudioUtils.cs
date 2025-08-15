namespace GameObjectToolkit
{
    using UnityEngine;
    using System.Collections;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Unity音频处理工具类
    /// 提供音频播放、录制、分析和处理功能
    /// </summary>
    public static class AudioUtils
    {
        #region 音频播放控制

        /// <summary>
        /// 在指定位置播放音频剪辑（3D音效）
        /// </summary>
        /// <param name="clip">音频剪辑</param>
        /// <param name="position">播放位置</param>
        /// <param name="volume">音量(0-1)</param>
        /// <param name="pitch">音高调节</param>
        /// <param name="spatialBlend">3D混合(0-1)</param>
        /// <param name="parent">父物体(可选)</param>
        /// <returns>创建的AudioSource组件</returns>
        public static AudioSource PlayClipAtPoint(AudioClip clip, Vector3 position, float volume = 1.0f,
            float pitch = 1.0f, float spatialBlend = 1.0f, Transform parent = null)
        {
            if (clip == null)
            {
                Debug.LogError("AudioUtils: 音频剪辑为空");
                return null;
            }

            GameObject tempGO = new GameObject("TempAudio");
            tempGO.transform.position = position;

            if (parent != null)
            {
                tempGO.transform.SetParent(parent);
            }

            AudioSource audioSource = tempGO.AddComponent<AudioSource>();
            audioSource.clip = clip;
            audioSource.volume = Mathf.Clamp01(volume);
            audioSource.pitch = Mathf.Clamp(pitch, 0.1f, 3f);
            audioSource.spatialBlend = Mathf.Clamp01(spatialBlend);
            audioSource.Play();

            // 播放完成后自动销毁
            Object.Destroy(tempGO, clip.length / Mathf.Abs(pitch));

            return audioSource;
        }

        /// <summary>
        /// 淡入播放音频
        /// </summary>
        /// <param name="source">音频源</param>
        /// <param name="duration">淡入时间(秒)</param>
        /// <param name="targetVolume">目标音量</param>
        public static IEnumerator FadeIn(AudioSource source, float duration = 1.0f, float targetVolume = 1.0f)
        {
            if (source == null) yield break;

            float startTime = Time.time;
            source.volume = 0f;
            source.Play();

            while (Time.time < startTime + duration)
            {
                source.volume = Mathf.Lerp(0f, targetVolume, (Time.time - startTime) / duration);
                yield return null;
            }

            source.volume = targetVolume;
        }

        /// <summary>
        /// 淡出停止音频
        /// </summary>
        /// <param name="source">音频源</param>
        /// <param name="duration">淡出时间(秒)</param>
        public static IEnumerator FadeOut(AudioSource source, float duration = 1.0f)
        {
            if (source == null) yield break;

            float startVolume = source.volume;
            float startTime = Time.time;

            while (Time.time < startTime + duration)
            {
                source.volume = Mathf.Lerp(startVolume, 0f, (Time.time - startTime) / duration);
                yield return null;
            }

            source.Stop();
            source.volume = startVolume;
        }

        /// <summary>
        /// 交叉淡入淡出切换音频
        /// </summary>
        /// <param name="source">音频源</param>
        /// <param name="newClip">新音频剪辑</param>
        /// <param name="duration">过渡时间(秒)</param>
        /// <param name="targetVolume">目标音量</param>
        public static IEnumerator CrossFade(AudioSource source, AudioClip newClip, float duration = 1.0f,
            float targetVolume = 1.0f)
        {
            if (source == null) yield break;

            // 如果正在播放同一个剪辑，直接返回
            if (source.clip == newClip && source.isPlaying) yield break;

            float halfDuration = duration * 0.5f;
            yield return FadeOut(source, halfDuration);

            source.clip = newClip;
            yield return FadeIn(source, halfDuration, targetVolume);
        }

        #endregion

        #region 音频录制

        /// <summary>
        /// 开始录制音频（从麦克风）
        /// </summary>
        /// <param name="source">音频源</param>
        /// <param name="maxDuration">最大录制时长(秒)</param>
        /// <param name="deviceName">麦克风设备名(可选)</param>
        /// <returns>录制的音频剪辑</returns>
        public static AudioClip StartRecording(AudioSource source, int maxDuration = 10, string deviceName = null)
        {
            if (Microphone.devices.Length == 0)
            {
                Debug.LogError("AudioUtils: 没有可用的麦克风设备");
                return null;
            }

            deviceName = string.IsNullOrEmpty(deviceName) ? Microphone.devices[0] : deviceName;

            if (!Microphone.devices.Contains(deviceName))
            {
                Debug.LogError($"AudioUtils: 找不到麦克风设备 - {deviceName}");
                return null;
            }

            // 停止所有录制
            if (Microphone.IsRecording(deviceName))
            {
                Microphone.End(deviceName);
            }

            AudioClip clip = Microphone.Start(deviceName, false, maxDuration, AudioSettings.outputSampleRate);

            if (source != null)
            {
                source.clip = clip;
                source.loop = true;

                // 等待录音初始化完成
                while (Microphone.GetPosition(deviceName) <= 0)
                {
                }

                source.Play();
            }

            return clip;
        }

        /// <summary>
        /// 停止录制音频
        /// </summary>
        /// <param name="deviceName">麦克风设备名(可选)</param>
        /// <returns>录制的音频剪辑</returns>
        public static AudioClip StopRecording(string deviceName = null)
        {
            if (Microphone.devices.Length == 0) return null;

            deviceName = string.IsNullOrEmpty(deviceName) ? Microphone.devices[0] : deviceName;

            if (!Microphone.IsRecording(deviceName)) return null;

            // 获取当前录音位置
            int recordingPosition = Microphone.GetPosition(deviceName);
            Microphone.End(deviceName);

            // 创建AudioClip
            return AudioClip.Create("RecordedClip",
                recordingPosition,
                AudioSettings.outputSampleRate,
                Microphone.devices.Length > 1 ? 2 : 1,
                false); // 非流式音频
        }

        /// <summary>
        /// 保存音频剪辑为WAV文件
        /// </summary>
        /// <param name="clip">音频剪辑</param>
        /// <param name="filePath">保存路径</param>
        public static bool SaveAudioClipAsWav(AudioClip clip, string filePath)
        {
            if (clip == null)
            {
                Debug.LogError("AudioUtils: 音频剪辑为空");
                return false;
            }

            try
            {
                byte[] wavData = EncodeAudioClipToWav(clip);
                File.WriteAllBytes(filePath, wavData);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"AudioUtils: 保存WAV文件失败 - {e.Message}");
                return false;
            }
        }

        // 将AudioClip编码为WAV格式字节数组
        private static byte[] EncodeAudioClipToWav(AudioClip clip)
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                // WAV文件头
                writer.Write("RIFF".ToCharArray());
                writer.Write(36 + clip.samples * clip.channels * 2); // 文件大小
                writer.Write("WAVE".ToCharArray());

                // fmt子块
                writer.Write("fmt ".ToCharArray());
                writer.Write(16); // PCM格式块大小
                writer.Write((ushort)1); // PCM格式
                writer.Write((ushort)clip.channels);
                writer.Write(clip.frequency);
                writer.Write(clip.frequency * clip.channels * 2); // 字节率
                writer.Write((ushort)(clip.channels * 2)); // 块对齐
                writer.Write((ushort)16); // 位深度

                // data子块
                writer.Write("data".ToCharArray());
                writer.Write(clip.samples * clip.channels * 2);

                // 音频数据
                float[] samples = new float[clip.samples * clip.channels];
                clip.GetData(samples, 0);

                foreach (float sample in samples)
                {
                    writer.Write((short)(sample * short.MaxValue));
                }

                return stream.ToArray();
            }
        }

        #endregion

        #region 音频分析与处理

        /// <summary>
        /// 获取音频剪辑的RMS（均方根）值，表示音量大小
        /// </summary>
        /// <param name="clip">音频剪辑</param>
        /// <param name="sampleWindow">采样窗口大小</param>
        public static float GetAudioRMS(AudioClip clip, int sampleWindow = 128)
        {
            if (clip == null) return 0f;

            float[] samples = new float[sampleWindow];
            clip.GetData(samples, 0); // 从开头读取

            float sum = 0f;
            foreach (float sample in samples)
            {
                sum += sample * sample;
            }

            return Mathf.Sqrt(sum / sampleWindow);
        }

        /// <summary>
        /// 实时获取音频源的RMS值
        /// </summary>
        /// <param name="source">音频源</param>
        /// <param name="sampleWindow">采样窗口大小</param>
        public static float GetCurrentRMS(AudioSource source, int sampleWindow = 1024)
        {
            if (source == null || !source.isPlaying) return 0f;

            float[] samples = new float[sampleWindow];
            source.GetOutputData(samples, 0);

            float sum = 0f;
            foreach (float sample in samples)
            {
                sum += sample * sample;
            }

            return Mathf.Sqrt(sum / sampleWindow);
        }

        /// <summary>
        /// 标准化音频剪辑（调整音量使其峰值达到最大不失真）
        /// </summary>
        /// <param name="clip">原始音频剪辑</param>
        /// <param name="peak">目标峰值(0-1)</param>
        public static AudioClip NormalizeAudioClip(AudioClip clip, float peak = 0.95f)
        {
            if (clip == null) return null;

            float[] samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);

            // 查找当前峰值
            float maxSample = 0f;
            foreach (float sample in samples)
            {
                float absSample = Mathf.Abs(sample);
                if (absSample > maxSample)
                {
                    maxSample = absSample;
                }
            }

            // 计算增益系数
            float gain = peak / maxSample;

            // 应用增益
            for (int i = 0; i < samples.Length; i++)
            {
                samples[i] = Mathf.Clamp(samples[i] * gain, -1f, 1f);
            }

            // 创建新音频剪辑
            AudioClip normalizedClip = AudioClip.Create(
                clip.name + "_Normalized",
                clip.samples,
                clip.channels,
                clip.frequency,
                false);

            normalizedClip.SetData(samples, 0);
            return normalizedClip;
        }

        /// <summary>
        /// 混合两个音频剪辑
        /// </summary>
        /// <param name="clip1">第一个音频剪辑</param>
        /// <param name="clip2">第二个音频剪辑</param>
        /// <param name="clip2Volume">第二个剪辑的音量</param>
        public static AudioClip MixAudioClips(AudioClip clip1, AudioClip clip2, float clip2Volume = 1.0f)
        {
            if (clip1 == null || clip2 == null) return null;

            // 确定输出剪辑参数
            int maxSamples = Mathf.Max(clip1.samples, clip2.samples);
            int channels = Mathf.Max(clip1.channels, clip2.channels);
            int frequency = Mathf.Max(clip1.frequency, clip2.frequency);

            float[] samples1 = new float[clip1.samples * clip1.channels];
            float[] samples2 = new float[clip2.samples * clip2.channels];

            clip1.GetData(samples1, 0);
            clip2.GetData(samples2, 0);

            // 混合样本
            float[] mixedSamples = new float[maxSamples * channels];
            for (int i = 0; i < mixedSamples.Length; i++)
            {
                float sample1 = i < samples1.Length ? samples1[i] : 0f;
                float sample2 = i < samples2.Length ? samples2[i] * clip2Volume : 0f;

                // 简单的混合算法（可以改为更复杂的算法）
                mixedSamples[i] = Mathf.Clamp(sample1 + sample2, -1f, 1f);
            }

            // 创建混合后的音频剪辑
            AudioClip mixedClip = AudioClip.Create(
                $"{clip1.name}_Mixed_{clip2.name}",
                maxSamples,
                channels,
                frequency,
                false);

            mixedClip.SetData(mixedSamples, 0);
            return mixedClip;
        }

        #endregion

        #region 音频资源管理

        /// <summary>
        /// 从Resources加载音频剪辑
        /// </summary>
        /// <param name="path">资源路径</param>
        public static AudioClip LoadAudioClipFromResources(string path)
        {
            AudioClip clip = Resources.Load<AudioClip>(path);
            if (clip == null)
            {
                Debug.LogError($"AudioUtils: 无法从Resources加载音频剪辑 - {path}");
            }

            return clip;
        }

        /// <summary>
        /// 从文件加载音频剪辑（支持WAV/MP3/OGG）
        /// </summary>
        /// <param name="filePath">文件路径</param>
        public static AudioClip LoadAudioClipFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Debug.LogError($"AudioUtils: 文件不存在 - {filePath}");
                return null;
            }

            string extension = Path.GetExtension(filePath).ToLower();

            switch (extension)
            {
                case ".wav":
                    return LoadWavFile(filePath);
                case ".mp3":
                case ".ogg":
                    return LoadCompressedAudioFile(filePath);
                default:
                    Debug.LogError($"AudioUtils: 不支持的音频格式 - {extension}");
                    return null;
            }
        }

        // 加载WAV文件（简化实现）
        private static AudioClip LoadWavFile(string filePath)
        {
            try
            {
                byte[] fileData = File.ReadAllBytes(filePath);
                return null; // WavUtility.ToAudioClip(fileData);   需要引入 GitHub - WavUtilityForUnity
            }
            catch (System.Exception e)
            {
                Debug.LogError($"AudioUtils: 加载WAV文件失败 - {e.Message}");
                return null;
            }
        }

        // 加载压缩音频文件（需要第三方库如NAudio）
        private static AudioClip LoadCompressedAudioFile(string filePath)
        {
            Debug.LogWarning("AudioUtils: 需要集成第三方库来处理压缩音频文件");
            return null;
        }

        #endregion
    }
}