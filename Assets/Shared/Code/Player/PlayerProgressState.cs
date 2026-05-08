using BeauUtil;
using FieldDay;
using FieldDay.Assets;
using FieldDay.Data;
using FieldDay.SharedState;
using SpaceFab.Materials;
using SpaceFab.Save;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab
{
    public class PlayerProgressState : SharedStateComponent, ISaveStateChunkObject, IRegistrationCallbacks
    {
        #region Save State

        public bool RecentlyCompletedChapter;

        public int ElapsedCycles;
        public int Funds;

        public uint CompletedContractBuffer;
        public MaterialPropertyRecord[] MaterialPropertyBuffer;

        #endregion // Save State

        public Dictionary<StringHash32, MaterialPropertyRecord> MaterialProperties;
        public HashSet<StringHash32> CompletedContractIds;

        public StringHash32 ContractAssetsWrapperId; // TODO: wrap contract def back into contract assets wrapper asset (decouple from AvailableContractsBundle)
        public StringHash32 CurrContractId;

        public void OnDeregister()
        {
        }

        public void OnRegister()
        {
            CompletedContractIds = new HashSet<StringHash32>();
            MaterialProperties = new Dictionary<StringHash32, MaterialPropertyRecord>();
            SpacefabGame.SaveBuffer.RegisterHandler("PlayerProgressState", this);
        }

        #region Interfaces

        // ISaveStateChunkObject

        public void Read(object self, ref ByteReader reader, SaveStateChunkConsts consts)
        {
            RecentlyCompletedChapter = reader.Read<bool>();
            ElapsedCycles = reader.Read<int>();
            Funds = reader.Read<int>();
            PlayerProgressUtility.UnpackCompletedContracts(this, reader.Read<uint>());
            PlayerProgressUtility.UnpackMaterialProperties(this, ref reader);
        }

        public void Write(object self, ref ByteWriter writer, SaveStateChunkConsts consts)
        {
            writer.Write(RecentlyCompletedChapter);
            writer.Write(ElapsedCycles);
            writer.Write(Funds);
            writer.Write(PlayerProgressUtility.PackCompletedContracts(this));
            PlayerProgressUtility.PackMaterialProperties(this, ref writer);
        }

        #endregion // Interfaces
    }

    public static class PlayerProgressUtility
    {
        public static bool HasCompletedContract(PlayerProgressState progressState, StringHash32 contractId)
        {
            if (progressState.CompletedContractIds.Contains(contractId))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Packs CompletedContractIds into a bitmask using ContractOrderAsset for stable indices.
        /// </summary>
        public static uint PackCompletedContracts(PlayerProgressState state)
        {
            var contractOrder = Find.GlobalAsset<ContractOrderAsset>();
            state.CompletedContractBuffer = 0;
            foreach (var id in state.CompletedContractIds)
            {
                if (contractOrder.TryGetIndex(id, out int idx))
                {
                    state.CompletedContractBuffer |= (1u << idx);
                }
            }
            return state.CompletedContractBuffer;
        }

        /// <summary>
        /// Unpacks a saved bitmask back into CompletedContractIds using ContractOrderAsset.
        /// </summary>
        public static void UnpackCompletedContracts(PlayerProgressState state, uint mask)
        {
            var contractOrder = Find.GlobalAsset<ContractOrderAsset>();
            state.CompletedContractIds.Clear();
            for (int i = 0; i < contractOrder.Count; i++)
            {
                if ((mask & (1u << i)) != 0)
                {
                    state.CompletedContractIds.Add(contractOrder.GetId(i));
                }
            }
        }

        /// <summary>
        /// Returns true if the given static property has been confirmed for the material.
        /// </summary>
        public static bool HasConfirmedStatic(PlayerProgressState state, StringHash32 materialId, StaticProperty property)
        {
            if (state.MaterialProperties.TryGetValue(materialId, out var record))
            {
                return (record.StaticMask & (1 << (int)property)) != 0;
            }
            return false;
        }

        /// <summary>
        /// Marks the given static property as confirmed for the material.
        /// </summary>
        public static void ConfirmStatic(PlayerProgressState state, StringHash32 materialId, StaticProperty property)
        {
            state.MaterialProperties.TryGetValue(materialId, out var record);
            record.StaticMask |= (ushort)(1 << (int)property);
            state.MaterialProperties[materialId] = record;
        }

        /// <summary>
        /// Returns true if the given dynamic property has been confirmed for materialId against otherMaterialId.
        /// </summary>
        public static bool HasConfirmedDynamic(PlayerProgressState state, StringHash32 materialId, DynamicProperty property, StringHash32 otherMaterialId)
        {
            if (!state.MaterialProperties.TryGetValue(materialId, out var record))
            {
                return false;
            }
            var materialOrder = Find.GlobalAsset<MaterialOrderAsset>();
            if (!materialOrder.TryGetIndex(otherMaterialId, out int idx))
            {
                return false;
            }
            ushort mask = property == DynamicProperty.PDopantForX ? record.DynamicMask_PDopant : record.DynamicMaskNDopant;
            return (mask & (1 << idx)) != 0;
        }

        /// <summary>
        /// Marks the given dynamic property as confirmed for materialId against otherMaterialId.
        /// </summary>
        public static void ConfirmDynamic(PlayerProgressState state, StringHash32 materialId, DynamicProperty property, StringHash32 otherMaterialId)
        {
            var materialOrder = Find.GlobalAsset<MaterialOrderAsset>();
            if (!materialOrder.TryGetIndex(otherMaterialId, out int idx))
            {
                return;
            }
            state.MaterialProperties.TryGetValue(materialId, out var record);
            ushort bit = (ushort)(1 << idx);
            if (property == DynamicProperty.PDopantForX)
            {
                record.DynamicMask_PDopant |= bit;
            }
            else
            {
                record.DynamicMaskNDopant |= bit;
            }
            state.MaterialProperties[materialId] = record;
        }

        /// <summary>
        /// Stages MaterialProperties into MaterialPropertyBuffer (in MaterialOrderAsset order)
        /// and writes the buffer to the save stream.
        /// </summary>
        public static void PackMaterialProperties(PlayerProgressState state, ref ByteWriter writer)
        {
            var materialOrder = Find.GlobalAsset<MaterialOrderAsset>();
            int count = materialOrder.Count;
            if (state.MaterialPropertyBuffer == null || state.MaterialPropertyBuffer.Length != count)
            {
                state.MaterialPropertyBuffer = new MaterialPropertyRecord[count];
            }
            for (int i = 0; i < count; i++)
            {
                state.MaterialProperties.TryGetValue(materialOrder.GetId(i), out var record);
                state.MaterialPropertyBuffer[i] = record;
            }
            writer.WriteBuffer(state.MaterialPropertyBuffer);
        }

        /// <summary>
        /// Reads MaterialPropertyBuffer from the save stream and rebuilds MaterialProperties,
        /// skipping all-zero entries to keep the dictionary sparse.
        /// </summary>
        public static void UnpackMaterialProperties(PlayerProgressState state, ref ByteReader reader)
        {
            var materialOrder = Find.GlobalAsset<MaterialOrderAsset>();
            int count = materialOrder.Count;
            if (state.MaterialPropertyBuffer == null || state.MaterialPropertyBuffer.Length != count)
            {
                state.MaterialPropertyBuffer = new MaterialPropertyRecord[count];
            }
            reader.ReadBuffer(state.MaterialPropertyBuffer);
            state.MaterialProperties.Clear();
            for (int i = 0; i < count; i++)
            {
                var record = state.MaterialPropertyBuffer[i];
                if (record.StaticMask != 0 || record.DynamicMask_PDopant != 0 || record.DynamicMaskNDopant != 0)
                {
                    state.MaterialProperties[materialOrder.GetId(i)] = record;
                }
            }
        }
    }
}