using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace AutoFabricator
{
    public class ProductionOrder : IExposable, ILoadReferenceable
    {
        //public string ProductDefName;
        public int Quantity; // 总生产次数
        public float TotalWorkNeeded;
        public int worktomake;

        public int stackCountPerBill = 1; // 每次生产的数量
        public int leftQuantity; // 剩余生产次数
        public bool StuffCategoryAllowed => ProductDef.stuffCategories?.Count > 0;
        public ThingDef ProductDef = null;

        public ThingDef StuffDef = null;

        //special
        public bool isSpecialOption = false;
        public int productionCountPerBill = 1; //每次生产数量，xml指定
        public int specialBillWorkToMake = 1000;
        public List<ThingDefCountClass> specialCostList = new List<ThingDefCountClass>();
        public List<StuffCategoryDef> specialStuffCategories = new List<StuffCategoryDef>();
        public int stuffCount = 20;

        public bool Allocated = false;

        public Comp_AutoFabricator AllocatedFabricator;

        protected long currentIndex;

        public long CurrentIndex => currentIndex;

        protected static long orderIndex = 0;



        public ProductionOrder()
        {
            orderIndex = Find.TickManager.TicksAbs;
            SetCurrentIndex();
        }
        public void SetCurrentIndex()
        {
            currentIndex = orderIndex;
        }
        public void IndexCopy(ProductionOrder pd)
        {
            this.currentIndex = pd.CurrentIndex;
        }

        public void ExposeData()
        {
            Scribe_Defs.Look(ref ProductDef, "ProductDef");
            Scribe_Values.Look(ref Quantity, "quantity");
            Scribe_Values.Look(ref orderIndex, "orderIndex");
            Scribe_Values.Look(ref leftQuantity, "leftQuantity");
            Scribe_Defs.Look(ref StuffDef, "StuffDef");
            Scribe_Values.Look(ref currentIndex, "currentIndex");
            Scribe_Values.Look(ref isSpecialOption, "isSpecialOption", false);
            Scribe_Values.Look(ref productionCountPerBill, "productionCountPerBill", 1);
            Scribe_Values.Look(ref specialBillWorkToMake, "specialBillWorkToMake", 1000);
            Scribe_Collections.Look(ref specialCostList, "specialCostList", LookMode.Deep);
            Scribe_Collections.Look(ref specialStuffCategories, "specialStuffCategories", LookMode.Def);
            Scribe_Values.Look(ref stuffCount, "stuffCount", 20);
        }

        public ProductionOrder DeepCopy()
        {
            ProductionOrder result = new ProductionOrder();
            result.ProductDef = this.ProductDef;
            result.Quantity = this.Quantity;
            result.leftQuantity = this.leftQuantity;
            result.worktomake = this.worktomake;
            result.TotalWorkNeeded = this.TotalWorkNeeded;
            result.stackCountPerBill = this.stackCountPerBill;
            result.StuffDef = this.StuffDef;
            result.IndexCopy(this);
            return result;
        }

        public string GetUniqueLoadID()
        {
            return "ProductionOrder_" + ProductDef.defName + "_" + this.GetHashCode();
        }

    }
    public class SpecialProductionOption
    {
        public ThingDef ProductDef;
        public int productionCountPerBill = 1; //每次生产数量，xml指定
        public int workToMake = 1000;  
        public List<ThingDefCountClass> specialCostList = new List<ThingDefCountClass>();
        public List<StuffCategoryDef> specialStuffCategories = new List<StuffCategoryDef>();
        public int stuffCount = 20;

        protected long currentIndex;

        public string customLabel;
        public long CurrentIndex => currentIndex;

        protected static long Index = 0;
        public SpecialProductionOption()
        {
            Index++;
            SetCurrentIndex();
        }
        public void SetCurrentIndex()
        {
            currentIndex = Index;
        }
    }

 
}
