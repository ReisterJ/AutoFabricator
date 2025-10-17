using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using RimWorld;

namespace AutoFabricator
{
    public class JobDriver_HaulToFabricator : JobDriver_HaulToContainer
    {
        public Thing Haulable => job.GetTarget(TargetIndex.A).Thing;
        public Thing Fabricator => job.GetTarget(TargetIndex.B).Thing;

        public Comp_AutoFabricator FabricatorComp => Fabricator.TryGetComp<Comp_AutoFabricator>();

        /*
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            pawn.ReserveAsManyAsPossible(job.GetTargetQueue(TargetIndex.A), job);
            if (pawn.Reserve(job.GetTarget(TargetIndex.B), job, 1, -1, null, errorOnFailed))
            {
                //LM("Pawn Reserve " + job.GetTarget(TargetIndex.B).Thing.Label + " for job Successfully");
                return true;
            }
            else
            {
                //LM("Pawn Reserve " + job.GetTarget(TargetIndex.B).Thing.Label + " for job failed");
                return false;
            }
        }
        */
        protected override IEnumerable<Toil> MakeNewToils()
        {
            AddEndCondition(() => (FabricatorComp.CheckIngredientsFullFillment() ? JobCondition.Succeeded : JobCondition.Ongoing));
            if (FabricatorComp == null)
            {
                Log.Error(GetReport() + "JobDriver_HaulToFabricator : Target fabricator component is null.");
            }
            this.FailOn(() => FabricatorComp == null || FabricatorComp.currentOrder == null);

            /*
             * foreach (var toil in base.MakeNewToils())
            {
                yield return toil;
            }
            this.FailOnDestroyedNullOrForbidden(TargetIndex.B);

            Toil clearQueue = Toils_JobTransforms.ClearDespawnedNullOrForbiddenQueuedTargets(TargetIndex.A);
            
            yield return clearQueue;
            yield return Toils_JobTransforms.SucceedOnNoTargetInQueue(TargetIndex.A);
            yield return Toils_JobTransforms.ExtractNextTargetFromQueue(TargetIndex.A);
            */
            //LM("Try reserve " + TargetIndex.B.ToString());
            yield return Toils_Reserve.Reserve(TargetIndex.A);

            //LM("GoTo " + TargetIndex.A.ToString());
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch);
            yield return Toils_Haul.StartCarryThing(TargetIndex.A ,subtractNumTakenFromJobCount:true);
            //yield return Toils_Haul.CheckForGetOpportunityDuplicate(clearQueue, TargetIndex.A, TargetIndex.None, takeFromValidStorage: true);
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch);

            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch);
            yield return Toils_General.Wait(10).WithProgressBarToilDelay(TargetIndex.B);
            
            yield return new Toil
            {
                initAction = () =>
                {
                    if (FabricatorComp != null && pawn.carryTracker.CarriedThing != null)
                    {
                        var carried = pawn.carryTracker.CarriedThing;
                        if(FabricatorComp.TryStoreMaterial(carried.def, carried.stackCount))
                        {
                            
                            carried.Destroy();
                        }
                        else
                        {
                            Log.Error(GetReport() + ": Failed to store material " + carried.def.defName + " in fabricator.");
                        }
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            
            
        }

        // Debug log
        protected void LM(object o)
        {
#if DEBUG
            Log.Message("JobDriver_HaulToFabricator | " + o);
#endif
        }
    }
}
