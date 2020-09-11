using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace PawnExtensions
{
    namespace HarmonyPatches
    {
        [HarmonyPatch(typeof(Game))]
        [HarmonyPatch(nameof(Game.InitNewGame))]
        internal static class Game_InitNewGamePatch
        {
            private static void Prefix()
            {
                ScribeCompatUtility.IsLoadedGame = false;
            }
        }

        [HarmonyPatch]
        internal static class ScribeMetaHeaderUtilityPatch
        {
            private static void Postfix()
            {
                ScribeCompatUtility.ExposeCompatibilityList();
            }

            private static IEnumerable<MethodBase> TargetMethods()
            {
                yield return AccessTools.Method(typeof(ScribeMetaHeaderUtility), nameof(ScribeMetaHeaderUtility.WriteMetaHeader));
                yield return AccessTools.Method(typeof(ScribeMetaHeaderUtility), nameof(ScribeMetaHeaderUtility.LoadGameDataHeader));
            }
        }
    }

    public static class ScribeCompatUtility
    {
        private static List<BackCompatibilityEntry> compatibilityEntries;
        private static List<BackCompatibilityDef> defDatabase = new List<BackCompatibilityDef>();
        private static HashSet<string> saveModIDsHash;
        public static bool IsLoadedGame { get; set; }

        public static void ExposeCompatibilityList()
        {
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                IsLoadedGame = true;

                string steamSuffix = ModMetaData.SteamModPostfix;
                saveModIDsHash = ScribeMetaHeaderUtility.loadedModIdsList.Select(s => s.EndsWith(steamSuffix) ? s.Substring(0, s.Length - steamSuffix.Length) : s)
                    .Where(m => ModsConfig.IsActive(m)).ToHashSet();
            }

            if (Scribe.mode == LoadSaveMode.Saving)
                PrepareForSaving();

            if (Scribe.EnterNode("modBackCompatibility"))
            {
                try
                {
                    Scribe_Collections.Look(ref compatibilityEntries, "installed", LookMode.Deep);
                }
                finally
                {
                    Scribe.ExitNode();
                }
            }
            else if (Scribe.mode == LoadSaveMode.LoadingVars)
                compatibilityEntries = new List<BackCompatibilityEntry>();
        }

        public static List<BackCompatibilityDef> GetCompatibilityDefsToExecute()
        {
            var resultList = new List<BackCompatibilityDef>();
            var activeDefs = defDatabase.Where(d => SaveHasModID(d.ModID));

            foreach (var def in activeDefs)
            {
                string defModID = def.ModID;
                var compatibilityEntry = compatibilityEntries.FirstOrDefault(d => d.ModID == defModID);

                if (compatibilityEntry != null && compatibilityEntry.CompatsInstalled.Contains(def.defName))
                    continue;

                resultList.Add(def);
            }

            return resultList;
        }

        public static void InstalledCompatability(BackCompatibilityDef def)
        {
            string modID = def.ModID;

            var compatibilityList = compatibilityEntries.FirstOrDefault(t => t.ModID == modID);

            if (compatibilityList == null)
            {
                compatibilityList = new BackCompatibilityEntry(modID);
                compatibilityEntries.Add(compatibilityList);
            }

            compatibilityList.CompatsInstalled.Add(def.defName);
        }

        public static void PrepareForSaving()
        {
            compatibilityEntries.RemoveAll(d => !ModsConfig.IsActive(d.ModID));

            foreach (var entry in compatibilityEntries)
                entry.CompatsInstalled.RemoveAll(e => !defDatabase.Any(t => t.defName == e));

            compatibilityEntries.RemoveAll(e => e.CompatsInstalled.Count == 0);

            var remainingModIDs = compatibilityEntries.Select(d => d.ModID).ToHashSet();

            foreach (var def in defDatabase)
            {
                if (!SaveHasModID(def.ModID) && !remainingModIDs.Contains(def.ModID))
                    InstalledCompatability(def);
            }
        }

        public static void RegisterBackCompatibilityDef(BackCompatibilityDef def)
        {
            defDatabase.Add(def);
        }

        private static bool SaveHasModID(string modID)
        {
            return IsLoadedGame && saveModIDsHash.Contains(modID);
        }
    }

    public class BackCompatibilityEntry : IExposable
    {
        private List<string> compatsInstalled;
        private string modID;
        public List<string> CompatsInstalled => compatsInstalled;

        public string ModID => modID;

        public BackCompatibilityEntry()
        {
        }

        public BackCompatibilityEntry(string modID)
        {
            this.modID = modID;
            this.compatsInstalled = new List<string>();
        }
        public void ExposeData()
        {
            Scribe_Values.Look(ref modID, "modID");
            Scribe_Collections.Look(ref compatsInstalled, "compats", LookMode.Value);
        }
    }
}