using AutoFabricator;
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
    public class Window_FabricatorControlExtended : Window
    {
        private Comp_FabricatorController controller;
        private Vector2 scrollPosition;
        private Vector2 orderListScrollPosition;
        private Dictionary<string, bool> expandedCategories = new Dictionary<string, bool>();
        
        // 特殊配方相关字段
        private SpecialProductionOption selectedSpecialOption = null;
        private ThingDef selectedStuffDef = null;
        private int synthesizeCount = 1;
        
        private float LeftlineHight = 40f;
        private Comp_AutoFabricator chosenFab = null;

        public Window_FabricatorControlExtended(Comp_FabricatorController controller)
        {
            this.controller = controller;
            this.doCloseButton = true;
            this.absorbInputAroundWindow = true;
            this.closeOnClickedOutside = true;
        }

        public override Vector2 InitialSize => new Vector2(900f, 600f);

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 40f), "SpecialProductionController".Translate());
            Widgets.DrawLineHorizontal(0f, 42f, inRect.width);

            Text.Font = GameFont.Small;

            if (!controller.IsConnected)
            {
                GUI.color = Color.red;
                Widgets.Label(new Rect(0f, 50f, 200f, 30f), "NotConnected".Translate());
                GUI.color = Color.white;
                return;
            }

            // 左侧：特殊配方列表
            Rect leftRect = new Rect(inRect.x, inRect.y + 50f, 350f, inRect.height - 60f);
            Widgets.DrawMenuSection(leftRect);

            // 右侧：订单详情/订单列表
            Rect rightRect = new Rect(leftRect.xMax + 10f, leftRect.y, inRect.width - leftRect.width - 20f, leftRect.height);
            Widgets.DrawMenuSection(rightRect);

            DrawSpecialProductionList(leftRect);
            DrawSelectedThingPanel(rightRect);
        }

        private int CalculateSpecialProductNum()
        {
            return controller.Props.specialProductions?.Count ?? 0;
        }

        private void DrawSpecialProductionList(Rect rect)
        {
            var specials = controller.Props.specialProductions;
            if (specials == null || specials.Count == 0)
            {
                Text.Font = GameFont.Small;
                Widgets.Label(new Rect(rect.x + 10f, rect.y + 10f, rect.width - 20f, 30f), "NoSpecialProductions".Translate());
                return;
            }

            Rect viewRect = new Rect(0, 0, rect.width - 16f, CalculateSpecialProductNum() * LeftlineHight);
            Widgets.BeginScrollView(rect, ref scrollPosition, viewRect);

            float curY = 0f;
            foreach (var option in specials)
            {
                if (option == null || option.ProductDef == null) continue;

                float iconSize = 18f;
                float padding = 6f;
                string label = option.customLabel ?? option.ProductDef.LabelCap;
                float textWidth = Text.CalcSize(label).x;
                float startX = 10f;

                Rect rowRect = new Rect(startX, curY, viewRect.width - 20f, LeftlineHight);
                Rect iconRect = new Rect(startX, curY + (LeftlineHight - iconSize) / 2, iconSize, iconSize);
                Widgets.ThingIcon(iconRect, option.ProductDef);

                Rect textRect = new Rect(startX + iconSize + padding, curY + (LeftlineHight - iconSize) / 2, textWidth, LeftlineHight);
                Widgets.Label(textRect, label);

                if (Widgets.ButtonInvisible(rowRect))
                {
                    selectedSpecialOption = option;
                    selectedStuffDef = null;
                    synthesizeCount = 1;
                }

                if (selectedSpecialOption == option)
                {
                    Widgets.DrawHighlightSelected(rowRect);
                }

                curY += LeftlineHight;
            }

            Widgets.EndScrollView();
        }

        private void DrawSelectedThingPanel(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            float y = rect.y + 10f;
            float x = rect.x + 10f;
            float width = rect.width - 20f;

            // 未选择特殊配方时显示订单列表
            if (selectedSpecialOption == null)
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
                    if (order == null) continue;
                    string stuffStr = order.StuffDef != null ? $"({order.StuffDef.LabelCap})" : "";
                    string line = $"{order.ProductDef.LabelCap}{stuffStr} x{order.Quantity}" + " " + "Remaining".Translate() + " : " + order.leftQuantity + " ";
                    
                    if (controller.OrderAllocationDict.ContainsKey(order))
                    {
                        if (controller.OrderAllocationDict[order] != null)
                        {
                            Comp_AutoFabricator af = controller.OrderAllocationDict[order].TryGetComp<Comp_AutoFabricator>();
                            if (af != null)
                            {
                                line += af.FabricatorID;
                                line += " " + "Status".Translate() + ":" + (af.StateCheck() ? "Working".Translate() : "Broken".Translate());
                            }
                            else
                            {
                                line += "None";
                            }
                        }
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

            // 已选择特殊配方，显示详情
            Text.Font = GameFont.Medium;
            string optionLabel = selectedSpecialOption.customLabel ?? selectedSpecialOption.ProductDef.LabelCap;
            Widgets.Label(new Rect(x, y, width, 30f), optionLabel);
            y += 35f;
            Text.Font = GameFont.Small;

            // 显示工作量信息
            Widgets.Label(new Rect(x, y, width, 24f), "WorkAmount".Translate() + ": " + selectedSpecialOption.workToMake);
            y += 26f;

            // 显示每次生产数量
            Widgets.Label(new Rect(x, y, width, 24f), "ProductionPerBill".Translate() + ": " + selectedSpecialOption.productionCountPerBill);
            y += 26f;

            // 材料需求
            Widgets.Label(new Rect(x, y, width, 24f), "Materials".Translate().Resolve());
            y += 26f;
            foreach (var cost in selectedSpecialOption.specialCostList ?? new List<ThingDefCountClass>())
            {
                Widgets.Label(new Rect(x + 10f, y, width - 10f, 24f), $"{cost.thingDef.LabelCap} x{cost.count}");
                y += 24f;
            }

            // 材质选择（如果有）
            if (selectedSpecialOption.specialStuffCategories != null && selectedSpecialOption.specialStuffCategories.Count > 0)
            {
                TaggedString stuffLabel = ((selectedStuffDef != null) ? selectedStuffDef.LabelCap : "ChooseStuff".Translate());
                if (Widgets.ButtonText(new Rect(x, y, 160f, 28f), stuffLabel))
                {
                    List<FloatMenuOption> options = new List<FloatMenuOption>();
                    foreach (var stuff in DefDatabase<ThingDef>.AllDefs.Where(td =>
                        td.IsStuff && td.stuffProps != null && selectedSpecialOption.specialStuffCategories.Any(cat => td.stuffProps.categories.Contains(cat))))
                    {
                        options.Add(new FloatMenuOption(stuff.LabelCap, () =>
                        {
                            selectedStuffDef = stuff;
                        }));
                    }
                    Find.WindowStack.Add(new FloatMenu(options));
                }
                y += 34f;
            }

            // 生产次数
            Widgets.Label(new Rect(x, y, 80f, 28f), "Quantity".Translate());
            string buffer = synthesizeCount.ToString();
            Widgets.TextFieldNumeric(new Rect(x + 80f, y, 60f, 28f), ref synthesizeCount, ref buffer, 1, 999);
            y += 34f;

            // 选择组装机
            TaggedString FabricatorLabel = ((chosenFab != null) ? chosenFab.FabricatorID.Translate() : "ChooseFabricator".Translate());
            
            if (Widgets.ButtonText(new Rect(x, y, 120f, 30f), FabricatorLabel))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (var fab in controller.AutoFabricators.Where(f => f.currentOrder == null))
                {
                    string id = fab.FabricatorID;
                    options.Add(new FloatMenuOption(id, () =>
                    {
                        chosenFab = fab;
                    }));
                }
                if (options.Count == 0)
                    options.Add(new FloatMenuOption("FabricatorsAllBusy".Translate(), null));
                Find.WindowStack.Add(new FloatMenu(options));
            }
            y += 34f;

            // 确认按钮
            if (Widgets.ButtonText(new Rect(x + 40f, rect.yMax - 80f, 120f, 32f), "Confirm"))
            {
                if (synthesizeCount > 0 && selectedSpecialOption != null)
                {
                    var order = new ProductionOrder
                    {
                        ProductDef = selectedSpecialOption.ProductDef,
                        Quantity = synthesizeCount,
                        leftQuantity = synthesizeCount,
                        stackCountPerBill = selectedSpecialOption.productionCountPerBill,
                        //worktomake = selectedSpecialOption.workToMake,
                        TotalWorkNeeded = selectedSpecialOption.workToMake
                    };

                    // 材质处理
                    if (selectedSpecialOption.specialStuffCategories != null && selectedSpecialOption.specialStuffCategories.Count > 0)
                    {
                        if (selectedStuffDef == null)
                        {
                            selectedStuffDef = GenStuff.DefaultStuffFor(selectedSpecialOption.ProductDef);
                        }
                        if (selectedStuffDef != null)
                        {
                            order.StuffDef = selectedStuffDef;
                        }
                    }

                    controller.AddOrder(order, chosenFab);
                    Messages.Message($"BillOrderAdded".Translate() + ":" + $"{optionLabel} x{synthesizeCount}", MessageTypeDefOf.TaskCompletion, false);
                    Close();
                }
            }
        }
    }
}
