using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using HarmonyLib;

namespace PawnExtensions
{
    class CompProperties_PawnSpawner : CompProperties
    {
        public List<PawnKindDef> spawnablePawnKinds;
        public Dictionary<SkillDef, IntRange> forceSkills;
        public bool needsActivation = false;
        public BodyTypeDef bodyType;
        public BackstoryDef childStory;
        public BackstoryDef adultStory;

        public CompProperties_PawnSpawner()
        {
            compClass = typeof(CompPawnSpawner);
        }
    }

    class CompPawnSpawner : ThingComp
    {
        private PawnKindDef chosenKind;

        private CompProperties_PawnSpawner Props => (CompProperties_PawnSpawner)props;
        private bool CanInstantlyActivate => !Props.needsActivation;

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);

            if (chosenKind == null)
                chosenKind = Props.spawnablePawnKinds.RandomElement();
        }

        public override void CompTick()
        {
            if (CanInstantlyActivate)
            {
                SpawnPawn();
            }
        }

        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            if (Props.needsActivation)
            {
                yield return new FloatMenuOption("Activate", delegate
                {
                    SpawnPawn();
                });
            }
        }

        private void SpawnPawn()
        {
            Faction faction = FactionUtility.DefaultFactionFrom(chosenKind.defaultFactionType);
            Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(chosenKind, faction, canGeneratePawnRelations: false, allowFood: false, allowAddictions: false));

            if (Props.bodyType != null)
                pawn.story.bodyType = Props.bodyType;

            if (Props.childStory != null)
                pawn.story.Childhood = Props.childStory;

            if (Props.adultStory != null)
                pawn.story.Adulthood = Props.adultStory;

            if (Props.adultStory != null || Props.childStory != null)
            {
                pawn.story.traits.allTraits.Clear();
                Traverse.Create(typeof(PawnGenerator)).Method("GenerateTraits", pawn, new PawnGenerationRequest(chosenKind)).GetValue();
            }


            if (Props.forceSkills != null)
            {
                var skills = pawn.skills.skills;

                foreach (var kv in Props.forceSkills)
                {
                    var skill = skills.FirstOrFallback(t => t.def == kv.Key);
                    if (skill == null)
                    {
                        skill = new SkillRecord(pawn, kv.Key);
                        skills.Add(skill);
                    }

                    skill.Level = kv.Value.RandomInRange;
                }
            }

            GenSpawn.Spawn(pawn, parent.Position, parent.Map);

            parent.Destroy();
        }
    }
}
