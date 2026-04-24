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
        public HashSet<StringHash32> AvailableMaterials;
        public HashSet<StringHash32> ResearchedMaterials;

        public bool RecentlyCompletedChapter;
        public List<StringHash32> CompletedContractIds;

        // Wiki pages the player has unlocked. Populated by WikiUtility.UnlockPage; read by
        // WikiAvailabilityUtility and WikiUtility's lock queries to decide which tabs/pages
        // are exposed. Account-scoped, so it persists across minigames.
        public HashSet<StringHash32> UnlockedWikiPages;

        public void OnDeregister()
        {
        }

        public void OnRegister()
        {
            SpacefabGame.SaveBuffer.RegisterHandler("PlayerProgressState", this);
        }

        #region Interfaces

        // ISaveStateChunkObject

        public void Read(object self, ref ByteReader reader, SaveStateChunkConsts consts)
        {
            RecentlyCompletedChapter = reader.Read<bool>();

            // Appended field — gate on Remaining so pre-wiki saves don't crash.
            UnlockedWikiPages ??= new HashSet<StringHash32>();
            UnlockedWikiPages.Clear();
            if (reader.Remaining > 0)
            {
                int count = reader.Read<int>();
                for (int i = 0; i < count; i++)
                {
                    UnlockedWikiPages.Add(reader.Read<StringHash32>());
                }
            }
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
    }
}