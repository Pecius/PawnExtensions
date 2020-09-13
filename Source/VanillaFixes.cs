using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace PawnExtensions
{
    namespace HarmonyPatches
    {
        [HarmonyPatch(typeof(GenTypes))]
        [HarmonyPatch("AllActiveAssemblies", MethodType.Getter)]
        internal class GenTypes_AllActiveAssembliesPatch
        {
            // Fixes the "already has short hash" error
            private static IEnumerable<Assembly> Postfix(IEnumerable<Assembly> assemblies)
            {
                foreach (Assembly assembly in assemblies.ToHashSet())
                    yield return assembly;
            }
        }

        [HarmonyPatch(typeof(MeditationUtility))]
        [HarmonyPatch(nameof(MeditationUtility.CanMeditateNow))]
        internal class MeditationUtility_CanMeditateNowPatch
        {
            // Fixes the null reference error if the pawn has no food need
            private static bool Prefix(Pawn pawn, ref bool __result)
            {
                if (pawn.needs.food == null)
                {
                    __result = false;
                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(ThinkNode_ConditionalNeedPercentageAbove))]
        [HarmonyPatch("Satisfied")]
        internal class ThinkNode_ConditionalNeedPercentageAbove_SatisfiedPatch
        {
            // Fixes the null reference error if the pawn has no requested need
            private static IEnumerable<CodeInstruction> Transpiler(ILGenerator generator, IEnumerable<CodeInstruction> instructions)
            {
                bool done = false;
                var code = new List<CodeInstruction>(instructions);
                MethodInfo target = AccessTools.Method(typeof(Pawn_NeedsTracker), "TryGetNeed", new Type[] { typeof(NeedDef) });

                for (int i = 0; i < code.Count; i++)
                {
                    CodeInstruction inst = code[i];

                    if (inst.opcode == OpCodes.Callvirt && (MethodInfo)inst.operand == target)
                    {
                        yield return inst;
                        Label label = generator.DefineLabel();
                        code[i + 1].labels.Add(label);

                        yield return new CodeInstruction(OpCodes.Dup);
                        yield return new CodeInstruction(OpCodes.Brtrue_S, label);
                        yield return new CodeInstruction(OpCodes.Pop);
                        yield return new CodeInstruction(OpCodes.Ldc_I4_1);
                        yield return new CodeInstruction(OpCodes.Ret);

                        done = true;
                        continue;
                    }

                    yield return inst;
                }

                if (!done)
                    Log.Error("Could not patch ThinkNode_ConditionalNeedPercentageAbove.Satisfied");
            }
        }
    }
}