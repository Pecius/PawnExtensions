using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace PawnExtensions
{
    namespace HarmonyPatches
    {
        [HarmonyPatch(typeof(Pawn_HealthTracker))]
        [HarmonyPatch(nameof(Pawn_HealthTracker.PostApplyDamage))]
        internal class Pawn_HealthTracker_PostApplyDamagePatch
        {
            private static void Postfix(DamageInfo dinfo, float totalDamageDealt, Pawn ___pawn)
            {
                Pawn pawn = ___pawn;

                var extensions = pawn.GetPawnExtensions();
                var rules = extensions?.hediffOnDamage;

                if (rules == null)
                    return;

                foreach (var rule in rules)
                {
                    if (dinfo.Def != rule.damageDef)
                        continue;

                    float severity = rule.damageToSeverityCurve.Evaluate(totalDamageDealt);

                    if (severity == 0)
                        continue;

                    BodyPartRecord part = null;

                    if (rule.createHediffOn != null)
                        part = pawn.RaceProps.body.AllParts.FirstOrDefault((BodyPartRecord p) => p.def == rule.createHediffOn);

                    var hediff = pawn.health.hediffSet.GetFirstHediffOfDef(rule.hediff);
                    if (hediff == null)
                    {
                        hediff = HediffMaker.MakeHediff(rule.hediff, pawn, null);
                        pawn.health.AddHediff(hediff, part);
                    }

                    hediff.Severity += severity;
                }
            }
        }
    }

    public class HediffOnDamageRule : Editable
    {
        public BodyPartDef createHediffOn;
        public DamageDef damageDef;
        public SimpleCurve damageToSeverityCurve = new SimpleCurve { new CurvePoint(0, 1) };
        public HediffDef hediff;
        public override IEnumerable<string> ConfigErrors()
        {
            if (hediff == null)
                yield return "hediff is null";

            if (damageDef == null)
                yield return "damageDef is null";
        }
    }
}