using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using Verse.Sound;

namespace PawnExtensions
{
    namespace HarmonyPatches
    {
        [HarmonyPatch(typeof(DamageWorker_AddInjury))]
        [HarmonyPatch("PlayWoundedVoiceSound")]
        internal class DamageWorker_AddInjury_PlayWoundedVoiceSoundPatch
        {
            private static void Postfix(DamageInfo dinfo, Pawn pawn)
            {
                var comp = pawn.GetComp<Comp_SapientCalls>();

                if (comp != null && !pawn.Dead && !dinfo.InstantPermanentInjury && pawn.SpawnedOrAnyParentSpawned && dinfo.Def.ExternalViolenceFor(pawn))
                {
                    comp.PlayWoundedSound();
                }
            }
        }

        [HarmonyPatch(typeof(Pawn))]
        [HarmonyPatch(nameof(Pawn.Kill))]
        internal class Pawn_KillPatch
        {
            private static void Prefix(DamageInfo? dinfo, Hediff exactCulprit, Pawn __instance)
            {
                Pawn pawn = __instance;
                var comp = pawn.GetComp<Comp_SapientCalls>();

                if (comp != null && pawn.Spawned && dinfo.HasValue && dinfo.Value.Def.ExternalViolenceFor(pawn))
                {
                    comp.PlaySound(comp.CurrentSet.onDeath);
                }
            }
        }
    }

    public class Comp_SapientCalls : ThingComp
    {
        private SapientCalls.CallSet currentSet;
        private int globalCooldown = 0;
        private SoundTrigger onWoundedTrigger;
        private List<SoundTrigger> triggers;
        public SapientCalls.CallSet CurrentSet => currentSet;
        public SapientCalls Props => (SapientCalls)props;
        private Pawn Pawn => (Pawn)parent;
        public override void CompTick()
        {
            if (triggers == null)
                return;

            if (globalCooldown > 0)
            {
                globalCooldown--;

                foreach (var trigger in triggers)
                    trigger.Update();

                return;
            }

            onWoundedTrigger?.Update();
            if (!Pawn.Downed && Pawn.Awake())
                DoCall();
        }

        public void DoCall()
        {
            //Rand.PushState();

            bool called = false;
            foreach (var trigger in triggers)
            {
                if (trigger.CanDo())
                {
                    if (!called && Rand.Chance(trigger.Call.chance))
                    {
                        PlaySound(trigger.Call);

                        globalCooldown = Props.callCooldownRange.RandomInRange;

                        called = true;
                    }
                }
            }

            //Rand.PopState();
        }

        public void InitializeTriggers()
        {
            triggers = new List<SoundTrigger>();
            currentSet = Props.calls;

            if (Props.linked != null)
            {
                foreach (var linked in Props.linked)
                {
                    if (linked.backstory != null &&
                        linked.backstory != Pawn.story.adulthood.identifier &&
                        linked.backstory != Pawn.story.childhood.identifier)
                        continue;

                    if (linked.lifestage != null && linked.lifestage != Pawn.ageTracker.CurLifeStage)
                        continue;

                    currentSet = linked.calls;

                    break;
                }
            }

            if (currentSet.onAim != null)
                triggers.Add(new SoundTrigger(currentSet.onAim, (state) => state != (Pawn.stances.curStance is Stance_Warmup)));

            if (currentSet.onDraft != null)
                triggers.Add(new SoundTrigger(currentSet.onDraft, (state) => state != Pawn.drafter.Drafted));

            if (currentSet.draftIdle != null)
                triggers.Add(new SoundTrigger(currentSet.draftIdle, (state) => Pawn.drafter.Drafted));

            if (currentSet.onWounded != null)
                onWoundedTrigger = new SoundTrigger(currentSet.onWounded);
        }

        public void PlaySound(SapientCalls.Call sound)
        {
            if (sound != null && Find.CameraDriver.CurrentViewRect.ExpandedBy(10).Contains(Pawn.Position))
            {
                SoundInfo info = SoundInfo.InMap(new TargetInfo(Pawn.PositionHeld, Pawn.MapHeld));
                Rand.PushState();
                sound.sound.PlayOneShot(info);
                Rand.PopState();
            }
        }

        public void PlayWoundedSound()
        {
            if (onWoundedTrigger.CanDo(0))
            {
                PlaySound(onWoundedTrigger.Call);
            }
        }

        public override void PostDeSpawn(Map map)
        {
            base.PostDeSpawn(map);
            triggers = null;
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            InitializeTriggers();
        }
    }

    public class SapientCalls : CompProperties
    {
        public IntRange callCooldownRange = new IntRange(60 * 3, 60 * 7);

        public CallSet calls;

        public List<CallLinked> linked;

        public SapientCalls()
        {
            compClass = typeof(Comp_SapientCalls);
        }

        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            foreach (var error in calls.ConfigErrors())
                yield return $"call: {error}";

            int index = 0;

            if (linked != null)
            {
                foreach (var linkedCall in linked)
                {
                    foreach (var error in linkedCall.ConfigErrors())
                        yield return $"call linked {index}: {error}";

                    index++;
                }
            }
        }

        public class Call
        {
            public float chance = 1f;
            public IntRange cooldownRange = new IntRange(60 * 3, 60 * 9);
            public SoundDef sound;
        };

        public class CallLinked
        {
            public string backstory;
            public CallSet calls;
            public LifeStageDef lifestage;

            public IEnumerable<string> ConfigErrors()
            {
                foreach (var error in calls.ConfigErrors())
                    yield return error;

                if (backstory == null && lifestage == null)
                    yield return "backstory and lifestage are both null";

                if (backstory != null && !BackstoryDatabase.allBackstories.ContainsKey(backstory))
                    yield return $"backstory '{backstory}' not found in the database";
            }
        }

        public class CallSet
        {
            public Call draftIdle;
            public Call onAim;
            public Call onDeath;
            public Call onDraft;
            public Call onWounded;

            public IEnumerable<string> ConfigErrors()
            {
                if (!IsCallSoundValid(onAim))
                    yield return "onAim.sound is null";

                if (!IsCallSoundValid(onDraft))
                    yield return "onDraft.sound is null";

                if (!IsCallSoundValid(draftIdle))
                    yield return "draftIdle.sound is null";

                if (!IsCallSoundValid(onDeath))
                    yield return "onDeath.sound is null";

                if (!IsCallSoundValid(onWounded))
                    yield return "onWounded.sound is null";
            }

            private bool IsCallSoundValid(Call call)
            {
                return call == null || call.sound != null;
            }
        }
    }

    public class SoundTrigger
    {
        private readonly SapientCalls.Call call;
        private readonly Func<bool, bool> trigger;
        private int cooldown = 0;
        private bool state = false;

        public SapientCalls.Call Call => call;

        public SoundTrigger(SapientCalls.Call call, Func<bool, bool> trigger = null)
        {
            this.trigger = trigger;
            this.call = call;
        }
        public bool CanDo(int ticks = 1)
        {
            if (cooldown > 0)
            {
                cooldown -= ticks;
                return false;
            }

            if (trigger == null)
            {
                cooldown = call.cooldownRange.RandomInRange;
                return true;
            }

            bool newState = trigger(state);

            if (newState)
            {
                state = !state;

                if (state)
                {
                    cooldown = call.cooldownRange.RandomInRange;
                }

                return state;
            }

            return false;
        }

        public void Update(int ticks = 1)
        {
            if (cooldown > 0)
            {
                cooldown -= ticks;
            }
        }
    }
}