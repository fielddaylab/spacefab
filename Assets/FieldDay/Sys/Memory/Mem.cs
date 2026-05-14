using BeauPools;
using BeauUtil;
using System.Text;
using UnityEngine;

namespace FieldDay.Memory {

    static public class Mem {
        static internal MemoryMgr Mgr;

        static public event GCNotifyDelegate OnGarbageCollectionOccurred;
        static public event HeapSizeChangedDelegate OnHeapSizeChanged;

        static internal void InvokeGCOccurred(int mask) {
            OnGarbageCollectionOccurred?.Invoke(mask);
        }

        static internal void InvokeHeapSizeChanged(long newHeapSize) {
            OnHeapSizeChanged?.Invoke(newHeapSize);
        }
    }

    public delegate void GCNotifyDelegate(int generationMask);
    public delegate void HeapSizeChangedDelegate(long newHeapSize);
}