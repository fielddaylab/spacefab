using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace FieldDay.Perf {
    [CreateAssetMenu(menuName = "Field Day/Performance/Performance Budget")]
    public sealed class PerformanceBudget : ScriptableObject {
        [Header("Frame Time")]
        [Tooltip("Alerts if frame time spikes above the desired frame time multiplied by this factor")]
        [Range(1, 10)] public float MaxFrameTimeSpikeRatio = 4;

        [Tooltip("Alerts if average frame time hovers above the desired frame time multiplied by this factor")]
        [Range(1, 10)] public float MaxAverageFrameTimeRatio = 1.5f;

        [Header("Memory")]
        [Tooltip("Alerts if CPU memory usage or heap size goes above this value in MiB")]
        [Range(32, 512)] public int MaxCPUMemoryMB = 160;

        [Tooltip("Alerts if GPU memory usage goes above this value in MiB")]
        [Range(32, 512)] public int MaxGPUMemoryMB = 80;

        [Tooltip("Alerts if texture memory usage goes above this value in MiB")]
        [Range(32, 512)] public int MaxTextureMemoryMB = 128;
    }
}