﻿using Artisan.QuestSync;
using Artisan.RawInformation;
using Dalamud.Logging;
using Dalamud.Utility;
using ECommons;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using System.Collections.Generic;
using System.Linq;

namespace Artisan.CraftingLists
{
    internal static class SpecialLists
    {
        private static string listName = string.Empty;
        private static Dictionary<uint, bool> JobSelected = LuminaSheets.ClassJobSheet.Values.Where(x => x.RowId >= 8 && x.RowId <= 15).ToDictionary(x => x.RowId, x => false);
        private static Dictionary<ushort, bool> Durabilities = LuminaSheets.RecipeSheet.Values.Where(x => x.Number > 0).Select(x => (ushort)(x.RecipeLevelTable.Value.Durability * ((float)x.DurabilityFactor / 100))).Distinct().Order().ToDictionary(x => x, x => false);

        private static int minLevel = 1;
        private static int maxLevel = 90;

        private static int minCraftsmanship = LuminaSheets.RecipeSheet.Values.Min(x => x.RequiredCraftsmanship);
        private static int minControl = LuminaSheets.RecipeSheet.Values.Min(x => x.RequiredControl);

        private static Dictionary<int, bool> isExpert = new Dictionary<int, bool>() { [1] = false, [2] = false };
        private static Dictionary<int, bool> hasToBeUnlocked = new Dictionary<int, bool>() { [1] = false, [2] = false };
        private static Dictionary<int, bool> questRecipe = new Dictionary<int, bool>() { [1] = false, [2] = false };
        private static Dictionary<int, bool> isSecondary = new Dictionary<int, bool>() { [1] = false, [2] = false };

        private static Dictionary<int, bool> Yields = LuminaSheets.RecipeSheet.Values.DistinctBy(x => x.AmountResult).OrderBy(x => x.AmountResult).ToDictionary(x => (int)x.AmountResult, x => false);
        //private static Dictionary<float, bool> PatchRelease = LuminaSheets.RecipeSheet.Values.Where(x => x.PatchNumber > 0).DistinctBy(x => x.PatchNumber).OrderBy(x => x.PatchNumber).ToDictionary(x => (float)x.PatchNumber / 100, x => false);
        private static Dictionary<string, bool> Stars = LuminaSheets.RecipeLevelTableSheet.Values.DistinctBy(x => x.Stars).ToDictionary(x => "★".Repeat(x.Stars), x => false);
        private static Dictionary<int, bool> Stats = LuminaSheets.RecipeSheet.Values.SelectMany(x => x.ItemResult.Value.UnkData59).DistinctBy(x => x.BaseParam).Where(x => x.BaseParam > 0).OrderBy(x => x.BaseParam).ToDictionary(x => (int)x.BaseParam, x => false);

        private static float DurY = 0f;
        public static void Draw()
        {
            ImGui.TextWrapped($@"此部分主要是基于特定标准来创建列表, 而非逐个添加. 请为你的列表命名, 然后在下方调整你的标准, 最后点击""创建列表"", 插件会自动生成符合标准的物品列表. 如果你不勾选任何复选框, 则该类别将会被视为""任意""或""所有""");

            ImGui.Separator();

            ImGui.Columns(6, null, false);

            ImGui.TextWrapped("列表名称");
            ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X);
            ImGui.InputText("###NameInput", ref listName, 300);

            ImGui.TextWrapped($"最大耐久");
            if (ImGui.BeginListBox("###SpecialListDurability", new System.Numerics.Vector2(ImGui.GetContentRegionAvail().X, 110)))
            {
                ImGui.Columns(2, null, false);
                foreach (var dur in Durabilities)
                {
                    var val = dur.Value;
                    if (ImGui.Checkbox($"{dur.Key}", ref val))
                    {
                        Durabilities[dur.Key] = val;
                    }
                    ImGui.NextColumn();
                }
                ImGui.EndListBox();

                DurY = ImGui.GetCursorPosY();
            }
            ImGui.Columns(6, null, false);
            ImGui.NextColumn();

            ImGui.TextWrapped("选择职业");
            if (ImGui.BeginListBox("###JobSelectListBox", new System.Numerics.Vector2(ImGui.GetContentRegionAvail().X, 110)))
            {
                ImGui.Columns(2, null, false);
                foreach (var item in JobSelected)
                {
                    string jobName = LuminaSheets.ClassJobSheet[item.Key].Name.ToString().ToUpper();
                    bool val = item.Value;
                    if (ImGui.Checkbox(jobName, ref val))
                    {
                        JobSelected[item.Key] = val;
                    }
                    ImGui.NextColumn();
                }

                ImGui.EndListBox();
            }
            ImGui.Columns(6, null, false);
            ImGui.NextColumn();
            ImGui.TextWrapped("最低等级");
            ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X);
            ImGui.SliderInt("###SpecialListMinLevel", ref minLevel, 1, 90);

            ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X);
            ImGui.TextWrapped($"可解锁的配方");
            if (ImGui.BeginListBox("###UnlockableRecipe", new System.Numerics.Vector2(ImGui.GetContentRegionAvail().X, 32f.Scale())))
            {
                ImGui.Columns(2, null, false);
                bool yes = hasToBeUnlocked[1];
                if (ImGui.Checkbox("是", ref yes))
                {
                    hasToBeUnlocked[1] = yes;
                }
                ImGui.NextColumn();
                bool no = hasToBeUnlocked[2];
                if (ImGui.Checkbox("否", ref no))
                {
                    hasToBeUnlocked[2] = no;
                }
                ImGui.Columns(6, null, false);
                ImGui.EndListBox();
            }

            ImGui.TextWrapped($"任务配方");
            if (ImGui.BeginListBox("###QuestRecipe", new System.Numerics.Vector2(ImGui.GetContentRegionAvail().X, 32f.Scale())))
            {
                ImGui.Columns(2, null, false);
                bool yes = questRecipe[1];
                if (ImGui.Checkbox("是", ref yes))
                {
                    questRecipe[1] = yes;
                }
                ImGui.NextColumn();
                bool no = questRecipe[2];
                if (ImGui.Checkbox("否", ref no))
                {
                    questRecipe[2] = no;
                }
                ImGui.Columns(6, null, false);
                ImGui.EndListBox();
            }
            ImGui.NextColumn();

            ImGui.TextWrapped("最大等级");
            ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X);
            ImGui.SliderInt("###SpecialListMaxLevel", ref maxLevel, 1, 90);

            ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X);
            ImGui.TextWrapped($"专家配方");
            if (ImGui.BeginListBox("###ExpertRecipe", new System.Numerics.Vector2(ImGui.GetContentRegionAvail().X, 32f.Scale())))
            {
                ImGui.Columns(2, null, false);
                bool yes = isExpert[1];
                if (ImGui.Checkbox("是", ref yes))
                {
                    isExpert[1] = yes;
                }
                ImGui.NextColumn();
                bool no = isExpert[2];
                if (ImGui.Checkbox("否", ref no))
                {
                    isExpert[2] = no;
                }
                ImGui.Columns(6, null, false);
                ImGui.EndListBox();
            }

            ImGui.TextWrapped($"次级配方");
            if (ImGui.BeginListBox("###SecondaryRecipes", new System.Numerics.Vector2(ImGui.GetContentRegionAvail().X, 32f.Scale())))
            {
                ImGui.Columns(2, null, false);
                bool yes = isSecondary[1];
                if (ImGui.Checkbox("是", ref yes))
                {
                    isSecondary[1] = yes;
                }
                ImGui.NextColumn();
                bool no = isSecondary[2];
                if (ImGui.Checkbox("否", ref no))
                {
                    isSecondary[2] = no;
                }
                ImGui.Columns(6, null, false);
                ImGui.EndListBox();
            }
            ImGui.NextColumn();

            ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X);
            ImGui.TextWrapped($"最低作业精度");
            ImGui.SliderInt($"###MinCraftsmanship", ref minCraftsmanship, LuminaSheets.RecipeSheet.Values.Min(x => x.RequiredCraftsmanship), LuminaSheets.RecipeSheet.Values.Max(x => x.RequiredCraftsmanship));

            ImGui.TextWrapped("成品数量");
            if (ImGui.BeginListBox("###Yields", new System.Numerics.Vector2(ImGui.GetContentRegionAvail().X, 147f.Scale())))
            {
                ImGui.Columns(2, null, false);
                foreach (var yield in Yields)
                {
                    var val = yield.Value;
                    if (ImGui.Checkbox($"{yield.Key}", ref val))
                    {
                        Yields[yield.Key] = val;
                    }
                    ImGui.NextColumn();
                }
                ImGui.EndListBox();
            }
            ImGui.Columns(6, null, false);
            ImGui.NextColumn();
            ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X);
            ImGui.TextWrapped($"最低加工精度");
            ImGui.SliderInt($"###MinControl", ref minControl, LuminaSheets.RecipeSheet.Values.Min(x => x.RequiredControl), LuminaSheets.RecipeSheet.Values.Max(x => x.RequiredControl));

            ImGui.TextWrapped("难度");
            if (ImGui.BeginListBox("###Stars", new System.Numerics.Vector2(ImGui.GetContentRegionAvail().X, 162f.Scale())))
            {
                foreach (var star in Stars)
                {
                    var val = star.Value;
                    if (ImGui.Checkbox($"{star.Key}", ref val))
                    {
                        Stars[star.Key] = val;
                    }
                }
                ImGui.EndListBox();
            }

            ImGui.Columns(1);
            ImGui.SetCursorPosY(DurY);
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 4);
            ImGui.TextWrapped("基础属性");
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 4);
            if (ImGui.BeginListBox("###Stats", new System.Numerics.Vector2((ImGui.GetContentRegionAvail().X / 6) * 4, 120)))
            {
                ImGui.Columns(5, null, false);
                foreach (var stat in Stats)
                {
                    var val = stat.Value;
                    if (ImGui.Checkbox($"###{Svc.Data.GetExcelSheet<BaseParam>().First(x => x.RowId == stat.Key).Name.ExtractText()}", ref val))
                    {
                        Stats[stat.Key] = val;
                    }
                    ImGui.SameLine();
                    ImGui.TextWrapped($"{Svc.Data.GetExcelSheet<BaseParam>().First(x => x.RowId == stat.Key).Name.ExtractText()}");
                    ImGui.NextColumn();
                }

                ImGui.EndListBox();
            }
            ImGui.Columns(1);

            ImGui.Spacing();
            if (ImGui.Button("创建列表", new System.Numerics.Vector2(ImGui.GetContentRegionAvail().X, 0)))
            {
                if (listName.IsNullOrWhitespace())
                {
                    Notify.Error("请命名你的列表.");
                    return;
                }

                if (CreateList(false))
                {
                    Notify.Success($"{listName} 已创建.");
                }
            }
            if (ImGui.Button("创建列表 (附带子配方)", new System.Numerics.Vector2(ImGui.GetContentRegionAvail().X, 0)))
            {
                if (listName.IsNullOrWhitespace())
                {
                    Notify.Error("请命名你的列表.");
                    return;
                }

                if (CreateList(true))
                {
                    Notify.Success($"{listName} 已创建.");
                }
            }
        }

        private static bool CreateList(bool withSubcrafts)
        {
            var craftingList = new CraftingList();
            craftingList.Name = listName;
            var recipes = new List<Recipe>();
            foreach (var job in JobSelected)
            {
                if (job.Value)
                {
                    recipes.AddRange(LuminaSheets.RecipeSheet.Values.Where(x => x.CraftType.Row == job.Key - 8));

                    if (Stats.Any(x => x.Value))
                    {
                        recipes.RemoveAll(x => x.ItemResult.Value.UnkData59.All(y => y.BaseParam == 0));
                        foreach (var v in Stats.Where(x => x.Key > 0).OrderByDescending(x => x.Key == 70 || x.Key == 71 || x.Key == 72 || x.Key == 73).ThenBy(x => x.Key))
                        {
                            if (!v.Value)
                            {
                                recipes.RemoveAll(x => x.ItemResult.Value.UnkData59[0].BaseParam == v.Key);
                            }
                            else
                            {
                                recipes.AddRange(LuminaSheets.RecipeSheet.Values.Where(x => x.ItemResult.Value.UnkData59.Any(y => y.BaseParam == v.Key) && x.CraftType.Row == job.Key - 8));
                            }
                        }
                    }
                }
            }

            foreach (var quest in QuestList.Quests)
            {
                recipes.RemoveAll(x => x.RowId == quest.Value.CRP);
                recipes.RemoveAll(x => x.RowId == quest.Value.BSM);
                recipes.RemoveAll(x => x.RowId == quest.Value.ARM);
                recipes.RemoveAll(x => x.RowId == quest.Value.GSM);
                recipes.RemoveAll(x => x.RowId == quest.Value.LTW);
                recipes.RemoveAll(x => x.RowId == quest.Value.WVR);
                recipes.RemoveAll(x => x.RowId == quest.Value.ALC);
                recipes.RemoveAll(x => x.RowId == quest.Value.CUL);
            }


            recipes.RemoveAll(x => x.RecipeLevelTable.Value.ClassJobLevel < minLevel);
            recipes.RemoveAll(x => x.RecipeLevelTable.Value.ClassJobLevel > maxLevel);
            recipes.RemoveAll(x => x.RequiredCraftsmanship < minCraftsmanship);
            recipes.RemoveAll(x => x.RequiredControl < minControl);

            if (Durabilities.Any(x => x.Value))
            {
                foreach (var dur in Durabilities)
                {
                    if (!dur.Value)
                    {
                        recipes.RemoveAll(x => (ushort)(x.RecipeLevelTable.Value.Durability * ((float)x.DurabilityFactor / 100)) == dur.Key);
                    }
                }
            }

            if (hasToBeUnlocked.Any(x => x.Value))
            {
                foreach (var v in hasToBeUnlocked)
                {
                    if (!v.Value)
                    {
                        if (v.Key == 1)
                        {
                            recipes.RemoveAll(x => x.SecretRecipeBook.Row > 0);
                        }
                        else
                        {
                            recipes.RemoveAll(x => x.SecretRecipeBook.Row == 0);
                        }
                    }
                }
            }

            if (isExpert.Any(x => x.Value))
            {
                foreach (var v in isExpert)
                {
                    if (!v.Value)
                    {
                        if (v.Key == 1)
                        {
                            recipes.RemoveAll(x => x.IsExpert);
                        }
                        else
                        {
                            recipes.RemoveAll(x => !x.IsExpert);
                        }
                    }
                }
            }

            if (questRecipe.Any(x => x.Value))
            {
                foreach (var v in questRecipe)
                {
                    if (!v.Value)
                    {
                        if (v.Key == 1)
                        {
                            recipes.RemoveAll(x => x.Quest.Row > 0);
                        }
                        else
                        {
                            recipes.RemoveAll(x => x.Quest.Row == 0);
                        }
                    }
                }
            }

            if (isSecondary.Any(x => x.Value))
            {
                foreach (var v in isSecondary)
                {
                    if (!v.Value)
                    {
                        if (v.Key == 1)
                        {
                            recipes.RemoveAll(x => x.IsSecondary);
                        }
                        else
                        {
                            recipes.RemoveAll(x => !x.IsSecondary);
                        }
                    }
                }
            }

            if (Yields.Any(x => x.Value))
            {
                foreach (var v in Yields)
                {
                    if (!v.Value)
                    {
                        recipes.RemoveAll(x => x.AmountResult == v.Key);
                    }
                }
            }

            if (Stars.Any(x => x.Value))
            {
                foreach (var v in Stars)
                {
                    if (!v.Value)
                    {
                        recipes.RemoveAll(x => x.RecipeLevelTable.Value.Stars == v.Key.Length);
                    }
                }
            }

            if (recipes.Count == 0)
            {
                Notify.Error("你选定的标准内没有任何匹配的物品.");
                return false;
            }

            if (!withSubcrafts)
            {
                foreach (var recipe in recipes.Distinct())
                {
                    craftingList.Items.Add(recipe.RowId);
                }
                craftingList.SetID();
                craftingList.Save(true);
            }
            else
            {
                foreach (var recipe in recipes.Distinct())
                {
                    CraftingListUI.AddAllSubcrafts(recipe, craftingList, 1);
                    craftingList.Items.Add(recipe.RowId);
                }
                craftingList.SetID();
                craftingList.Save(true);
            }

            return true;
        }
    }
}
