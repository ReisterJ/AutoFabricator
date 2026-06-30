using AutoFabricator;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AutoFabricator
{
    public class Comp_AutoFabricator : Comp_Fabricator, INotifyHauledTo , IThingHolder
    {
        protected CompAffectedByFacilities facilitiesComp;
        protected Comp_FabricatorController connectedController;

        protected ThingWithComps connectedControllerBuilding;
        protected List<ThingWithComps> connectedControllers = new List<ThingWithComps>();

        protected List<ProductionOrder> productionOrders = new List<ProductionOrder>();

        protected float workProgress;
        public bool isProducing = false;

        public List<ThingDefCountClass> pendingMaterials = new List<ThingDefCountClass>();

        protected Dictionary<ThingDef, int> storedMaterials = new Dictionary<ThingDef, int>();

        public float BaseProductionSpeed => ((CompProperties_AutoFabricator)props).baseProductionSpeed;

        private CompPowerTrader PowerTraderComp => this.parent.TryGetComp<CompPowerTrader>();
        

        public int WorkSpeedPercent => Mathf.RoundToInt(GetWorkSpeed() / BaseProductionSpeed * 100);

        protected float WorkProgessPercent => currentOrder == null ? 0f : Mathf.Clamp01(workProgress / GetTotalWork()) * 100f;

        public bool pending = false;

        private ThingOwner innerContainer ;
        
        public static int fabricatorIndex = 0;

        public int FabricatorCurrentIndex => fabricatorCurrentIndex;
        private int fabricatorCurrentIndex = 0;


        public string FabricatorID => "Fabricator".Translate() +" "+ Math.Abs( this.GetHashCode() ) % 10000;
        public Comp_AutoFabricator()
        {
            //fabricatorIndex++;
            
            innerContainer = new ThingOwner<Thing>(this);
        }
        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return innerContainer;
        }
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            /*
            if(fabricatorCurrentIndex == 0)
            {
                fabricatorCurrentIndex = fabricatorIndex;
            }
            */
            powerComp = parent.GetComp<CompPowerTrader>();
            facilitiesComp = parent.GetComp<CompAffectedByFacilities>();

            /*
            if(connectedControllerBuilding != null)
            {
                connectedController = connectedControllerBuilding.TryGetComp<Comp_FabricatorController>();
            }
            */
        }

        public override void CompTick()
        {
            base.CompTick();
            if(!this.HasPower)
            {
                //AbondonCurrentOrder(false);
            }
            
        }


        public override void CompTickInterval(int delta)
        {
            if (!HasPower || currentOrder == null) return;

            if (!isProducing)
            {
                if (CheckIngredientsFullFillment())
                {
                    isProducing = true;
                    pending = false;
                    PowerTraderComp.PowerOutput = (0f - this.powerComp.Props.PowerConsumption);
                }
                return;
            }

            Production(delta);
        }

        public void ConnectToController(Comp_FabricatorController controller)
        {
            connectedController = controller;
            connectedControllerBuilding = controller.parent;
            connectedControllers.Add(controller.parent);
        }

        public void DisconnectFromController(bool broken = false)
        {
            if (broken)
            {
                connectedController = null;
                connectedControllerBuilding = null;
                this.connectedControllers.Clear();
                return;
            }

            if (connectedController?.parent != null)
            {
                if (connectedControllers?.Contains(connectedController.parent) == true)
                {
                   connectedControllers.Remove(connectedController.parent);
                }
            }
            connectedController = null;
            connectedControllerBuilding = null;
            

            if(connectedControllers?.Count > 0)
            {
                connectedControllerBuilding = connectedControllers[0];
                connectedController = connectedControllerBuilding.TryGetComp<Comp_FabricatorController>();
            }
        }

        public bool IsConnected => connectedController != null;

        //Availablity check
        public bool StateCheck()
        {
            if (!this.HasPower)
            {
                return false;
            }
            if (this.parent.TryGetComp<CompBreakdownable>()?.BrokenDown == true)
            {
                return false;
            }
            return true;
        }


        //HAUL AND STORAGE
        public bool NeedHaulWork()
        {
            if(currentOrder != null && pending && this.StateCheck())
            {
                return true;
            }
            return false;
        }

        public void Notify_HauledTo(Pawn hauler, Thing thing, int count)
        {
            TryStoreMaterial(thing.def, count);
            //TryStoreMaterial(thing, count);
        }

        public bool TryStoreMaterial(ThingDef def, int count)
        {
            if (!storedMaterials.ContainsKey(def))
            {
                Log.Error("Trying to store material not in pending list: " + def.defName);
                return false;
            }
            Log.Message("try store def " + def + " name :" + def.defName + " count : " + count);

            storedMaterials[def] += count;
            
            Thing thing = ThingMaker.MakeThing(def);
            thing.stackCount = count;
            innerContainer.TryAdd(thing, true);
            
            return true;
        }

        public bool TryStoreMaterial(Thing thing , int stackcount)
        {
            thing.stackCount = stackcount;
            return (innerContainer.TryAdd(thing, true));
        }

        public int GetStoredMaterial(ThingDef def)
        {
            return storedMaterials.TryGetValue(def, out int val) ? val : 0;
        }
        

        // PRODUCTION START
        public void ReceiveOrder(ProductionOrder order)
        {
            if (currentOrder != null) return;
            //Log.Message("Received order: " + order.ProductDef.defName + " x" + order.leftQuantity);
            currentOrder = order;
            workProgress = 0f;
            isProducing = false;
            
            pendingMaterials.Clear();
            ReadyMaterialList(order);
            pending= true;
        }

        protected void ReadyMaterialList(ProductionOrder order)
        {
            if (order.ProductDef.costList != null)
            {
                foreach (var cost in order.ProductDef.costList)
                {
                    pendingMaterials.Add(new ThingDefCountClass(cost.thingDef, cost.count));
                }
            }
            //for stuffcategories
            if (order.StuffDef != null && order.ProductDef.MadeFromStuff)
            {
                int stuffCount = order.ProductDef.costStuffCount;
                if (stuffCount > 0)
                {
                    pendingMaterials.Add(new ThingDefCountClass(order.StuffDef, stuffCount));
                }
            }
            storedMaterials.Clear();
            foreach(ThingDefCountClass thingDefCountClass in pendingMaterials)
            {
                storedMaterials.Add(thingDefCountClass.thingDef, 0);
            }
        }

        

        protected virtual void Production(int interval)
        {
            if (currentOrder == null) return;
            
            float workSpeed = GetWorkSpeed();
            workProgress += workSpeed * interval;
            ConsumeIngredients();
            if (workProgress >= GetTotalWork())
            {
                CompleteProduction();
            }
        }

        

        public bool CheckIngredientsFullFillment()
        {
            if(currentOrder == null) return false;
            if(pending == false)
            {
                return true;
            }
            foreach (ThingDefCountClass thingDefCountClass in pendingMaterials)
            {
                storedMaterials.TryGetValue(thingDefCountClass.thingDef, out int storedCount);
                if (storedCount != thingDefCountClass.count)
                {
                    return false;
                }
            }
            return true;
        }

        protected virtual void ConsumeIngredients(int interval = 0)
        {
            float left = 1f - Mathf.Clamp01(workProgress / GetTotalWork());
            foreach (var kv in pendingMaterials)
            {
                storedMaterials[kv.thingDef] = Mathf.CeilToInt(kv.count * left);
                
            }

           
        }

        protected virtual void CompleteProduction()
        {
            if (currentOrder == null) return;

            ThingDef productDef = currentOrder.ProductDef;
            if (productDef != null)
            {
                TryDoSpawn(productDef, currentOrder.stackCountPerBill , currentOrder.StuffDef);
            }
            else
            {
                Log.Error("Has null productDef in currentOrder");
            }
            innerContainer.ClearAndDestroyContents();

            PowerTraderComp.PowerOutput = (0f - this.powerComp.Props.PowerConsumption);
            workProgress = 0f;
            if (currentOrder.leftQuantity  <= 1)
            {
                isProducing = false;
                connectedController?.RemoveOrder(currentOrder.CurrentIndex);
                currentOrder = null;
            }
            else
            {
                currentOrder.leftQuantity -= 1;
                pendingMaterials.Clear();
                isProducing = false;
                ReadyMaterialList(currentOrder);
                pending = true;
            }
            
        }

        //offset
        public virtual float GetWorkSpeed()
        {
            float speed = BaseProductionSpeed;

            if (facilitiesComp != null)
            {
                foreach (var facility in facilitiesComp.LinkedFacilitiesListForReading)
                {
                    var speedModule = facility.TryGetComp<Comp_FabricatorSpeedModule>();
                    if (speedModule != null)
                    {
                        speed *= speedModule.WorkSpeedMultiplier;
                    }
                }
            }

            return speed;
        }
        protected int GetQualityOffset()
        {
            int offset = 0;
            float chance = 0f;
            if (facilitiesComp != null)
            {
                foreach (var facility in facilitiesComp.LinkedFacilitiesListForReading)
                {
                    var qualityModule = facility.TryGetComp<Comp_FabricatorQualityModule>();
                    if (qualityModule != null)
                    {
                        chance += qualityModule.UpgradeChance;
                    }
                }
                while (chance > 1f)
                {
                    offset += 1;
                    chance /= 2f;
                }
                offset += Util_Production.UpgradeQualityLevel(chance);
            }

            return offset;
        }

        public virtual float GetTotalWork()
        {
            if (currentOrder == null)
            {
                return 0f;
            }
            bool workMultiplierApplied = Util_Production.HasQualityComp(currentOrder.ProductDef);
            float work = Util_Production.WorktoMakeSimple(currentOrder.ProductDef);
            if (!workMultiplierApplied) return work;
            if (facilitiesComp != null)
            {
                foreach (var facility in facilitiesComp.LinkedFacilitiesListForReading)
                {
                    var qualityModule = facility.TryGetComp<Comp_FabricatorQualityModule>();
                    if (qualityModule != null )
                    {
                        work *= qualityModule.MoreWorkNeeded;
                    }
                }
            }
            return work;
        }

        public void EjectAllMaterials()
        {
            innerContainer.TryDropAll(parent.InteractionCell, parent.Map, ThingPlaceMode.Near);
            storedMaterials.Clear();
            innerContainer.ClearAndDestroyContents();
        }
        public void AbondonCurrentOrder(bool forced = false)
        {
            if (currentOrder == null) return;
            if(!forced) EjectAllMaterials();
            currentOrder = null;
            workProgress = 0f;
            isProducing = false;
            pendingMaterials.Clear();
            storedMaterials.Clear();
            pending = false;
        }


        //spawn 

        public bool TryDoSpawn(ThingDef productDef,int quantity,ThingDef stuffDef = null)
        {
            if (!parent.Spawned)
            {
                return false;
            }
            if (TryFindSpawnCell(parent, productDef, quantity, out var result))
            {
                Thing thing = ThingMaker.MakeThing(productDef, stuffDef);
                
                /*
                if (productDef.MadeFromStuff)
                {
                    ThingDef stuff = GenStuff.DefaultStuffFor(productDef);
                    thing = ThingMaker.MakeThing(productDef, stuff);
                }
                else if (productDef.MadeFromStuff && stuffDef != null)
                {
                    Log.Message("Using defined stuff for " + productDef.defName + ": " + stuffDef.defName);
                    thing = ThingMaker.MakeThing(productDef, stuffDef);
                }
                else
                {
                    Log.Message("No stuff needed for " + productDef.defName);
                    thing = ThingMaker.MakeThing(productDef);
                }
                */
                if (thing.TryGetComp<CompQuality>() != null)
                {
                    int qualityOffset = GetQualityOffset();
                    QualityCategory quality = (QualityCategory)Mathf.Clamp((int)QualityCategory.Normal + qualityOffset, (int)QualityCategory.Awful, (int)QualityCategory.Legendary);
                    thing.TryGetComp<CompQuality>().SetQuality(quality, null);
                }

                thing.stackCount = quantity;
                if(productDef.CanHaveFaction)
                {
                    thing.SetFaction(parent.Faction);
                }
                
                if (!GenPlace.TryPlaceThing(thing, result, parent.Map, ThingPlaceMode.Direct, out var lastResultingThing, null, null, null))
                {
                    return false;
                }
                return true;
            }
            return false;
        }

        public static bool TryFindSpawnCell(Thing parent, ThingDef thingToSpawn, int spawnCount, out IntVec3 result)
        {
            foreach (IntVec3 item in GenAdj.CellsAdjacent8Way(parent).InRandomOrder())
            {
                if (!item.Walkable(parent.Map))
                {
                    continue;
                }
                Building edifice = item.GetEdifice(parent.Map);
                if ((edifice != null && (thingToSpawn.IsEdifice() || edifice is IHaulDestination || edifice is Building_Door )) || (parent.def.passability != Traversability.Impassable && !GenSight.LineOfSight(parent.Position, item, parent.Map)))
                {
                    continue;
                }
                bool flag = false;
                List<Thing> thingList = item.GetThingList(parent.Map);
                for (int i = 0; i < thingList.Count; i++)
                {
                    Thing thing = thingList[i];
                    if (thing.def.category == ThingCategory.Item && (thing.def != thingToSpawn || thing.stackCount > thingToSpawn.stackLimit - spawnCount))
                    {
                        flag = true;
                        break;
                    }
                }
                if (!flag)
                {
                    result = item;
                    return true;
                }
            }
            result = IntVec3.Invalid;
            return false;
        }

        public bool ForcedStopCurrentOrder(bool forced = false)
        {
            if (currentOrder == null) return false;
            //    this.connectedController?.FabricatorAbondonOrder(currentOrder, this);
            this.AbondonCurrentOrder(forced);
            return true;
        }
        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            Down();
            base.PostDeSpawn(map, mode);
        }

        protected void Down()
        {
            this.ForcedStopCurrentOrder(true);
            this.DisconnectFromController(true);
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            if (currentOrder != null)
            {
                
                storedMaterials.Clear();
                innerContainer.ClearAndDestroyContents();
                currentOrder = null;
                workProgress = 0f;
                isProducing = false;
                pendingMaterials.Clear();
                storedMaterials.Clear();
                pending = false;
            }
            this.DisconnectFromController(true);
            base.PostDestroy(mode, previousMap);
        }
        public override string CompInspectStringExtra()
        {
            string s = this.FabricatorID;
            if (IsConnected)
            {
                s += "\n" + "ConnectedToController".Translate() + ": " + connectedController.GetHashCode();
                s += "\n" + "WorkSpeed".Translate() + ": " + WorkSpeedPercent + " %";
            }
            else
            {
                s += "\n" + "NotConnectedToController".Translate();
            }
            if (isProducing)
            {
                s+= "\n" + "Producing".Translate() + ": " + currentOrder.ProductDef.label + " " + WorkProgessPercent + " %";
            }
            if(this.currentOrder != null)
            {
                foreach(var kv in storedMaterials)
                {
                    s += "\n" + kv.Key.label + " " + kv.Value + " / " + pendingMaterials.Find(x => x.thingDef == kv.Key).count;
                }
            }
            return s;
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_References.Look(ref currentOrder, "currentOrder");
            Scribe_Values.Look(ref workProgress, "workProgress");
            Scribe_Values.Look(ref isProducing, "isProducing");
            Scribe_Collections.Look(ref pendingMaterials, "pendingMaterials", LookMode.Deep);
            Scribe_Collections.Look(ref storedMaterials, "storedMaterials", LookMode.Def, LookMode.Value);
            Scribe_Values.Look(ref fabricatorCurrentIndex, "fabricatorCurrentIndex");
            Scribe_Values.Look(ref pending, "pending");
            //Scribe_Collections.Look(ref connectedControllers, "connectedControllers", LookMode.Deep);
            //Scribe_Collections.Look(ref productionOrders, "productionOrders", LookMode.Deep);
            //Scribe_References.Look(ref connectedControllerBuilding, "connectedControllerBuilding");
        }
        protected void ResetContainer(bool forcedClear = false)
        {
            if(this.storedMaterials != null)
            {

            }
        }
    }

    public class CompProperties_AutoFabricator : CompProperties
    {
        public float baseProductionSpeed = 3.0f;

        public int WorkingPowerConsumption = 200;
        
        public CompProperties_AutoFabricator()
        {
            this.compClass = typeof(Comp_AutoFabricator);
        }
    }
}
