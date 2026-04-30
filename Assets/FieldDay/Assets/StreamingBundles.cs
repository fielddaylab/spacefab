using UnityEngine;

namespace FieldDay.Assets {
    /// <summary>
    /// Interface for a streaming bundle root.
    /// </summary>
    public interface IStreamingBundleRoot {
#if UNITY_EDITOR
        public struct ExportData {
            public string BundleName;
            public string Category;
        }

        bool GetExportParameters(out ExportData buildParams);
#endif // UNITY_EDITOR
    }
}