﻿using Artisan.CraftingLists;
using Artisan.RawInformation;
using Artisan.Tasks;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Logging;
using Dalamud.Plugin.Ipc;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.ExcelServices.TerritoryEnumeration;
using ECommons.Reflection;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using static ECommons.GenericHelpers;
using RetainerManager = FFXIVClientStructs.FFXIV.Client.Game.RetainerManager;

namespace Artisan.IPC
{
    public static class RetainerInfo
    {
        private static ICallGateSubscriber<ulong?, bool>? _OnRetainerChanged;
        private static ICallGateSubscriber<(uint, InventoryItem.ItemFlags, ulong, uint), bool>? _OnItemAdded;
        private static ICallGateSubscriber<(uint, InventoryItem.ItemFlags, ulong, uint), bool>? _OnItemRemoved;
        private static ICallGateSubscriber<uint, ulong, uint, uint>? _ItemCount;
        private static ICallGateSubscriber<bool, bool>? _Initialized;
        private static ICallGateSubscriber<bool>? _IsInitialized;

        public static TaskManager TM = new TaskManager();
        internal static bool GenericThrottle => EzThrottler.Throttle("RetainerInfoThrottler", 100);
        internal static void RethrottleGeneric(int num) => EzThrottler.Throttle("RetainerInfoThrottler", num, true);
        internal static void RethrottleGeneric() => EzThrottler.Throttle("RetainerInfoThrottler", 100, true);
        internal static Tasks.RetainerManager retainerManager = new(Svc.SigScanner);

        public static bool ATools => DalamudReflector.TryGetDalamudPlugin("Allagan Tools", out var it, false, true) && _IsInitialized != null && _IsInitialized.InvokeFunc();
        private static uint firstFoundQuantity = 0;

        public static bool CacheBuilt = ATools ? false : true;
        public static CancellationTokenSource CTSource = new();
        public static readonly object _lockObj = new();

        internal static void Init()
        {
            _Initialized = Svc.PluginInterface.GetIpcSubscriber<bool, bool>("AllaganTools.Initialized");
            _IsInitialized = Svc.PluginInterface.GetIpcSubscriber<bool>("AllaganTools.IsInitialized");
            _Initialized.Subscribe(SetupIPC);
            SetupIPC(true);
        }

        private static void SetupIPC(bool obj)
        {

            _OnRetainerChanged = Svc.PluginInterface.GetIpcSubscriber<ulong?, bool>("AllaganTools.RetainerChanged");
            _OnItemAdded = Svc.PluginInterface.GetIpcSubscriber<(uint, InventoryItem.ItemFlags, ulong, uint), bool>("AllaganTools.ItemAdded");
            _OnItemRemoved = Svc.PluginInterface.GetIpcSubscriber<(uint, InventoryItem.ItemFlags, ulong, uint), bool>("AllaganTools.ItemRemoved");

            _ItemCount = Svc.PluginInterface.GetIpcSubscriber<uint, ulong, uint, uint>("AllaganTools.ItemCount");
            _OnItemAdded.Subscribe(OnItemAdded);
            _OnItemRemoved.Subscribe(OnItemRemoved);
            TM.TimeoutSilently = true;

            if (Svc.ClientState.IsLoggedIn)
                LoadCache(true);
            Svc.ClientState.Login += LoadCacheLogin;

        }

        private static void LoadCacheLogin(object? sender, EventArgs e)
        {
            TM.DelayNext("LoadCacheLogin", 5000);
            TM.Enqueue(() => LoadCache(true));
        }

        public async static Task<bool?> LoadCache(bool onLoad = false)
        {
            if (onLoad)
            {
                CraftingListUI.CraftableItems.Clear();
                RetainerData.Clear();
            }

            CacheBuilt = false;
            CraftingListUI.CraftableItems.Clear();

            if (Service.Configuration.ShowOnlyCraftable || onLoad)
            {
                foreach (var recipe in CraftingListUI.FilteredList.Values)
                {
                    if (ATools && Service.Configuration.ShowOnlyCraftableRetainers || onLoad)
                        await Task.Run(async () => await CraftingListUI.CheckForIngredients(recipe, false, true));
                    else
                        await Task.Run(async () => await CraftingListUI.CheckForIngredients(recipe, false, false));
                }
            }

            ClearCache(null);
            CacheBuilt = true;
            return true;
        }

        private static void OnItemAdded((uint, InventoryItem.ItemFlags, ulong, uint) tuple)
        {
            if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.OccupiedSummoningBell])
            {
                ClearCache(null);
            }
        }

        private static void OnItemRemoved((uint, InventoryItem.ItemFlags, ulong, uint) tuple)
        {
            if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.OccupiedSummoningBell])
            {
                ClearCache(null);
            }
        }

        internal static void Dispose()
        {
            _Initialized?.Unsubscribe(SetupIPC);
            _Initialized = null;
            _IsInitialized = null;
            _OnRetainerChanged = null;
            _OnItemAdded?.Unsubscribe(OnItemAdded);
            _OnItemRemoved?.Unsubscribe(OnItemRemoved);
            _OnItemAdded = null;
            _OnItemRemoved = null;
            _ItemCount = null;
            Svc.ClientState.Login -= LoadCacheLogin;
        }

        public static Dictionary<ulong, Dictionary<uint, ItemInfo>> RetainerData = new Dictionary<ulong, Dictionary<uint, ItemInfo>>();
        public class ItemInfo
        {
            public uint ItemID { get; set; }

            public uint Quantity { get; set; }

            public ItemInfo(uint itemId, uint quantity)
            {
                ItemID = itemId;
                Quantity = quantity;
            }
        }

        public static void ClearCache(ulong? RetainerId)
        {
            RetainerData.Each(x => x.Value.Clear());
        }

        public static unsafe uint GetRetainerInventoryItem(uint itemID, ulong retainerId)
        {
            if (ATools)
            {
                return _ItemCount.InvokeFunc(itemID, retainerId, 10000) +
                        _ItemCount.InvokeFunc(itemID, retainerId, 10001) +
                        _ItemCount.InvokeFunc(itemID, retainerId, 10002) +
                        _ItemCount.InvokeFunc(itemID, retainerId, 10003) +
                        _ItemCount.InvokeFunc(itemID, retainerId, 10004) +
                        _ItemCount.InvokeFunc(itemID, retainerId, 10005) +
                        _ItemCount.InvokeFunc(itemID, retainerId, 10006) +
                        _ItemCount.InvokeFunc(itemID, retainerId, (uint)InventoryType.RetainerCrystals);
            }
            return 0;
        }
        public static unsafe uint GetRetainerItemCount(uint itemId, bool tryCache = true)
        {
            lock (_lockObj)
            {
                if (ATools)
                {
                    try
                    {
                        if (tryCache)
                        {
                            if (RetainerData.SelectMany(x => x.Value).Any(x => x.Key == itemId))
                            {
                                return (uint)RetainerData.Values.SelectMany(x => x.Values).Where(x => x.ItemID == itemId).Sum(x => x.Quantity);
                            }
                        }

                        for (int i = 0; i < 10; i++)
                        {
                            ulong retainerId = 0;
                            var retainer = RetainerManager.Instance()->GetRetainerBySortedIndex((uint)i);
                            if (Service.Configuration.RetainerIDs.Count(x => x.Value == Svc.ClientState.LocalContentId) > i)
                            {
                                retainerId = Service.Configuration.RetainerIDs.Where(x => x.Value == Svc.ClientState.LocalContentId).Select(x => x.Key).ToArray()[i];
                            }
                            else
                            {
                                retainerId = RetainerManager.Instance()->GetRetainerBySortedIndex((uint)i)->RetainerID;
                            }

                            if (retainer->RetainerID > 0 && !Service.Configuration.RetainerIDs.Any(x => x.Key == retainer->RetainerID && x.Value == Svc.ClientState.LocalContentId))
                            {
                                Service.Configuration.RetainerIDs.Add(retainer->RetainerID, Svc.ClientState.LocalContentId);
                                Service.Configuration.Save();
                            }

                            if (retainerId > 0)
                            {
                                if (RetainerData.ContainsKey(retainerId))
                                {
                                    var ret = RetainerData[retainerId];
                                    if (ret.ContainsKey(itemId))
                                    {
                                        var item = ret[itemId];
                                        item.ItemID = itemId;
                                        item.Quantity = GetRetainerInventoryItem(itemId, retainerId);

                                    }
                                    else
                                    {
                                        ret.TryAdd(itemId, new ItemInfo(itemId, GetRetainerInventoryItem(itemId, retainerId)));
                                    }
                                }
                                else
                                {
                                    RetainerData.TryAdd(retainerId, new Dictionary<uint, ItemInfo>());
                                    var ret = RetainerData[retainerId];
                                    if (ret.ContainsKey(itemId))
                                    {
                                        var item = ret[itemId];
                                        item.ItemID = itemId;
                                        item.Quantity = GetRetainerInventoryItem(itemId, retainerId);

                                    }
                                    else
                                    {
                                        ret.TryAdd(itemId, new ItemInfo(itemId, GetRetainerInventoryItem(itemId, retainerId)));

                                    }
                                }
                            }
                        }

                        return (uint)RetainerData.SelectMany(x => x.Value).Where(x => x.Key == itemId).Sum(x => x.Value.Quantity);
                    }
                    catch (Exception ex)
                    {
                        PluginLog.Error(ex, "RetainerInfoItemCount");
                        return 0;
                    }
                }
            }
            return 0;
        }

        public static void RestockFromRetainers(CraftingList list)
        {
            if (GetReachableRetainerBell() == null) return;

            Dictionary<int, int> requiredItems = new();
            Dictionary<int, int> materialList = new();

            foreach (var item in list.Items)
            {
                var recipe = LuminaSheets.RecipeSheet[item];
                CraftingListUI.AddRecipeIngredientsToList(recipe, ref materialList, false);
            }

            foreach (var material in materialList.OrderByDescending(x => x.Key))
            {
                var invCount = CraftingListUI.NumberOfIngredient((uint)material.Key);
                if (invCount < material.Value)
                {
                    var diffcheck = material.Value - invCount;
                    requiredItems.Add(material.Key, diffcheck);
                }
            }

            if (RetainerData.SelectMany(x => x.Value).Any(x => requiredItems.Any(y => y.Key == x.Value.ItemID)))
            {
                TM.Enqueue(() => AutoRetainer.Suppress());
                TM.EnqueueBell();
                TM.DelayNext("BellInteracted", 200);
                foreach (var retainer in RetainerData)
                {
                    if (retainer.Value.Values.Any(x => requiredItems.Any(y => y.Value > 0 && y.Key == x.ItemID && x.Quantity > 0)))
                    {
                        TM.Enqueue(() => RetainerListHandlers.SelectRetainerByID(retainer.Key));
                        TM.DelayNext("WaitToSelectEntrust", 200);
                        TM.Enqueue(() => RetainerHandlers.SelectEntrustItems());
                        TM.DelayNext("EntrustSelected", 200);
                        foreach (var item in requiredItems)
                        {
                            if (retainer.Value.Values.Any(x => x.ItemID == item.Key && x.Quantity > 0))
                            {
                                TM.DelayNext("SwitchItems", 200);
                                TM.Enqueue(() =>
                                {
                                    if (requiredItems[item.Key] != 0)
                                    {
                                        TM.EnqueueImmediate(() => RetainerHandlers.OpenItemContextMenu((uint)item.Key, out firstFoundQuantity), 1500);
                                        TM.DelayNextImmediate("WaitOnNumericPopup", 300);
                                        TM.EnqueueImmediate(() =>
                                        {
                                            var value = Math.Min(requiredItems[item.Key], (int)firstFoundQuantity);
                                            if (value == 1) return true;
                                            if (RetainerHandlers.InputNumericValue(value))
                                            {
                                                requiredItems[item.Key] -= value;

                                                return true;
                                            }
                                            else
                                            {
                                                return false;
                                            }
                                        }, 300);
                                    }
                                });

                                TM.Enqueue(() =>
                                {
                                    if (requiredItems[item.Key] != 0)
                                    {
                                        TM.DelayNextImmediate("TryForExtraMat", 200);
                                        TM.EnqueueImmediate(() => RetainerHandlers.OpenItemContextMenu((uint)item.Key, out firstFoundQuantity), 1500);
                                        TM.DelayNextImmediate("WaitOnNumericPopup", 300);
                                        TM.EnqueueImmediate(() =>
                                        {
                                            var value = Math.Min(requiredItems[item.Key], (int)firstFoundQuantity);
                                            if (value == 1) return true;
                                            if (RetainerHandlers.InputNumericValue(value))
                                            {
                                                requiredItems[item.Key] -= value;

                                                return true;
                                            }
                                            else
                                            {
                                                return false;
                                            }
                                        }, 300);
                                    }
                                });

                            }
                        }
                        TM.DelayNext("CloseRetainer", 200);
                        TM.Enqueue(() => RetainerHandlers.CloseAgentRetainer());
                        TM.DelayNext("ClickQuit", 200);
                        TM.Enqueue(() => RetainerHandlers.SelectQuit());
                    }
                }
                TM.DelayNext("CloseRetainerList", 200);
                TM.Enqueue(() => RetainerListHandlers.CloseRetainerList());
                TM.Enqueue(() => YesAlready.EnableIfNeeded());
                TM.Enqueue(() => AutoRetainer.Unsuppress());
            }
        }

        internal static GameObject GetReachableRetainerBell()
        {
            foreach (var x in Svc.Objects)
            {
                if ((x.ObjectKind == ObjectKind.Housing || x.ObjectKind == ObjectKind.EventObj) && x.Name.ToString().EqualsIgnoreCaseAny(BellName, "リテイナーベル"))
                {
                    if (Vector3.Distance(x.Position, Svc.ClientState.LocalPlayer.Position) < GetValidInteractionDistance(x) && x.IsTargetable())
                    {
                        return x;
                    }
                }
            }
            return null;
        }

        internal static float GetValidInteractionDistance(GameObject bell)
        {
            if (bell.ObjectKind == ObjectKind.Housing)
            {
                return 6.5f;
            }
            else if (Inns.List.Contains(Svc.ClientState.TerritoryType))
            {
                return 4.75f;
            }
            else
            {
                return 4.6f;
            }
        }

        internal static string BellName
        {
            get => Svc.Data.GetExcelSheet<EObjName>().GetRow(2000401).Singular.ToString();
        }

        public unsafe static bool IsTargetable(this GameObject o)
        {
            return o.Struct()->GetIsTargetable();
        }

        public unsafe static FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject* Struct(this GameObject o)
        {
            return (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)o.Address;
        }
    }
}
