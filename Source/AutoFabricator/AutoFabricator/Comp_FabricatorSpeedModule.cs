using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace AutoFabricator
{
    public class CompProperties_FabricatorSpeedModule : CompProperties
    {
        public float workSpeedMultiplier = 0.8f;

        public CompProperties_FabricatorSpeedModule()
        {
            this.compClass = typeof(Comp_FabricatorSpeedModule);
        }
    }

    // 提速模块组件
    public class Comp_FabricatorSpeedModule : ThingComp
    {
        public float WorkSpeedMultiplier => ((CompProperties_FabricatorSpeedModule)props).workSpeedMultiplier;

        public override string CompInspectStringExtra()
        {
            return "AutoFabricatorSpeedMultiplier".Translate() +" : " + this.WorkSpeedMultiplier;
        }
    }
}
