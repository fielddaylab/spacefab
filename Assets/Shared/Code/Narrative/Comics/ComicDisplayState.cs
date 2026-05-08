using FieldDay.Collections;
using FieldDay.SharedState;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace SpaceFab.Comic
{
    public class ComicDisplayState : SharedStateComponent
    {
        public Pipe<ComicDisplayCommand> Commands = new Pipe<ComicDisplayCommand>(64, false);
    }

    public enum ComicDisplayOpCode : byte {
        SnapCamera,
        MoveCamera,
        UnloadPage,
        LoadPage,
        UnloadPrevPage,
        LoadNextPage,
        SetNextPage,
        SetNextPanel,
        SetCurrentPage,
        SetCurrentPanel,
        ShowLayer,
        ShowMask,
        HideLayer,
        SyncAnimations
    }

    public enum ComicDisplayFenceType : byte {
        None,
        MeshLoading,
        ActiveAnimations,
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct ComicDisplayCommand {
        [FieldOffset(0)] public ComicDisplayOpCode OpCode;
        [FieldOffset(1)] public ComicDisplayFenceType Fence;
        [FieldOffset(2)] public ushort ResourceIndex;
    }
}