using BeauUtil;
using FieldDay;
using System;
using System.Collections.Generic;

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

        /// <summary>
        /// Appends every confirmed (label, contextMaterialId) pair in
        /// the record to `output`. Static bits resolve to (label,
        /// Null); dynamic bits resolve to (label, materialId-at-bit-
        /// index) via MaterialOrderAsset. Caller-allocates output; does
        /// NOT clear it. Returns the number of pairs appended this
        /// call.
        ///
        /// Order: static bits first (in GetStaticBitIndex order),
        /// then PDopantFor entries (in MaterialOrderAsset order), then
        /// NDopantFor entries. Stable for UIs that display the list
        /// directly. Bits past the registered MaterialOrderAsset count
        /// or with no static-label mapping are silently skipped.
        /// </summary>
        public static int EnumerateConfirmed(in MaterialPropertyRecord record, ICollection<KeyValuePair<MaterialPropertyLabel, StringHash32>> output)
        {
            if (output == null) return 0;
            int count = 0;

            // 1. Static bits. Bits 0..9 map to specific persistent
            // labels; bits 10..15 are reserved and treated as no-ops.
            ushort staticMask = record.StaticMask;
            for (int bit = 0; bit < 16 && staticMask != 0; bit++)
            {
                if ((staticMask & (1 << bit)) == 0) continue;
                staticMask &= unchecked((ushort)~(1 << bit));
                if (MaterialPropertyLabelUtility.TryGetStaticLabelAt(bit, out var label))
                {
                    output.Add(new KeyValuePair<MaterialPropertyLabel, StringHash32>(label, StringHash32.Null));
                    count++;
                }
            }

            // 2. Dynamic bits. Each bit index resolves to a material id
            // via MaterialOrderAsset. Skip if the registry is missing
            // or the bit position has no corresponding registered id.
            MaterialOrderAsset materialOrder = Find.GlobalAsset<MaterialOrderAsset>();
            if (materialOrder != null)
            {
                int orderCount = materialOrder.Count;

                ushort pMask = record.DynamicMask_PDopant;
                for (int bit = 0; bit < 16 && pMask != 0; bit++)
                {
                    if ((pMask & (1 << bit)) == 0) continue;
                    pMask &= unchecked((ushort)~(1 << bit));
                    if (bit >= orderCount) continue;
                    output.Add(new KeyValuePair<MaterialPropertyLabel, StringHash32>(MaterialPropertyLabel.PDopantFor, materialOrder.GetId(bit)));
                    count++;
                }

                ushort nMask = record.DynamicMask_NDopant;
                for (int bit = 0; bit < 16 && nMask != 0; bit++)
                {
                    if ((nMask & (1 << bit)) == 0) continue;
                    nMask &= unchecked((ushort)~(1 << bit));
                    if (bit >= orderCount) continue;
                    output.Add(new KeyValuePair<MaterialPropertyLabel, StringHash32>(MaterialPropertyLabel.NDopantFor, materialOrder.GetId(bit)));
                    count++;
                }
            }

            return count;
        }
    }
}
