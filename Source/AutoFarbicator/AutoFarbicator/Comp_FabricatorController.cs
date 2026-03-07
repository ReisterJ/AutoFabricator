using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace AutoFabricator
{
    // 合成控制平台组件
    public class Comp_FabricatorController : Comp_Fabricator
    {
        public static int controllerIDCounter = 0;

        protected Comp_AutoFabricator connectedFabricator;
        
        public bool IsConnected => connectedFabricator != null;
        public Comp_AutoFabricator ConnectedFabricator => connectedFabricator;

        public List<Comp_AutoFabricator> AutoFabricators = new List<Comp_AutoFabricator>();

        public List<ProductionOrder> ProductionOrders = new List<ProductionOrder>();
        public CompProperties_FabricatorController Props => (CompProperties_FabricatorController)props;

        protected bool AutoAllocate = true;

        public Dictionary<ProductionOrder,ThingWithComps> OrderAllocationDict => Order_Allocation;
        protected Dictionary<ProductionOrder, ThingWithComps> Order_Allocation = new Dictionary<ProductionOrder, ThingWithComps>();


        //deprecated
        private List<ProductionOrder> Order_Allocation_Keys_For_Save = new List<ProductionOrder>();
        private List<ThingWithComps> Order_Allocation_Values_For_Save = new List<ThingWithComps>();

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            //Log.Message("FabricatorController spawned. HashCode: " + this.GetHashCode());
            base.PostSpawnSetup(respawningAfterLoad);
            Order_Allocation.Clear();
            AutoFabricators.Clear();
            UpdateConnection();
            foreach(var pd in ProductionOrders)
            {
                Order_Allocation.Add(pd, null);
                //Log.Message("Re-adding order " + pd.ProductDef.label + " to controller " + this.GetHashCode());
            }
            
            SuspendedOrderReassign();
            OrderAssignment();
            if (Props.rootCategories.Count < 1)
            {
                Log.Error("FabricatorController must have at least one root category defined in its properties.".Translate());
            }
            powerComp = parent.GetComp<CompPowerTrader>();
            
            
            //connectedFabricator = AutoFabricators.FirstOrDefault();
            
            controllerIDCounter++;
        }

        

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            Disconnect();
            this.connectedFabricator = null;
            this.AutoFabricators.Clear();
            this.ProductionOrders.Clear();
            this.Order_Allocation.Clear();
            
            base.PostDeSpawn(map, mode);
        }

        public override void CompTickInterval(int delta)
        {
            base.CompTickInterval(delta);
            if (this.comp_Cable == null) return;
            if (!this.comp_Cable.previouslyChecked)
            {
                this.comp_Cable.BFSCheckConnect(this.comp_Cable,this.GetHashCode());
            }
            UpdateConnection();

            //Log.Message(AutoFabricators.Count.ToString() + " connected to controller " + this.GetHashCode());

            SuspendedOrderReassign();
            OrderAssignment();
            
        }
        public void OrderAssignment()
        {
            if (ProductionOrders.Count > 0)
            {
                foreach (var order in ProductionOrders)
                {
                    if (!Order_Allocation.ContainsKey(order))
                    {
                        AllocateOrder(order);
                    }
                    else
                    {
                        if (Order_Allocation[order] == null)
                        {
                            AllocateOrder(order);
                        }
                    }
                }
            }
        }
        protected void UpdateConnection()
        {
            if (!HasPower)
            {
                Disconnect();
                return;
            }
            var fabricators = comp_Cable?.Fabricators;
            if (fabricators == null) return;
            List<Comp_AutoFabricator> temp = new List<Comp_AutoFabricator>();
            temp.Clear();
            for(int i = 0; i < fabricators.Count; i++)
            {
                if (fabricators[i] == null) continue;
                connectedFabricator = fabricators[i].GetComp<Comp_AutoFabricator>();
                if (connectedFabricator == null) continue;
                if (connectedFabricator.StateCheck())
                {
                    temp.Add(connectedFabricator);

                    if (!AutoFabricators.Contains(connectedFabricator))
                    {
                        AutoFabricators.Add(connectedFabricator);
                        connectedFabricator.ConnectToController(this);
                    }
                }
            }
            for(int i = 0;i< AutoFabricators.Count; i++)
            {
                if (AutoFabricators[i] == null)
                {
                    AutoFabricators.RemoveAt(i);
                    i--;
                    continue;
                }
                if (!temp.Contains(AutoFabricators[i]))
                {
                    fabricators.Remove(AutoFabricators[i]?.parent);
                    AutoFabricators[i].DisconnectFromController();
                    AutoFabricators.RemoveAt(i);
                    i--;
                }
            }

            //remove not functional machine
            
        }

        public void Disconnect()
        {
            foreach(var fab in AutoFabricators)
            {
                fab.DisconnectFromController();
            }
            connectedFabricator = null;
            AutoFabricators.Clear();
        }
        

        public virtual void AddOrder(ProductionOrder pd, Comp_AutoFabricator chosen = null)
        {
            ProductionOrders.Add(pd);
            if(chosen != null)
            {
                //Log.Message(chosen.parent.Label);
                if (AutoFabricators.Contains(chosen))
                {
                    //Log.Message(pd.ProductDef.label + " assigned to " + chosen.parent.LabelCap);
                    chosen.ReceiveOrder(pd);
                    Order_Allocation.Add(pd, chosen.parent);
                }
            }
            
        }

        public virtual void RemoveOrder(ProductionOrder pd, bool ControllerForced = false)
        {
            if (pd == null) return;
            if (ControllerForced)
            {
                Comp_AutoFabricator fab = null;
                if (Order_Allocation.ContainsKey(pd))
                {
                    fab = Order_Allocation[pd]?.GetComp<Comp_AutoFabricator>();
                }
                // Fallback: allocation may be null after load before BFS reconnects it
                if (fab == null)
                {
                    foreach (var autofab in AutoFabricators)
                    {
                        if (autofab.currentOrder?.CurrentIndex == pd.CurrentIndex)
                        {
                            fab = autofab;
                            break;
                        }
                    }
                }
                fab?.AbondonCurrentOrder();
            }
            Order_Allocation.Remove(pd);
            ProductionOrders.Remove(pd);
        }

        public void RemoveOrder(long index)
        {
            ProductionOrder pd = ProductionOrders.FirstOrDefault(x => x.CurrentIndex == index);
            if (pd != null)
            {
                RemoveOrder(pd);
            }
        }



        public virtual void FabricatorAbondonOrder(ProductionOrder pd, Comp_AutoFabricator autoFabricator)
        {
            //Order_Allocation.Remove(pd);
        }



        
        public virtual void AllocateOrder(ProductionOrder pd)
        {
            if (!IsConnected) return;
            // Default : allocate to every available fabricator.Every fabricator will produce one item.
            // if there are more fabricators than the quantity of first order.Next order will be allocated to remaining fabricators.
            DefaultOrderAllocation(pd);
        }

        protected void DefaultOrderAllocation(ProductionOrder pd)
        {
            foreach (var a in AutoFabricators)
            {
                if(a.currentOrder == null)
                {
                    if(!Order_Allocation.ContainsKey(pd))
                        Order_Allocation.Add(pd, a.parent);
                    else
                    {
                        Order_Allocation[pd] = a.parent;  
                    }
                    a.ReceiveOrder(pd);
                    return;
                }
            }
            
        }

        protected void SuspendedOrderReassign()
        {
            if(ProductionOrders.Count <= 0 || AutoFabricators.Count <= 0)
            {
                return;
            }
            foreach (ProductionOrder pd in ProductionOrders)
            {
                if (Order_Allocation.ContainsKey(pd))
                {
                    if (Order_Allocation[pd] == null || !Order_Allocation[pd].Spawned)
                    {
                        bool onefabworking = false;
                        
                        foreach (var autofab in AutoFabricators)
                        {
                            if (autofab.currentOrder?.CurrentIndex == pd.CurrentIndex)
                            {
                                // Re-link the fabricator's currentOrder to the controller's canonical instance
                                // so that mutations like leftQuantity-- are reflected in the UI.
                                autofab.currentOrder = pd;
                                Order_Allocation[pd] = autofab.parent;
                                onefabworking = true;
                                break;
                            }
                        }
                        if (!onefabworking && AutoFabricators.Count > 0)
                        {
                            // Only remove if fabricators were discovered but none is working on this order.
                            // If AutoFabricators is empty, BFS hasn't run yet — keep the null entry for later reconnection.
                            Order_Allocation.Remove(pd);
                        }
                    }
                }
            }
        }

        public override string CompInspectStringExtra()
        {
            string s = "Controller".Translate().Resolve() + " HashCode: " + this.GetHashCode();
            if (IsConnected)
            {
                s += "\n" + "Connected".Translate().Resolve();
            }
            else
            {
                s += "\n" + "NotConnected".Translate().Resolve();
            }
            return s;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (var gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }
            /*
            yield return new Command_Action
            {
                defaultLabel = AutoAllocate?"Auto" : "Choose",
                defaultDesc = "AllocationSwitchDesc",
                icon = ContentFinder<Texture2D>.Get("Things/Building/Power/PowerSwitch"),
                action = () =>
                {
                    AutoAllocate = !AutoAllocate;
                }
            };
            */
            if (IsConnected && HasPower)
            {
                yield return new Command_Action
                {
                    defaultLabel = "ControlPad",
                    defaultDesc = "ControlPadDesc",
                    icon = ContentFinder<Texture2D>.Get("Things/Building/Power/PowerSwitch"),
                    action = () => Find.WindowStack.Add(new Window_FabricatorControl(this))
                };
            }

        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Collections.Look(ref ProductionOrders, "ProductionOrders", LookMode.Deep);
        }

    }
    public class CompProperties_FabricatorController : CompProperties
    {
        public CompProperties_FabricatorController()
        {
            this.compClass = typeof(Comp_FabricatorController);
        }
        public List<ThingCategoryDef> rootCategories = new List<ThingCategoryDef>();

        public List<ThingCategoryDef> BlackListCategory = new List<ThingCategoryDef>();

        // WhiteList is deprecated. Use specialProductions instead.
        public List<ThingDef> WhiteList = new List<ThingDef>();

        public List<ThingDef> BlackList = new List<ThingDef>();

        public List<SpecialProductionOption> specialProductions = new List<SpecialProductionOption>();
    }


}
