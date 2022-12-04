using Oasys.Common.Enums.GameEnums;
using Oasys.Common.EventsProvider;
using Oasys.Common.GameObject.Clients;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.SpellCasting;
using Oasys.SDK.Tools;
using SixAIO.Helpers;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SixAIO.Utilities
{
    internal sealed class AutoMikaelsBlessing
    {
        private static float _lastCleanse = 0f;

        private static Tab Tab => MenuManagerProvider.GetTab("SIXAIO - Utilities");
        private static Group AutoMikaelsBlessingGroup => Tab.GetGroup("Auto Mikaels Blessing");

        private static bool LogMikaelsBuff
        {
            get => AutoMikaelsBlessingGroup.GetItem<Switch>("Log Mikaels Buff").IsOn;
            set => AutoMikaelsBlessingGroup.GetItem<Switch>("Log Mikaels Buff").IsOn = value;
        }

        private static bool UseMikaelsBlessing
        {
            get => AutoMikaelsBlessingGroup.GetItem<Switch>("Use Mikaels Blessing").IsOn;
            set => AutoMikaelsBlessingGroup.GetItem<Switch>("Use Mikaels Blessing").IsOn = value;
        }

        private static int ReactionDelay
        {
            get => AutoMikaelsBlessingGroup?.GetItem<Counter>("Reaction Delay")?.Value ?? 100;
            set => AutoMikaelsBlessingGroup.GetItem<Counter>("Reaction Delay").Value = value;
        }

        internal static Task GameEvents_OnGameLoadComplete()
        {
            TabItem.OnTabItemChange += TabItem_OnTabItemChange;
            Tab.AddGroup(new Group("Auto Mikaels Blessing"));
            AutoMikaelsBlessingGroup.AddItem(new Switch() { Title = "Log Cleanse Buff", IsOn = true });
            AutoMikaelsBlessingGroup.AddItem(new Switch() { Title = "Use Mikaels Blessing", IsOn = true });

            AutoMikaelsBlessingGroup.AddItem(new InfoDisplay() { Title = "-Only mikaels debuffs longer than ms-" });
            AutoMikaelsBlessingGroup.AddItem(new Counter() { Title = "Stun", Value = 500, MinValue = 0, MaxValue = 5000, ValueFrequency = 250 });
            AutoMikaelsBlessingGroup.AddItem(new Counter() { Title = "Snare", Value = 500, MinValue = 0, MaxValue = 5000, ValueFrequency = 250 });
            AutoMikaelsBlessingGroup.AddItem(new Counter() { Title = "Charm", Value = 500, MinValue = 0, MaxValue = 5000, ValueFrequency = 250 });
            AutoMikaelsBlessingGroup.AddItem(new Counter() { Title = "Taunt", Value = 500, MinValue = 0, MaxValue = 5000, ValueFrequency = 250 });
            AutoMikaelsBlessingGroup.AddItem(new Counter() { Title = "Fear", Value = 500, MinValue = 0, MaxValue = 5000, ValueFrequency = 250 });
            AutoMikaelsBlessingGroup.AddItem(new Counter() { Title = "Flee", Value = 500, MinValue = 0, MaxValue = 5000, ValueFrequency = 250 });
            AutoMikaelsBlessingGroup.AddItem(new Counter() { Title = "Sleep", Value = 1250, MinValue = 0, MaxValue = 5000, ValueFrequency = 250 });
            AutoMikaelsBlessingGroup.AddItem(new Counter() { Title = "Polymorph", Value = 1250, MinValue = 0, MaxValue = 5000, ValueFrequency = 250 });

            AutoMikaelsBlessingGroup.AddItem(new InfoDisplay() { Title = "-Only mikaels allies-" });
            foreach (var allyChampion in UnitManager.AllyChampions.Where(x => !x.IsTargetDummy && x.NetworkID != UnitManager.MyChampion.NetworkID))
            {
                AutoMikaelsBlessingGroup.AddItem(new Counter() { Title = "Mikaels Ally Prio- " + allyChampion.ModelName, MinValue = 0, MaxValue = 5, Value = 0 });
            }

            return Task.CompletedTask;
        }

        private static void TabItem_OnTabItemChange(string tabName, TabItem tabItem)
        {
            try
            {
                if (tabItem.TabName == "SIXAIO - Utilities" && tabItem.GroupName == "Auto Mikaels Blessing")
                {
                    if (tabItem.Title == "Use Mikaels Blessing" && tabItem is Switch itemSwitchOnCombo)
                    {
                        if (itemSwitchOnCombo.IsOn)
                        {
                            Logger.Log($"[AutoMikaelsBlessing] Activated!");
                            CoreEvents.OnCoreMainInputAsync += InputHandler;
                        }
                        else
                        {
                            Logger.Log($"[AutoMikaelsBlessing] Deactivated!");
                            CoreEvents.OnCoreMainInputAsync -= InputHandler;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }

        private static Task InputHandler()
        {
            try
            {
                if (UseMikaelsBlessing &&
                    UnitManager.MyChampion.IsAlive &&
                    TargetSelector.IsAttackable(UnitManager.MyChampion, false) &&
                    GameEngine.GameTime > _lastCleanse + 1f &&
                    UnitManager.MyChampion.Inventory.HasItem(ItemID.Mikaels_Blessing) &&
                    UnitManager.MyChampion.Inventory.GetItemByID(ItemID.Mikaels_Blessing)?.IsReady == true)
                {
                    var target = UnitManager.AllyChampions
                                            .Where(ally => AutoMikaelsBlessingGroup.GetItem<Counter>("Mikaels Ally Prio- " + ally?.ModelName)?.Value > 0)
                                            .OrderByDescending(ally => AutoMikaelsBlessingGroup.GetItem<Counter>("Mikaels Ally Prio- " + ally?.ModelName)?.Value)
                                            .FirstOrDefault(IsCrowdControlled);

                    if (target is not null)
                    {
                        ItemCastProvider.CastItem(ItemID.Mikaels_Blessing, target);
                        _lastCleanse = GameEngine.GameTime;
                    }
                }
            }
            catch (Exception ex)
            {
            }

            return Task.CompletedTask;
        }

        private static bool IsCrowdControlled(AIHeroClient hero)
        {
            try
            {
                var buffs = hero.BuffManager.GetBuffList();
                if (BuffChecker.IsKnockedUpOrBack(hero))
                {
                    return false;
                }

                var gameTime = GameEngine.GameTime;
                var cc = buffs.Where(BuffChecker.IsCrowdControllButCanCleanse).FirstOrDefault(buff =>
                               (float)buff.StartTime + (float)(ReactionDelay / 1000f) < gameTime && buff.DurationMs < 10_000 &&
                                buff.DurationMs >= AutoMikaelsBlessingGroup.GetItem<Counter>(x => x.Title.Contains(buff.EntryType.ToString(), StringComparison.OrdinalIgnoreCase))?.Value);
                //return cc != null;

                if (cc != null && !cc.Name.Contains("Unknown", StringComparison.OrdinalIgnoreCase))
                {
                    if (LogMikaelsBuff)
                    {
                        Logger.Log($"[AutoMikaelsBlessing] {cc.Name} {cc.IsActive} {cc.DurationMs} {cc.EntryType}");
                    }

                    return true;
                }

                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
