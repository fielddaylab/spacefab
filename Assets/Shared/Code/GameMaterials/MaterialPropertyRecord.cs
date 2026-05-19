using BeauUtil;
using FieldDay;
using System;

namespace SpaceFab.Materials
{
    /// <summary>
    /// Per-material confirmed-property bitmasks. Save-state representation.
    /// StaticMask: bit i = the static persistent MaterialPropertyLabel whose
    /// MaterialPropertyLabelUtility.GetStaticBitIndex(...) is i (10 of 16 bits
    /// used today). Bit positions are wire-format - never reorder.
    /// DynamicMask_PDopant / DynamicMaskNDopant: bit i = the corresponding
    /// dynamic MaterialPropertyLabel confirmed against the material at
    /// MaterialOrderAsset index i.
    /// </summary>
    [Serializable]
    public struct MaterialPropertyRecord
    {
        public ushort StaticMask;
        public ushort DynamicMask_PDopant;
        public ushort DynamicMask_NDopant;
    }

    /// <summary>
    /// Shared bit math for MaterialPropertyRecord. Pure functions over the
    /// record: no awareness of where it lives (player progress, in-session
    /// sandbox, save buffer, ...). Callers handle the dictionary plumbing
    /// around these, then delegate the actual mask reads/writes here so the
    /// bit layout is defined in exactly one place.
    /// </summary>
    public static class MaterialPropertyRecordUtility
    {
        /// <summary>
        /// True iff every mask in the record is zero. Useful for skipping
        /// empty entries during save serialization.
        /// </summary>
        public static bool IsEmpty(in MaterialPropertyRecord record)
        {
            return record.StaticMask == 0
                && record.DynamicMask_PDopant == 0
                && record.DynamicMask_NDopant == 0;
        }

        /// <summary>
        /// Returns true if the record has the given persistent property
        /// confirmed. For dynamic labels, contextMaterialId names the X in
        /// "P-Type Dopant for X" and is resolved through MaterialOrderAsset.
        /// Observation-only labels always return false.
        /// </summary>
        public static bool Has(in MaterialPropertyRecord record, MaterialPropertyLabel label, StringHash32 contextMaterialId)
        {
            if (!MaterialPropertyLabelUtility.IsPersistent(label))
            {
                return false;
            }

            if (MaterialPropertyLabelUtility.IsDynamic(label))
            {
                MaterialOrderAsset materialOrder = Find.GlobalAsset<MaterialOrderAsset>();
                if (!materialOrder.TryGetIndex(contextMaterialId, out int idx))
                {
                    return false;
                }
                ushort mask = label == MaterialPropertyLabel.PDopantFor ? record.DynamicMask_PDopant : record.DynamicMask_NDopant;
                return (mask & (1 << idx)) != 0;
            }
            else
            {
                int bitIndex = MaterialPropertyLabelUtility.GetStaticBitIndex(label);
                if (bitIndex < 0) return false;
                return (record.StaticMask & (1 << bitIndex)) != 0;
            }
        }

        /// <summary>
        /// Sets the bit for the given persistent property on the record.
        /// Returns true iff the record actually changed (lets callers track
        /// dirty deltas without an outer comparison). Idempotent: re-setting
        /// an already-set bit returns false. Observation-only labels are
        /// silently ignored.
        /// </summary>
        public static bool TrySet(ref MaterialPropertyRecord record, MaterialPropertyLabel label, StringHash32 contextMaterialId)
        {
            if (!MaterialPropertyLabelUtility.IsPersistent(label))
            {
                return false;
            }

            if (MaterialPropertyLabelUtility.IsDynamic(label))
            {
                MaterialOrderAsset materialOrder = Find.GlobalAsset<MaterialOrderAsset>();
                if (!materialOrder.TryGetIndex(contextMaterialId, out int idx))
                {
                    return false;
                }
                ushort bit = (ushort)(1 << idx);
                if (label == MaterialPropertyLabel.PDopantFor)
                {
                    if ((record.DynamicMask_PDopant & bit) != 0) return false;
                    record.DynamicMask_PDopant |= bit;
                }
                else
                {
                    if ((record.DynamicMask_NDopant & bit) != 0) return false;
                    record.DynamicMask_NDopant |= bit;
                }
                return true;
            }
            else
            {
                int bitIndex = MaterialPropertyLabelUtility.GetStaticBitIndex(label);
                if (bitIndex < 0) return false;
                ushort bit = (ushort)(1 << bitIndex);
                if ((record.StaticMask & bit) != 0) return false;
                record.StaticMask |= bit;
                return true;
            }
        }

        /// <summary>
        /// OR-merges other's masks into target. Idempotent; additive only -
        /// bits set in target are never cleared.
        /// </summary>
        public static void Merge(ref MaterialPropertyRecord target, in MaterialPropertyRecord other)
        {
            target.StaticMask |= other.StaticMask;
            target.DynamicMask_PDopant |= other.DynamicMask_PDopant;
            target.DynamicMask_NDopant |= other.DynamicMask_NDopant;
        }
    }
}
