using BeauUtil;
using BeauUtil.Debugger;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace FieldDay.Rendering {
    static internal class BayerMatrices {
        private const int Size2 = 4;
        private const int Size4 = 16;
        private const int Size8 = 64;

        private const int TotalSize = Size2 + Size4 + Size8;

        static private GraphicsBuffer BayerMatrixBuffer;

        [EditorStaticResource]
        static public unsafe void CreateBuffer() {
            if (BayerMatrixBuffer != null) {
                return;
            }

            Log.Msg("[BayerMatrices] Initializing bayer matrix buffer");

            BayerMatrixBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Constant, TotalSize, 4);
            EditorStaticResource.SetupLifetime(CreateBuffer, DestroyBuffer);

            float* data = stackalloc float[TotalSize];
            float* head = data;

            // matrix8
            WriteRow(ref head, 8, 0, 32, 8, 40);    WriteRow(ref head, 8, 2, 34, 10, 42);
            WriteRow(ref head, 8, 48, 16, 56, 24);  WriteRow(ref head, 8, 50, 18, 58, 26);
            WriteRow(ref head, 8, 12, 44, 4, 36);   WriteRow(ref head, 8, 14, 46, 6, 38);
            WriteRow(ref head, 8, 60, 28, 52, 20);  WriteRow(ref head, 8, 62, 30, 54, 22);
            WriteRow(ref head, 8, 3, 35, 11, 43);   WriteRow(ref head, 8, 1, 33, 9, 41);
            WriteRow(ref head, 8, 51, 19, 59, 27);  WriteRow(ref head, 8, 49, 17, 57, 25);
            WriteRow(ref head, 8, 15, 47, 7, 39);   WriteRow(ref head, 8, 13, 45, 5, 37);
            WriteRow(ref head, 8, 63, 31, 55, 23);  WriteRow(ref head, 8, 61, 29, 53, 21);

            // matrix4
            WriteRow(ref head, 4, 0, 8, 2, 10);
            WriteRow(ref head, 4, 12, 4, 14, 6);
            WriteRow(ref head, 4, 3, 11, 1, 9);
            WriteRow(ref head, 4, 15, 7, 13, 5);

            // matrix2
            WriteRow(ref head, 2, 0, 2);
            WriteRow(ref head, 2, 3, 1);

            BayerMatrixBuffer.SetData(Unsafe.NativeArray<float>(data, TotalSize));
            Shader.SetGlobalConstantBuffer("FDBayerMatrices", BayerMatrixBuffer, 0, TotalSize * 4);
        }

        static public void DestroyBuffer() {
            if (BayerMatrixBuffer != null) {
                Log.Msg("[BayerMatrices] Destroying matrix buffer");
                BayerMatrixBuffer.Dispose();
                BayerMatrixBuffer = null;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static private unsafe void WriteRow(ref float* head, int size, int value0, int value1) {
            *head++ = CalcValue(value0, size);
            *head++ = CalcValue(value1, size);
        }

        static private unsafe void WriteRow(ref float* head, int size, int value0, int value1, int value2, int value3) {
            WriteRow(ref head, size, value0, value1);
            WriteRow(ref head, size, value2, value3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static private float CalcValue(int index, int size) {
            return index / (float)(size * size);
        }
    }
}