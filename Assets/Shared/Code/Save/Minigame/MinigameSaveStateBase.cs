using FieldDay.Data;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Save
{
    /// <summary>
    /// Shared base for minigame save states. Owns the FoundValidSolution flag,
    /// implements HasValidSolution(), and provides virtual SetDefaults / Read / Write
    /// that handle the shared flag. Concrete save states override Read / Write to
    /// lay out their own bytes and call base.Read / base.Write at the position
    /// where FoundValidSolution should appear (currently the tail of the chunk).
    /// </summary>
    public abstract class MinigameSaveStateBase : IMinigameSaveState, ISaveStateChunkObject
    {
        public bool FoundValidSolution;

        public virtual void SetDefaults()
        {
            FoundValidSolution = false;
        }

        public bool HasValidSolution()
        {
            return FoundValidSolution;
        }

        public virtual void Read(object self, ref ByteReader reader, SaveStateChunkConsts consts)
        {
            FoundValidSolution = reader.Read<bool>();
        }

        public virtual void Write(object self, ref ByteWriter writer, SaveStateChunkConsts consts)
        {
            writer.Write(FoundValidSolution);
        }
    }
}
