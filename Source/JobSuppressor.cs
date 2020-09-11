using HarmonyLib;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace PawnExtensions
{
    namespace HarmonyPatches
    {
        [HarmonyPatch(typeof(Pawn_JobTracker))]
        [HarmonyPatch(nameof(Pawn_JobTracker.StartJob))]
        internal class Pawn_JobTracker_StartJobPatch
        {
            private static bool Prefix(Job newJob, Pawn ___pawn)
            {
                var jobDefs = ___pawn.GetPawnExtensions()?.suppressJobs?.hard;

                return jobDefs == null || !jobDefs.Contains(newJob.def);
            }
        }

        [HarmonyPatch(typeof(ThinkNode_JobGiver))]
        [HarmonyPatch(nameof(ThinkNode_JobGiver.TryIssueJobPackage))]
        internal class ThinkNode_JobGiver_Pawn_JobTrackerPatch
        {
            private static void Postfix(Pawn pawn, JobIssueParams jobParams, ref ThinkResult __result)
            {
                var jobDefs = pawn.GetPawnExtensions()?.suppressJobs?.soft;
                var job = __result.Job;

                if (jobDefs != null && job != null && jobDefs.Contains(job.def))
                    __result = ThinkResult.NoJob;
            }
        }
    }

    public class JobSuppressor
    {
        public List<JobDef> hard;
        public List<JobDef> soft;
    }
}