using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace PawnExtensions
{
    namespace HarmonyPatches
    {
        [HarmonyPatch(typeof(HediffSet))]
        [HarmonyPatch("CalculatePain")]
        internal class HediffSet_CalculatePainPatch
        {
            private static bool Prefix(ref float __result, Pawn ___pawn)
            {
                ExtensionsDef extension = ___pawn.GetPawnExtensions();
                if (extension != null && extension.feelsNoPain)
                {
                    __result = 0;
                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(ImmunityHandler))]
        [HarmonyPatch(nameof(ImmunityHandler.DiseaseContractChanceFactor))]
        [HarmonyPatch(new Type[] { typeof(HediffDef), typeof(HediffDef), typeof(BodyPartRecord) }, new ArgumentType[] { ArgumentType.Normal, ArgumentType.Out, ArgumentType.Normal })]
        internal class ImmunityHandler_DiseaseContractChanceFactorPatch
        {
            private static bool Prefix(HediffDef diseaseDef, out HediffDef immunityCause, BodyPartRecord part, ref float __result, Pawn ___pawn)
            {
                immunityCause = null;
                var diseaseImmunity = ___pawn.GetPawnExtensions()?.diseaseImmunity;

                if (diseaseImmunity != null && diseaseImmunity.hediffs.Contains(diseaseDef) != diseaseImmunity.restrict)
                {
                    __result = 0f;
                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(Pawn_RelationsTracker))]
        [HarmonyPatch(nameof(Pawn_RelationsTracker.SecondaryLovinChanceFactor))]
        internal class IPawn_RelationsTracker_SecondaryLovinChanceFactorPatch
        {
            private static bool Prefix(Pawn otherPawn, Pawn ___pawn, ref float __result)
            {
                var pawnCant = ___pawn.GetPawnExtensions()?.prohibitRomance;
                var otherCant = otherPawn.GetPawnExtensions()?.prohibitRomance;

                if ((pawnCant != null && pawnCant == true) || (otherCant != null && otherCant == true))
                {
                    __result = 0;
                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(PawnBreathMoteMaker))]
        [HarmonyPatch(nameof(PawnBreathMoteMaker.ProcessPostTickVisuals))]
        internal class PawnBreathMoteMaker_BreathMoteMakerTickPatch
        {
            private static bool Prefix(Pawn ___pawn)
            {
                ExtensionsDef extension = ___pawn.GetPawnExtensions();
                if (extension != null && extension.disableBreathMote)
                    return false;

                return true;
            }
        }

        [HarmonyPatch(typeof(PawnGenerator))]
        [HarmonyPatch("GenerateSkills")]
        internal class PawnGenerator_GenerateSkillsPatch
        {
            private static void Postfix(Pawn pawn)
            {
                ExtensionsDef extension = pawn.GetPawnExtensions();
                if (extension != null && !extension.hasPassions)
                {
                    foreach (SkillRecord skill in pawn.skills.skills)
                    {
                        skill.passion = Passion.None;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Need))]
        [HarmonyPatch("DrawOnGUI")]
        internal class Need_DrawOnGUIPatch
        {
            private static bool Prefix(Need __instance, Pawn ___pawn)
            {
                var extension = ___pawn.GetPawnExtensions();

                if (extension?.hideNeeds != null && extension.hideNeeds.Contains(__instance.GetType().Name))
                    return false;

                return true;
            }
        }
    }

    public static class PawnExtensionsDefModUtil
    {
        public static ExtensionsDef GetPawnExtensions(this Pawn pawn)
        {
            return pawn.def.GetModExtension<ExtensionsDef>();
        }
    }

    public class ExtensionsDef : DefModExtension
    {
        public bool disableBreathMote;
        public HediffImmunity diseaseImmunity;
        public bool feelsNoPain;
        public bool hasPassions = true;
        public List<HediffOnDamageRule> hediffOnDamage;
        public bool prohibitRomance;
        public List<string> hideNeeds;

        //public RenamerDefs renamer;
        public JobSuppressor suppressJobs;

        public override IEnumerable<string> ConfigErrors()
        {
            if (hediffOnDamage != null)
            {
                foreach (string error in CheckErrorsInList(hediffOnDamage, nameof(hediffOnDamage)))
                    yield return error;
            }

            if (diseaseImmunity != null)
            {
                foreach (string error in diseaseImmunity.ConfigErrors())
                    yield return error;
            }
        }

        private IEnumerable<string> CheckErrorsInList(IEnumerable<Editable> items, string name)
        {
            int pos = 0;

            foreach (Editable editable in items)
            {
                foreach (string error in editable.ConfigErrors())
                    yield return $"PawnExtensions.ExtensionsDef.{name}[{pos}]: {error}";

                pos++;
            }
        }
    }
}