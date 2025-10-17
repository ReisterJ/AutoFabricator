using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse.AI;
using Verse;

namespace AutoFabricator
{
    public class WorkGiver_FabricatorHaul : WorkGiver_Scanner
    {
        private readonly JobDef haulToFabricator = DefDatabase<JobDef>.GetNamed("HaulToFabricator");
        public override PathEndMode PathEndMode => PathEndMode.Touch;
       
        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            foreach (var comp in Find.CurrentMap.listerBuildings.AllBuildingsColonistOfClass<Building>())
            {
                var autoFab = comp.TryGetComp<Comp_AutoFabricator>();
                if (autoFab != null && autoFab.currentOrder != null &&  autoFab.NeedHaulWork())
                    yield return comp;
            }
        }
        
       
        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            
            var comp = t.TryGetComp<Comp_AutoFabricator>();
            if(comp == null)
            {
                return false;
            }
            if (!comp.NeedHaulWork())
            {
                return false;
            }
            if (!pawn.CanReserve(t, 1, -1, null, forced))
            {
                return false;
            }
            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            var comp = t.TryGetComp<Comp_AutoFabricator>();

            foreach (var need in comp.pendingMaterials)
            {
                int needed = need.count - comp.GetStoredMaterial(need.thingDef);
                if (needed > 0)
                {
                    Thing found = GenClosest.ClosestThingReachable(
                        pawn.Position, pawn.Map,
                        ThingRequest.ForDef(need.thingDef),
                        PathEndMode.ClosestTouch,
                        TraverseParms.For(pawn),
                        9999f,
                        x => !x.IsForbidden(pawn) && pawn.CanReserve(x)
                    );
                    if (found != null)
                    {
                        int toTake = Mathf.Min(needed, found.stackCount);
                        Job job = JobMaker.MakeJob(haulToFabricator, found, t);
                        job.count = toTake;
                        job.targetB = t;
                        job.targetQueueB = null;
                        job.haulOpportunisticDuplicates = false;
                        job.def = haulToFabricator; 
                        return job;
                    }
                }
            }
            return null;
        }
    }
}
