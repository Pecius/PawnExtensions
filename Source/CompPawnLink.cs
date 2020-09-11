using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace PawnExtensions
{
    public class CompProperties_PawnLink : CompProperties
    {
        public HediffDef hediff;
        public PawnLinkNetworkDef networkDef;
        public BodyPartDef part;
        public StatDef statMaxLinked;
        public StatDef statRange;
        public StatDef statRepeater;
        public CompProperties_PawnLink()
        {
            compClass = typeof(CompPawnLink);
        }

        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            if (hediff == null)
                yield return "hediff is null";

            if (networkDef == null)
                yield return "networkDef is null";

            if (statRange == null)
                yield return "statRange is null";

            if (statMaxLinked == null)
                yield return "statMaxLinked is null";

            if (statRepeater == null)
                yield return "statRepeater is null";

            foreach (string error in base.ConfigErrors(parentDef))
                yield return error;
        }
    }

    public class PawnLinkNetworkDef : Def
    {
    }

    public class StatWorker_PawnLink : StatWorker
    {
        public override bool IsDisabledFor(Thing thing)
        {
            if (thing.TryGetComp<CompPawnLink>() != null)
                return false;

            return true;
        }

        public override bool ShouldShowFor(StatRequest req)
        {
            return !IsDisabledFor(req.Thing);
        }
    }

    public class StatWorker_PawnLinkRepeater : StatWorker_PawnLink
    {
        public override string ValueToString(float val, bool finalized, ToStringNumberSense numberSense = ToStringNumberSense.Absolute)
        {
            if (val == 1f)
                return "Yes".Translate();

            return "No".Translate();
        }
    }

    internal class CompPawnLink : ThingComp
    {
        private BodyPartRecord affectedPart;
        private PawnLinkNetwork link;
        public PawnLinkNetwork Link => link;
        public CompProperties_PawnLink Props => (CompProperties_PawnLink)props;
        private Pawn Owner => parent as Pawn;

        public override void CompTickRare()
        {
            base.CompTickRare();

            if (Owner.Spawned)
            {
                List<Pawn> pawns = Owner.Map.mapPawns.PawnsInFaction(Owner.Faction);
                link.Scan(pawns, (int)Owner.GetStatValue(Props.statRange), (int)Owner.GetStatValue(Props.statMaxLinked));
                Notify_NetworkChanged();
                return;
            }

            Caravan caravan = Owner.GetCaravan();
            if (caravan != null)
            {
                link.ScanCaravan(caravan.pawns.InnerListForReading, (int)Owner.GetStatValue(Props.statMaxLinked));
                Notify_NetworkChanged();
            }
        }

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            link = new PawnLinkNetwork(parent as Pawn, Props.networkDef);

            if (Props.part != null)
                affectedPart = Owner.RaceProps.body.GetPartsWithDef(Props.part).First();
        }
        private void Notify_NetworkChanged()
        {
            float severity = link.LinkedCount;

            Hediff hediff = Owner.health.hediffSet.GetFirstHediffOfDef(Props.hediff);
            if (hediff == null)
            {
                if (severity == 0f)
                    return;

                hediff = HediffMaker.MakeHediff(Props.hediff, Owner, affectedPart);
                Owner.health.AddHediff(hediff);
            }

            hediff.Severity = severity;
        }
    }

    internal class PawnLinkNetwork
    {
        private readonly HashSet<Pawn> linkedPawns = new HashSet<Pawn>();
        private readonly PawnLinkNetworkDef network;
        private readonly Pawn owner;
        private int rangeCache;

        public int LinkedCount => LinkedPawns.Count();

        public IEnumerable<Pawn> LinkedPawns
        {
            get
            {
                var done = new HashSet<Pawn> { owner };

                return GetLinkedPawnsHelper(linkedPawns, done);
            }
        }

        public PawnLinkNetwork(Pawn pawn, PawnLinkNetworkDef networkDef)
        {
            owner = pawn;
            network = networkDef;
        }
        public void Scan(List<Pawn> pawns, int range, int maxLinked)
        {
            rangeCache = range * range;
            IntVec3 parentPosition = owner.Position;

            linkedPawns.RemoveWhere(p => !p.Spawned || parentPosition.DistanceToSquared(p.Position) > rangeCache);

            if (linkedPawns.Count >= maxLinked)
                return;

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                PawnLinkNetwork link = pawn.GetComp<CompPawnLink>()?.Link;

                if (link == null || network != link.network || owner == pawn)
                    continue;

                int maxRange = Math.Max(rangeCache, link.rangeCache);

                if (parentPosition.DistanceToSquared(pawn.Position) <= maxRange)
                {
                    if (linkedPawns.Add(pawn) && linkedPawns.Count >= maxLinked)
                        break;
                }
            }
        }

        public void ScanCaravan(List<Pawn> pawns, int maxLinked)
        {
            linkedPawns.RemoveWhere(p => !pawns.Contains(p));

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];

                CompPawnLink link = pawn.GetComp<CompPawnLink>();
                if (link == null || network != link.Link.network || owner == pawn)
                    continue;

                if (linkedPawns.Add(pawn) && linkedPawns.Count > maxLinked)
                    break;
            }
        }
        private static IEnumerable<Pawn> GetLinkedPawnsHelper(IEnumerable<Pawn> pawns, HashSet<Pawn> done)
        {
            foreach (Pawn pawn in pawns)
            {
                if (done.Contains(pawn))
                    continue;

                done.Add(pawn);

                yield return pawn;

                CompPawnLink link = pawn.GetComp<CompPawnLink>();
                if (pawn.GetStatValue(link.Props.statRepeater) != 0f)
                {
                    foreach (Pawn p in GetLinkedPawnsHelper(link.Link.linkedPawns, done))
                        yield return p;
                }
            }
        }
    }
}