using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace AutoFabricator
{
    public class Window_FabricatorControlExtended : Window
    {
        private Comp_FabricatorController controller;
        private Vector2 specialScrollPosition = Vector2.zero;
        protected ThingDef selectedThingDef;
        protected ThingDef selectedStuffDef;
        protected int synthesizeCount = 1;
        protected Comp_AutoFabricator chosenFabricator = null;
        private Vector2 orderListScrollPosition;

        private SpecialProductionOption selectedSpecialProductionOption;
        public Window_FabricatorControlExtended(Comp_FabricatorController controller)
        {
            this.controller = controller;
            this.forcePause = true;
            this.doCloseX = true;
            this.draggable = true;
            this.closeOnClickedOutside = true;
        }

        public override Vector2 InitialSize => new Vector2(520f, 520f);

        public override void DoWindowContents(Rect inRect)
        {
            float leftWidth = 220f;
            Rect leftRect = new Rect(inRect.x, inRect.y, leftWidth, inRect.height);
            Rect rightRect = new Rect(inRect.x + leftWidth + 10f, inRect.y, inRect.width - leftWidth - 10f, inRect.height);

            DrawSpecialProductionList(leftRect);
            DrawSelectedThingPanel(rightRect);
        }

        // 左侧仅显示specialProductions
        protected virtual void DrawSpecialProductionList(Rect rect)
        {
            var specials = controller.Props.specialProductions;
            float rowHeight = 28f;
            float listHeight = specials.Count * rowHeight;
            Rect outRect = new Rect(rect.x, rect.y, rect.width, rect.height);
            Rect viewRect = new Rect(0, 0, outRect.width - 16f, listHeight);

            Widgets.BeginScrollView(outRect, ref specialScrollPosition, viewRect);
            float curY = 0f;
            for (int i = 0; i < specials.Count; i++)
            {
                var sp = specials[i];
                Rect lineRect = new Rect(10f, curY, viewRect.width - 20f, rowHeight - 4f);
                if (Widgets.ButtonInvisible(lineRect))
                {
                    selectedThingDef = sp.ProductDef;
                    selectedStuffDef = null;
                    synthesizeCount = 1;
                    selectedSpecialProductionOption = sp;
                }
                Widgets.Label(lineRect, sp.ProductDef.LabelCap);
                if (selectedThingDef == sp.ProductDef)
                {
                    Widgets.DrawHighlightSelected(lineRect);
                }
                curY += rowHeight;
            }
            Widgets.EndScrollView();
        }

        
        protected virtual void DrawSelectedThingPanel(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            float y = rect.y + 10f;
            float x = rect.x + 10f;
            float width = rect.width - 20f;
            if (selectedThingDef == null)
            {
                Text.Font = GameFont.Medium;
                Widgets.Label(new Rect(rect.x + 10f, rect.y + 10f, rect.width - 20f, 30f), "OrderList".Translate());
                y += 35f;
                Text.Font = GameFont.Small;

                if (controller.ProductionOrders.Count == 0)
                {
                    return;
                }

                float rowHeight = 26f;
                float listHeight = controller.ProductionOrders.Count * rowHeight;
                Rect outRect = new Rect(rect.x, y, rect.width, rect.height - (y - rect.y));
                Rect viewRect = new Rect(0, 0, outRect.width - 16f, listHeight);

                Widgets.BeginScrollView(outRect, ref orderListScrollPosition, viewRect);
                float curY = 0f;
                foreach (var orderkv in controller.OrderAllocationDict)
                {
                    var order = orderkv.Key;
                    //if (order == null) continue;
                    string stuffStr = order.StuffDef != null ? $"({order.StuffDef.LabelCap})" : "";
                    string line = $"{order.ProductDef.LabelCap}{stuffStr} x{order.Quantity}" + " " + "Remaining" + " : " + order.leftQuantity;
                    line += controller.OrderAllocationDict[order] != null ? controller.OrderAllocationDict[order]?.TryGetComp<Comp_AutoFabricator>()?.FabricatorID.ToString() : "None";
                    Rect lineRect = new Rect(10f, curY, viewRect.width - 60f, 40f);
                    Widgets.Label(lineRect, line);

                    Rect delRect = new Rect(viewRect.width - 50f, curY, 80f, 35f);
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

            

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(x, y, width, 30f), selectedThingDef.LabelCap);
            y += 35f;
            Text.Font = GameFont.Small;

            Widgets.Label(new Rect(x, y, width, 24f), "Quantity".Translate());
            Rect countRect = new Rect(x + 50f, y, 60f, 24f);
            string countBuffer = synthesizeCount.ToString();
            Widgets.TextFieldNumeric(countRect, ref synthesizeCount, ref countBuffer, 1, 100);
            y += 30f;

            
            if (Widgets.ButtonText(new Rect(x, y, 120f, 30f), "ChooseFabricator".Translate()))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (var fab in controller.AutoFabricators)
                {
                    if (fab.currentOrder != null) return;
                    int hash = fab.GetHashCode();
                    options.Add(new FloatMenuOption("Fabricator".Translate() + $"HashID: {hash}", () =>
                    {
                        chosenFabricator = fab;
                    }));
                }
                if (options.Count == 0)
                    options.Add(new FloatMenuOption("FabricatorsAllBusy".Translate(), null));
                Find.WindowStack.Add(new FloatMenu(options));
            }
            y += 40f;

            if (chosenFabricator != null)
            {
                Widgets.Label(new Rect(x, y, width, 24f), "Fabricator".Translate() + $"HashID:"+ chosenFabricator.GetHashCode());
                y += 30f;
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
                        stackCountPerBill = selectedSpecialProductionOption.productionCountPerBill,
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

                    controller.AddOrder(order, chosenFabricator);
                    Messages.Message($"BillOrderAdded".Translate() + ":" + $"{selectedThingDef.LabelCap} x{synthesizeCount}", MessageTypeDefOf.TaskCompletion, false);
                    Close();
                }
            }
        }
    }
}