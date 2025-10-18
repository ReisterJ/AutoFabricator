using AutoFabricator;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Profiling;
using Verse;
using Verse.Noise;

namespace AutoFabricator
{
    public class Window_FabricatorControl : Window
    {
        private Comp_FabricatorController controller;
        private Vector2 scrollPosition;

        private Vector2 orderListScrollPosition;
        private Dictionary<string, bool> expandedCategories = new Dictionary<string, bool>();
        private ThingDef selectedThingDef = null;
        private ThingDef selectedStuffDef = null;
        private int synthesizeCount = 1;

        private float LeftlineHight = 40f;
        private List<ThingCategoryDef> rootCategories => controller.Props.rootCategories;
        private Comp_AutoFabricator chosenFab = null;
        public Window_FabricatorControl(Comp_FabricatorController controller)
        {
            this.controller = controller;
            this.doCloseButton = true;
            this.absorbInputAroundWindow = true;
            this.closeOnClickedOutside = true;
            
        }

        public override void PostOpen()
        {
            base.PostOpen();
            /*
            foreach (var orderkv in controller.OrderAllocationDict)
            {
                Log.Message(orderkv.Key.ProductDef.defName + " allocated to " + (orderkv.Value != null ? orderkv.Value?.TryGetComp<Comp_AutoFabricator>()?. FabricatorID.ToString() : "None") );
            }
            */
        }
        public override Vector2 InitialSize => new Vector2(900f, 600f);

        public override void DoWindowContents(Rect inRect)
        {

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 40f), "Window_Controller".Translate());
            Widgets.DrawLineHorizontal(0f, 42f, inRect.width);

            Text.Font = GameFont.Small;

            if (!controller.IsConnected)
            {
                GUI.color = Color.red;
                Widgets.Label(new Rect(0f, 50f, 200f, 30f), "NotConnected".Translate());
                GUI.color = Color.white;
                return;
            }

            // Left : Thingy thingy thingy tree
            Rect leftRect = new Rect(inRect.x, inRect.y + 50f, 350f, inRect.height - 60f);
            Widgets.DrawMenuSection(leftRect);

            // Right part : orders 
            Rect rightRect = new Rect(leftRect.xMax + 10f, leftRect.y, inRect.width - leftRect.width - 20f, leftRect.height);
            Widgets.DrawMenuSection(rightRect);

            DrawCategoryTree(leftRect);
            DrawSelectedThingPanel(rightRect);

        }

        private int CalculateProductNum()
        {
            int productNum = 0;
            foreach (var cat in rootCategories)
            {
                var things = cat.childThingDefs.Where(td => Util_Production.IsAllowedForProduction(td) && !controller.Props.BlackList.Contains(td) && !td.IsBuildingArtificial && !td.IsFrame && !td.destroyOnDrop).ToList();
                productNum += things.Count;
                productNum += CalculateChildcatProductNum(cat);
            }
            return productNum;
        }
        private int CalculateChildcatProductNum(ThingCategoryDef cat)
        {
            int productNum = 0;
            foreach (var childCat in cat.childCategories)
            {
                var things = childCat.childThingDefs.Where(td => Util_Production.IsAllowedForProduction(td) && !controller.Props.BlackList.Contains(td) && !td.IsBuildingArtificial && !td.IsFrame && !td.destroyOnDrop).ToList();
                productNum += things.Count;
                productNum += CalculateChildcatProductNum(childCat);
            }
            return productNum;

        }


        private void DrawCategoryTree(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            
            Rect viewRect = new Rect(0, 0, rect.width - 16f, CalculateProductNum() * LeftlineHight);
            Widgets.BeginScrollView(rect, ref scrollPosition, viewRect);

            float y = 0f;
            foreach (var cat in rootCategories)
            {
                DrawCategoryNode(cat, 0, ref y, viewRect.width);
            }
            
            Widgets.EndScrollView();
        }

        
        private void DrawCategoryNode(ThingCategoryDef cat, int indent, ref float curY, float width)
        {
            if (controller.Props.BlackListCategory.Contains(cat))
            {
                return;
            }
            float lineHeight = LeftlineHight;
            Rect labelRect = new Rect(indent * 20f, curY, width - indent * 20f, lineHeight);

            bool expanded = expandedCategories.TryGetValue(cat.defName, out bool exp) && exp;
            if (Widgets.ButtonImage(new Rect(labelRect.x, labelRect.y, 24f, 24f), expanded ? TexButton.Collapse : TexButton.Reveal))
            {
                expandedCategories[cat.defName] = !expanded;
            }
            Widgets.Label(new Rect(labelRect.x + 26f, labelRect.y, labelRect.width - 26f, labelRect.height), cat.LabelCap);

            curY += lineHeight;

            if (expanded)
            {
                var things = cat.childThingDefs.Where(td => !controller.Props.BlackList.Contains(td) && !td.IsBuildingArtificial && !td.IsFrame && !td.destroyOnDrop).ToList();

                foreach (var thing in things)
                {
                    if (!Util_Production.IsAllowedForProduction(thing))
                    {
                        continue;
                    }
                    float iconSize = 18f;
                    float padding = 6f;
                    float textWidth = Text.CalcSize(thing.LabelCap).x;
                    float totalWidth = iconSize + padding + textWidth;
                    float startX = (indent + 1) * 20f;
                    Rect thingRect = new Rect((indent + 1) * 20f, curY, width - (indent + 1) * 20f, lineHeight);
                    Rect iconRect = new Rect(startX, curY + (lineHeight - iconSize) / 2, iconSize, iconSize);
                    Widgets.ThingIcon(iconRect, thing);
                    Rect textRect = new Rect(startX+ iconSize + padding, curY + (lineHeight - iconSize) / 2, textWidth, lineHeight);
                    Widgets.Label(textRect, thing.LabelCap);
                    if (Widgets.ButtonInvisible(thingRect))
                    {
                        selectedThingDef = thing;
                        selectedStuffDef = null;
                        synthesizeCount = 1;
                    }
                    if (selectedThingDef == thing)
                    {
                        Widgets.DrawHighlightSelected(thingRect);
                    }
                    curY += lineHeight;
                }
            }
            foreach (var childcat in cat.childCategories)
            {
                DrawCategoryNode(childcat, indent+1, ref curY, width);
            }
        }
       
        private void DrawSelectedThingPanel(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            float y = rect.y + 10f;
            float x = rect.x + 10f;
            float width = rect.width - 20f;

            if (selectedThingDef == null)
            {
                Text.Font = GameFont.Medium;
                Widgets.Label(new Rect(x, y, width, 30f), "OrderList".Translate());
                y += 35f;
                Text.Font = GameFont.Small;

                if (controller.ProductionOrders.Count == 0)
                {
                    return;
                }

                float rowHeight = 40f;
                float listHeight = controller.ProductionOrders.Count * rowHeight;
                Rect outRect = new Rect(rect.x, y, rect.width, rect.height - (y - rect.y));
                Rect viewRect = new Rect(0, 0, outRect.width - 16f, listHeight);

                Widgets.BeginScrollView(outRect, ref orderListScrollPosition, viewRect);
                float curY = 0f;
                foreach (var order in controller.ProductionOrders)
                {
                    //var order = orderkv.Key;
                    if (order == null) continue;
                    string stuffStr = order.StuffDef != null ? $"({order.StuffDef.LabelCap})" : "";
                    
                    string line = $"{order.ProductDef.LabelCap}{stuffStr} x{order.Quantity}" +" "+ "Remaining".Translate() +" : " +order.leftQuantity+" ";
                    if (controller.OrderAllocationDict.ContainsKey(order))
                    {
                        line += controller.OrderAllocationDict[order] != null ? controller.OrderAllocationDict[order]?.TryGetComp<Comp_AutoFabricator>()?.FabricatorID.ToString() : "None";
                    }
                    
                    Rect lineRect = new Rect(10f, curY, viewRect.width - 60f, 40f);
                    Widgets.Label(lineRect, line);

                    Rect delRect = new Rect(viewRect.width - 50f, curY, 70f, 35f);
                    if (Widgets.ButtonText(delRect, "Delete"))
                    {
                        controller.RemoveOrder(order, true);
                        break;
                    }
                    curY += rowHeight;
                }
                Widgets.EndScrollView();
                return;
            }


            //Name
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(x, y, width, 30f), selectedThingDef.LabelCap.Resolve());
            y += 35f;
            Text.Font = GameFont.Small;

            //Material
            Widgets.Label(new Rect(x, y, width, 24f), "Materials".Translate().Resolve());
            y += 26f;
            foreach (var cost in selectedThingDef.costList ?? new List<ThingDefCountClass>())
            {
                Widgets.Label(new Rect(x + 10f, y, width - 10f, 24f), $"{cost.thingDef.LabelCap} x{cost.count}");
                y += 24f;
            }

            //StuffCategory
            //bool stuffchosen = false;
            if (selectedThingDef.stuffCategories != null && selectedThingDef.stuffCategories.Count > 0)
            {
                //bool needsStuffCateButNotChosen = true;
                TaggedString stuffLabel = ((selectedStuffDef != null) ? selectedStuffDef.LabelCap : "ChooseStuff".Translate());
                if (Widgets.ButtonText(new Rect(x, y, 160f, 28f), stuffLabel))
                {
                    List<FloatMenuOption> options = new List<FloatMenuOption>();
                    foreach (var stuff in DefDatabase<ThingDef>.AllDefs.Where(td =>
                        td.IsStuff && td.stuffProps != null && selectedThingDef.stuffCategories.Any(cat => td.stuffProps.categories.Contains(cat))))
                    {
                        options.Add(new FloatMenuOption(stuff.LabelCap, () => { 
                            selectedStuffDef = stuff;
                            //needsStuffCateButNotChosen = false; 
                            //stuffchosen = true; 
                        }));
                    }
                    Find.WindowStack.Add(new FloatMenu(options));
                }
               
                y += 34f;
            }

            //
            Widgets.Label(new Rect(x, y, 80f, 28f), "Quantity".Translate());
            string buffer = synthesizeCount.ToString();
            Widgets.TextFieldNumeric(new Rect(x + 80f, y, 60f, 28f), ref synthesizeCount, ref buffer, 1, 999);
            y += 34f;


            //choose fabricator
            
            TaggedString FabricatorLabel = ((chosenFab != null) ? chosenFab.FabricatorID.Translate() : "ChooseFabricator".Translate());
            if (Widgets.ButtonText(new Rect(x, y, 120f, 30f), FabricatorLabel))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (var fab in controller.AutoFabricators.Where(f => f.currentOrder == null))
                {
                    string id = fab.FabricatorID.Translate();
                    options.Add(new FloatMenuOption(id, () =>
                    {
                        chosenFab = fab;
                    }));
                }
                if (options.Count == 0)
                    options.Add(new FloatMenuOption("FabricatorsAllBusy".Translate(), null));
                Find.WindowStack.Add(new FloatMenu(options));
            }
            

            if (Widgets.ButtonText(new Rect(x + 40f, rect.yMax - 80f, 120f, 32f), "Confirm"))
            {
                if (synthesizeCount > 0)
                {
                    var order = new ProductionOrder
                    {
                        ProductDef = selectedThingDef,
                        Quantity = synthesizeCount,
                        leftQuantity = synthesizeCount,
                        stackCountPerBill = 1,
                        TotalWorkNeeded = selectedThingDef.GetStatValueAbstract(StatDefOf.WorkToMake)
                    };


                    if (selectedThingDef.MadeFromStuff && selectedStuffDef == null)
                    {
                        selectedStuffDef = GenStuff.DefaultStuffFor(selectedThingDef);
                    }
                    if (selectedStuffDef != null)
                    {
                        order.StuffDef = selectedStuffDef;
                    }
                    
                    controller.AddOrder(order,chosenFab);
                    Messages.Message($"BillOrderAdded".Translate()+":" + $"{selectedThingDef.LabelCap} x{synthesizeCount}", MessageTypeDefOf.TaskCompletion, false);
                    Close();
                }
            }
        }
    }

}
