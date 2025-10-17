using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace AutoFabricator
{
    public static class Util_Production
    {
        public static int WorktoMakeSimple(ThingDef def)
        {
            if (def == null || !(def is BuildableDef))
            {
                Log.Error("WorktoMakeSimple: def is null or not BuildableDef");
                return 0;
            }
            foreach(var stat in def.statBases)
            {
                if(stat.stat == StatDefOf.WorkToMake)
                {
                    return (int)stat.value;
                }
            }
            Log.Error("WorktoMakeSimple: def has no WorkToMake stat");
            return 6000;
        }

        public static bool IsAllowedForProduction(ThingDef def)
        {
            if(null == def)
            {
                return false;
            }
            if(!def.MadeFromStuff && def.costList.NullOrEmpty())
            {
                return false;
            }
            if (!def.IsResearchFinished)
            {
                return false;
            }
            if (def.Minifiable) { return false;}
            if(def.recipeMaker?.researchPrerequisite != null)
            {
                if (!def.recipeMaker.researchPrerequisite.IsFinished)
                {
                    return false;
                }
            }
            if (def.recipeMaker?.researchPrerequisites != null)
            {
                foreach(var r in def.recipeMaker.researchPrerequisites)
                {
                    if (!r.IsFinished)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public static int UpgradeQualityLevel(float chance)
        {
            if(chance >= 1f)
            {
                return 1;
            }
            if (Rand.Chance(chance))
            {
                return 1 + UpgradeQualityLevel(chance / 2f);
            }
            else {
                return 0;
            }
        }

        
    }
}
