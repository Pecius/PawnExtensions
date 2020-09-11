using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Verse;

namespace PawnExtensions
{
    public class DefBackCompatibilityConverter : BackCompatibilityConverter
    {
        private static bool installed;
        private HashSet<object> alreadyPostprocessed = new HashSet<object>();
        private List<BackCompatibilityDef> compatibilityDefs;
        private HashSet<string> pawnBackstoryToRefresh = new HashSet<string>();

        public static void InstallConverter()
        {
            if (installed)
            {
                Log.Error("Attempted to install back compatibility converter when it was already installed.");
                return;
            }

            var converter = new DefBackCompatibilityConverter();
            var conversionChain = Traverse.Create(typeof(Verse.BackCompatibility)).Field<List<BackCompatibilityConverter>>("conversionChain").Value;

            conversionChain.Add(converter);

            installed = true;
        }

        public override bool AppliesToVersion(int majorVer, int minorVer)
        {
            return true;
        }

        public override string BackCompatibleDefName(Type defType, string defName, bool forDefInjections = false, XmlNode node = null)
        {
            if (compatibilityDefs == null)
                return null;

            foreach (var def in compatibilityDefs)
            {
                if (def.TryGetDefReplacement(defType, defName, out string replacement))
                {
                    Log.Message($"Missing '{defName}' def has been replaced with '{replacement}' def");
                    return replacement;
                }
            }

            return null;
        }

        public override Type GetBackCompatibleType(Type baseType, string providedClassName, XmlNode node)
        {
            if (compatibilityDefs == null)
                return null;

            if (baseType == typeof(Pawn_StoryTracker))
            {
                BackCompatibleBackstory(node["childhood"]);
                BackCompatibleBackstory(node["adulthood"]);
            }

            return null;
        }

        public override void PostExposeData(object obj)
        {
            if (compatibilityDefs == null || alreadyPostprocessed.Contains(obj))
                return;

            if (obj is Pawn pawn)
            {
                var storyTracker = pawn.story;

                if (storyTracker == null || pawn.kindDef == null)
                    return;

                string uniqueID = pawn.GetUniqueLoadID();

                if (pawn.def != pawn.kindDef.race)
                {
                    LoadReferenceUtils.AddAlias(pawn, uniqueID);
                    pawn.def = pawn.kindDef.race;
                }

                if (pawnBackstoryToRefresh.Remove(uniqueID) ||
                    compatibilityDefs.Any(t => t.ShouldRefreshBackstory(storyTracker.adulthood?.identifier) || t.ShouldRefreshBackstory(storyTracker.childhood?.identifier)))
                {
                    StoryTrackerUtils.RefreshStoryTracker(storyTracker, pawn);
                    Log.Message($"Updating backstory for pawn {pawn.Name}");
                }
            }
            else
                return;

            alreadyPostprocessed.Add(obj);
        }

        public override void PostLoadSavegame(string loadingVersion)
        {
            if (compatibilityDefs != null)
            {
                Log.Message($"Back compatibility conversion finished!");

                compatibilityDefs.ForEach(d => ScribeCompatUtility.InstalledCompatability(d));

                compatibilityDefs = null;
                alreadyPostprocessed.Clear();
                pawnBackstoryToRefresh.Clear();
            }
        }

        public override void PreLoadSavegame(string loadingVersion)
        {
            compatibilityDefs = ScribeCompatUtility.GetCompatibilityDefsToExecute();

            if (compatibilityDefs.Count != 0)
            {
                Log.Message("Running back compatibility converter with listed compatibility defs:");
                compatibilityDefs.ForEach(t => Log.Message(t.defName));
            }
            else
                compatibilityDefs = null;
        }

        private void BackCompatibleBackstory(XmlNode node)
        {
            string childValue = node?.FirstChild?.Value;
            if (childValue == null)
                return;

            foreach (var def in compatibilityDefs)
            {
                XmlNode destinationNode;
                string prefix;
                string newBackstory;

                if (def.TryGetBackstoryReplacement(childValue, out newBackstory))
                {
                    destinationNode = node;
                    prefix = "";
                }
                else if (def.TryGetBackstoryOpposingReplacement(childValue, out newBackstory))
                {
                    var parentNode = node.ParentNode;

                    destinationNode = node.Name == "adulthood" ? parentNode["childhood"] : parentNode["adulthood"];
                    prefix = "opposing";
                }
                else
                    continue;

                if (!BackstoryDatabase.TryGetWithIdentifier(newBackstory, out Backstory backstory))
                {
                    Log.Error($"Couldn't replace {prefix} backstory '{destinationNode.FirstChild.Value}' with identifier '{newBackstory}' - not found.");
                    continue;
                }
                Log.Message($"Replacing backstory {prefix} '{destinationNode.FirstChild.Value}' with '{newBackstory}'");

                destinationNode.FirstChild.Value = backstory.identifier;

                var thingNode = node.ParentNode.ParentNode;
                string pawnID = thingNode["id"]?.FirstChild?.Value;

                if (pawnID != null)
                    pawnBackstoryToRefresh.Add($"Thing_{pawnID}");
                else
                    Log.Warning("pawnID is null");
            }
        }

        public static class LoadReferenceUtils
        {
            public static void AddAlias(ILoadReferenceable referencable, string alias)
            {
                var loadedObjectDirectory = Traverse.Create(Scribe.loader.crossRefs).Field("loadedObjectDirectory").Field<Dictionary<string, ILoadReferenceable>>("allObjectsByLoadID").Value;

                loadedObjectDirectory.Add(alias, referencable);
            }
        }

        public static class StoryTrackerUtils
        {
            private static readonly Traverse GenerateBodyType = Traverse.Create(typeof(PawnGenerator)).Method("GenerateBodyType_NewTemp", new Type[] { typeof(Pawn), typeof(PawnGenerationRequest) });
            private static readonly Traverse GenerateTraits = Traverse.Create(typeof(PawnGenerator)).Method("GenerateTraits", new Type[] { typeof(Pawn), typeof(PawnGenerationRequest) });

            public static void RefreshStoryTracker(Pawn_StoryTracker storyTracker, Pawn pawn)
            {
                var req = new PawnGenerationRequest(pawn.kindDef, canGeneratePawnRelations: false);

                if (storyTracker.adulthood != null)
                    GenerateBodyType.GetValue(pawn, default(PawnGenerationRequest));

                if (storyTracker.adulthood?.forcedTraits != null || storyTracker.childhood?.forcedTraits != null)
                {
                    storyTracker.traits.allTraits.Clear();
                    GenerateTraits.GetValue(pawn, req);
                }
            }
        }
    }
}