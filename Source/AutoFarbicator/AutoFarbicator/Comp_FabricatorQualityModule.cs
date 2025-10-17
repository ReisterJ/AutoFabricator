using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace AutoFabricator
{
    public class CompProperties_FabricatorQualityModule : CompProperties
    {
        public float moreWorkNeeded = 1.2f;

        public float upgradeChance = 0.5f;
        public CompProperties_FabricatorQualityModule()
        {
            this.compClass = typeof(Comp_FabricatorQualityModule);
        }
    }

    // 品质模块组件
    public class Comp_FabricatorQualityModule : ThingComp
    {
        public CompProperties_FabricatorQualityModule Props => (CompProperties_FabricatorQualityModule)props;
        public float UpgradeChance => Props.upgradeChance;    
        public float MoreWorkNeeded => Props.moreWorkNeeded;
        public int QualityOffset => 1;

        public override string CompInspectStringExtra()
        {
            string s =  "AutoFabricatorQualityOffset".Translate() + " : " + this.UpgradeChance;
            s += "\n" + "AutoFabricatorProductionWorkMultiplier".Translate() + " : " + this.MoreWorkNeeded;
            return s;
        }
    }
}
