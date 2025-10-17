using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace AutoFabricator
{
    public abstract class Comp_Fabricator : ThingComp
    {
        protected CompPowerTrader powerComp;
        public ProductionOrder currentOrder;

        public Comp_Cable comp_Cable;
        public bool HasPower => powerComp?.PowerOn ?? false;

        
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            powerComp = parent.GetComp<CompPowerTrader>();
            comp_Cable = this.parent.GetComp<Comp_Cable>();
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            this.powerComp = null;
            this.comp_Cable = null;
            this.currentOrder = null;
            base.PostDeSpawn(map, mode);
        }
        
    }
}
