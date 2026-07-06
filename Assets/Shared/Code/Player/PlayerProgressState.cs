using BeauUtil;
using FieldDay;
using FieldDay.Assets;
using FieldDay.Data;
using FieldDay.SharedState;
using SpaceFab.Materials;
using SpaceFab.Save;
using SpaceFab.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab
{
    public class PlayerProgressState : SharedStateComponent, ISaveStateChunkObject, IRegistrationCallbacks
    {
        #region Save State

        public bool RecentlyCompletedChapter;

        public bool BigBatteryUnlocked;

        // Tracks whether the one-shot wiki initial-unlocks pass has
        // already run for this save. OverarchingStartupSequenceSystem
        // applies WikiInitialUnlocksConfig.InitialUnlockedPages once
        // when this is false, then sets it true.
        public bool InitialUnlocksApplied;

        public int ElapsedCycles;
        public int Funds;

        public uint CompletedContractBuffer;
        public MaterialPropertyRecord[] MaterialPropertyBuffer;

        // Wiki pages the player has unlocked. Populated by WikiUtility.UnlockPage; read by
        // WikiAvailabilityUtility and WikiUtility's lock queries to decide which tabs/pages
        // are exposed. Account-scoped, so it persists across minigames.
        public HashSet<StringHash32> UnlockedWikiPages;
        
        #endregion // Save State

        public Dictionary<StringHash32, MaterialPropertyRecord> MaterialProperties;
        public HashSet<StringHash32> CompletedContractIds;

        public void OnDeregister()
        {
        }

        public void OnRegister()
        {
            CompletedContractIds = new HashSet<StringHash32>();
            MaterialProperties = new Dictionary<StringHash32, MaterialPropertyRecord>();
            UnlockedWikiPages ??= new HashSet<StringHash32>();
            SpacefabGame.SaveBuffer.RegisterHandler("PlayerProgressState", this);
        }

        #region Interfaces

        // ISaveStateChunkObject

        public void Read(object self, ref ByteReader reader, SaveStateChunkConsts consts)
        {
            RecentlyCompletedChapter = reader.Read<bool>();

            UnlockedWikiPages ??= new HashSet<StringHash32>();
            UnlockedWikiPages.Clear();
            int count = reader.Read<int>();
            for (int i = 0; i < count; i++)
            {
                UnlockedWikiPages.Add(reader.Read<StringHash32>());
            }
            ElapsedCycles = reader.Read<int>();
            Funds = reader.Read<int>();
            PlayerProgressUtility.UnpackCompletedContracts(this, reader.Read<uint>());
            PlayerProgressUtility.UnpackMaterialProperties(this, ref reader);
            // Appended at the tail; SaveVersion is -1 (in flux), so no
            // version gate. When SaveVersion is fixed, move into a
            // versioned slot.
            BigBatteryUnlocked = reader.Read<bool>();
            InitialUnlocksApplied = reader.Read<bool>();
        }

        public void Write(object self, ref ByteWriter writer, SaveStateChunkConsts consts)
        {
            writer.Write(RecentlyCompletedChapter);

            int count = UnlockedWikiPages?.Count ?? 0;
            writer.Write(count);
            if (UnlockedWikiPages != null)
            {
                foreach (StringHash32 id in UnlockedWikiPages)
                {
                    writer.Write(id);
                }
            }
            writer.Write(ElapsedCycles);
            writer.Write(Funds);
            writer.Write(PlayerProgressUtility.PackCompletedContracts(this));
            PlayerProgressUtility.PackMaterialProperties(this, ref writer);
            writer.Write(BigBatteryUnlocked);
            writer.Write(InitialUnlocksApplied);
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
            var contractOrder = Find.GlobalAsset<ContractManifest>();
            state.CompletedContractBuffer = 0;
            for(int i = 0; i < contractOrder.Contracts.Length; i++) {
                StringHash32 contractId = contractOrder.Contracts[i];
                if (!contractId.IsEmpty && state.CompletedContractIds.Contains(contractId)) {
                    state.CompletedContractBuffer |= (1u << i);
                }
            }
            return state.CompletedContractBuffer;
        }

        /// <summary>
        /// Unpacks a saved bitmask back into CompletedContractIds using ContractOrderAsset.
        /// </summary>
        public static void UnpackCompletedContracts(PlayerProgressState state, uint mask)
        {
            var contractOrder = Find.GlobalAsset<ContractManifest>();
            state.CompletedContractIds.Clear();
            for (int i = 0; i < contractOrder.Contracts.Length; i++)
            {
                if ((mask & (1u << i)) != 0)
                {
                    state.CompletedContractIds.Add(contractOrder.Contracts[i]);
                }
            }
        }

        /// <summary>
        /// Returns true if the given persistent property has been confirmed for the material.
        /// For dynamic labels (PDopantFor / NDopantFor), contextMaterialId names the X
        /// in "P-Type Dopant for X". For static labels, contextMaterialId is ignored.
        /// Observation-only labels always return false.
        /// </summary>
        public static bool HasConfirmed(PlayerProgressState state, StringHash32 materialId, MaterialPropertyLabel label, StringHash32 contextMaterialId)
        {
            if (!state.MaterialProperties.TryGetValue(materialId, out var record))
            {
                return false;
            }
            return MaterialPropertyRecordUtility.Has(record, label, contextMaterialId);
        }

        /// <summary>
        /// Marks the given persistent property as confirmed for the material.
        /// Observation-only labels are silently ignored. For dynamic labels,
        /// contextMaterialId is required and resolved through MaterialOrderAsset.
        /// Idempotent (OR-mask semantics).
        /// </summary>
        public static void Confirm(PlayerProgressState state, StringHash32 materialId, MaterialPropertyLabel label, StringHash32 contextMaterialId)
        {
            state.MaterialProperties.TryGetValue(materialId, out var record);
            if (MaterialPropertyRecordUtility.TrySet(ref record, label, contextMaterialId))
            {
                state.MaterialProperties[materialId] = record;
            }
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
                if (!MaterialPropertyRecordUtility.IsEmpty(record))
                {
                    state.MaterialProperties[materialOrder.GetId(i)] = record;
                }
            }
        }

        /// <summary>
        /// Applies the WikiInitialUnlocksConfig page-id list on first
        /// startup per save. Short-circuits when the save has already
        /// had its initial-unlock pass run (the InitialUnlocksApplied
        /// flag is persisted), so this is a one-shot per save —
        /// editing the config afterwards only affects fresh saves.
        /// Missing config and empty/null id arrays are treated as
        /// no-ops (still sets the flag so the system doesn't keep
        /// checking the asset forever). UnlockPage itself
        /// short-circuits per-id duplicates via HashSet.Add.
        ///
        /// Called from OverarchingStartupSequenceSystem on entry to
        /// the Overarching scene; could be called from any startup
        /// site that runs once per save after the save buffer has
        /// been read.
        /// </summary>
        public static void TryApplyInitialWikiUnlocks(PlayerProgressState progressState)
        {
            if (progressState == null || progressState.InitialUnlocksApplied)
            {
                return;
            }
            WikiInitialUnlocksConfig config = Find.GlobalAsset<WikiInitialUnlocksConfig>();
            if (config != null)
            {
                StringHash32[] ids = config.InitialUnlockedPages;
                if (ids != null)
                {
                    for (int i = 0; i < ids.Length; i++)
                    {
                        WikiUtility.UnlockPage(progressState, ids[i]);
                    }
                }
            }
            progressState.InitialUnlocksApplied = true;
        }
    }
}