using BeauUtil;
using FieldDay;
using FieldDay.Assets;
using FieldDay.Data;
using FieldDay.SharedState;
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
        public uint ResearchedMaterialBuffer;

        #endregion // Save State

        public HashSet<StringHash32> ResearchedMaterials;
        public HashSet<StringHash32> CompletedContractIds;

        public StringHash32 ContractAssetsWrapperId; // TODO: wrap contract def back into contract assets wrapper asset (decouple from AvailableContractsBundle)
        public StringHash32 CurrContractId;

        public void OnDeregister()
        {
        }

        public void OnRegister()
        {
            CompletedContractIds = new HashSet<StringHash32>();
            ResearchedMaterials = new HashSet<StringHash32>();
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
            PlayerProgressUtility.UnpackResearchedMaterials(this, reader.Read<uint>());
        }

        public void Write(object self, ref ByteWriter writer, SaveStateChunkConsts consts)
        {
            writer.Write(RecentlyCompletedChapter);
            writer.Write(ElapsedCycles);
            writer.Write(Funds);
            writer.Write(PlayerProgressUtility.PackCompletedContracts(this));
            writer.Write(PlayerProgressUtility.PackResearchedMaterials(this));
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
        /// Packs ResearchedMaterials into a bitmask using MaterialOrderAsset for stable indices.
        /// </summary>
        public static uint PackResearchedMaterials(PlayerProgressState state)
        {
            var materialOrder = Find.GlobalAsset<MaterialOrderAsset>();
            state.ResearchedMaterialBuffer = 0;
            foreach (var id in state.ResearchedMaterials)
            {
                if (materialOrder.TryGetIndex(id, out int idx))
                {
                    state.ResearchedMaterialBuffer |= (1u << idx);
                }
            }
            return state.ResearchedMaterialBuffer;
        }

        /// <summary>
        /// Unpacks a saved bitmask back into ResearchedMaterials using MaterialOrderAsset.
        /// </summary>
        public static void UnpackResearchedMaterials(PlayerProgressState state, uint mask)
        {
            var materialOrder = Find.GlobalAsset<MaterialOrderAsset>();
            state.ResearchedMaterials.Clear();
            for (int i = 0; i < materialOrder.Count; i++)
            {
                if ((mask & (1u << i)) != 0)
                {
                    state.ResearchedMaterials.Add(materialOrder.GetId(i));
                }
            }
        }
    }
}