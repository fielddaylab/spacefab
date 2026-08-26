using BeauUtil;
using BeauUtil.Variants;
using FieldDay.Scripting;

namespace SpaceFab.Materials
{
    /// <summary>
    /// In-session observation buffer for one material. Each entry is a
    /// (label, contextMaterialId) pair: the observation the player made,
    /// and the X material the observation was made against (StringHash32.Null
    /// for non-context observations like Conductive or HeatActivated).
    ///
    /// Fixed capacity (MaxObservations); never serialized. Lives in
    /// ResearchMinigameState.Observations during the minigame and is cleared
    /// at minigame entry. Distinct from MaterialPropertyRecord, which holds
    /// confirmed properties that round-trip to PlayerProgressState.
    ///
    /// Storage is two parallel inline fixed buffers: the label byte at slot i
    /// pairs with the StringHash32 (uint) at slot i. Total footprint is
    /// 10 + 40 + 1 = 51 bytes, no heap allocation per material.
    /// </summary>
    public unsafe struct MaterialObservationList
    {
        public const int MaxObservations = 10;

        public fixed byte LabelBuffer[MaxObservations];
        public fixed uint ContextBuffer[MaxObservations];
        public byte Count;
    }

    /// <summary>
    /// Operations on MaterialObservationList. Equality is on the (label, context)
    /// pair: two entries match iff both fields match. Adds reject duplicates so
    /// the same observation can't be hoarded.
    /// </summary>
    public static unsafe class MaterialObservationListUtility
    {
        // Adds an observation. Returns false if the buffer is full or if an
        // entry with the same (label, context) is already present.
        public static bool TryAdd(ref MaterialObservationList list, MaterialPropertyLabel label, StringHash32 contextMaterialId)
        {
            if (list.Count >= MaterialObservationList.MaxObservations)
            {
                return false;
            }
            if (Has(list, label, contextMaterialId))
            {
                return false;
            }

            int slot = list.Count;
            list.LabelBuffer[slot] = (byte)label;
            list.ContextBuffer[slot] = contextMaterialId.HashValue;
            list.Count++;

            ScriptUtility.WriteVariable(new TableKeyPair("research", "observationCount"), (int) list.Count);

            return true;
        }

        // Removes the first entry matching (label, context). Compacts the buffer
        // by swapping the last entry into the removed slot. Returns true if an
        // entry was removed.
        public static bool Remove(ref MaterialObservationList list, MaterialPropertyLabel label, StringHash32 contextMaterialId)
        {
            byte labelByte = (byte)label;
            uint contextHash = contextMaterialId.HashValue;
            int count = list.Count;

            for (int i = 0; i < count; i++)
            {
                if (list.LabelBuffer[i] == labelByte && list.ContextBuffer[i] == contextHash)
                {
                    int last = count - 1;
                    if (i != last)
                    {
                        list.LabelBuffer[i] = list.LabelBuffer[last];
                        list.ContextBuffer[i] = list.ContextBuffer[last];
                    }
                    list.LabelBuffer[last] = 0;
                    list.ContextBuffer[last] = 0;
                    list.Count--;

                    ScriptUtility.WriteVariable(new TableKeyPair("research", "observationCount"), (int) list.Count);

                    return true;
                }
            }
            return false;
        }

        // True if an entry with the given (label, context) is present.
        public static bool Has(in MaterialObservationList list, MaterialPropertyLabel label, StringHash32 contextMaterialId)
        {
            byte labelByte = (byte)label;
            uint contextHash = contextMaterialId.HashValue;
            int count = list.Count;

            // `in` parameter prevents direct fixed-buffer indexing, so take a
            // pointer to the readonly reference.
            fixed (MaterialObservationList* ptr = &list)
            {
                for (int i = 0; i < count; i++)
                {
                    if (ptr->LabelBuffer[i] == labelByte && ptr->ContextBuffer[i] == contextHash)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        // Reads slot i. Returns default if i is out of range.
        public static MaterialPropertyLabel GetLabel(in MaterialObservationList list, int index)
        {
            if (index < 0 || index >= list.Count)
            {
                return default;
            }
            fixed (MaterialObservationList* ptr = &list)
            {
                return (MaterialPropertyLabel)ptr->LabelBuffer[index];
            }
        }

        public static StringHash32 GetContext(in MaterialObservationList list, int index)
        {
            if (index < 0 || index >= list.Count)
            {
                return StringHash32.Null;
            }
            fixed (MaterialObservationList* ptr = &list)
            {
                return new StringHash32(ptr->ContextBuffer[index]);
            }
        }

        // Empties the list.
        public static void Clear(ref MaterialObservationList list)
        {
            int count = list.Count;
            for (int i = 0; i < count; i++)
            {
                list.LabelBuffer[i] = 0;
                list.ContextBuffer[i] = 0;
            }
            list.Count = 0;
        }
    }
}
