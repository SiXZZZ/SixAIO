using Oasys.Common.Enums.GameEnums;
using Oasys.Common.EventsProvider;
using Oasys.Common.GameObject.Clients.ExtendedInstances.Spells;
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
    internal class AutoCleanse
    {
        private static float _lastCleanse = 0f;
        private static float _lastQss = 0f;

        public static CastSlot CleanseCastSlot;
        public static SpellClass CleanseSpellClass;

        private static Tab Tab => MenuManagerProvider.GetTab("SIXAIO - Utilities");
        private static Group AutoCleanseGroup => Tab.GetGroup("Auto Cleanse");

        private static bool LogCleanseBuff
        {
            get => AutoCleanseGroup.GetItem<Switch>("Log Cleanse Buff").IsOn;
            set => AutoCleanseGroup.GetItem<Switch>("Log Cleanse Buff").IsOn = value;
        }

        private static bool UseCleanse
        {
            get => AutoCleanseGroup.GetItem<Switch>("Use Cleanse").IsOn;
            set => AutoCleanseGroup.GetItem<Switch>("Use Cleanse").IsOn = value;
        }

        private static bool UseItems
        {
            get => AutoCleanseGroup.GetItem<Switch>("Use Items").IsOn;
            set => AutoCleanseGroup.GetItem<Switch>("Use Items").IsOn = value;
        }

        private static bool CleanseOnCombo
        {
            get => AutoCleanseGroup?.GetItem<Switch>("Cleanse On Combo")?.IsOn ?? false;
            set => AutoCleanseGroup.GetItem<Switch>("Cleanse On Combo").IsOn = value;
        }

        private static bool CleanseOnTick
        {
            get => AutoCleanseGroup?.GetItem<Switch>("Cleanse On Tick")?.IsOn ?? false;
            set => AutoCleanseGroup.GetItem<Switch>("Cleanse On Tick").IsOn = value;
        }

        private static int ReactionDelay
        {
            get => AutoCleanseGroup?.GetItem<Counter>("Reaction Delay")?.Value ?? 100;
            set => AutoCleanseGroup.GetItem<Counter>("Reaction Delay").Value = value;
        }

        internal static Task GameEvents_OnGameLoadComplete()
        {
            TabItem.OnTabItemChange += TabItem_OnTabItemChange;

            var spellBook = UnitManager.MyChampion.GetSpellBook();
            if (SummonerSpellsProvider.IHaveSpellOnSlot(SummonerSpellsEnum.Cleanse, SummonerSpellSlot.First))
            {
                CleanseCastSlot = CastSlot.Summoner1;
                CleanseSpellClass = spellBook.GetSpellClass(SpellSlot.Summoner1);
            }
            else if (SummonerSpellsProvider.IHaveSpellOnSlot(SummonerSpellsEnum.Cleanse, SummonerSpellSlot.Second))
            {
                CleanseCastSlot = CastSlot.Summoner2;
                CleanseSpellClass = spellBook.GetSpellClass(SpellSlot.Summoner2);
            }

            Tab.AddGroup(new Group("Auto Cleanse"));
            AutoCleanseGroup.AddItem(new Switch() { Title = "Log Cleanse Buff", IsOn = true });
            AutoCleanseGroup.AddItem(new Switch() { Title = "Use Cleanse", IsOn = true });
            AutoCleanseGroup.AddItem(new Switch() { Title = "Use Items", IsOn = true });
            AutoCleanseGroup.AddItem(new Switch() { Title = "Cleanse On Combo", IsOn = true });
            AutoCleanseGroup.AddItem(new Switch() { Title = "Cleanse On Tick", IsOn = false });
            AutoCleanseGroup.AddItem(new Counter() { Title = "Reaction Delay", Value = 100, MinValue = 0, MaxValue = 5000, ValueFrequency = 50 });

            AutoCleanseGroup.AddItem(new InfoDisplay() { Title = "-Only cleanse debuffs longer than ms-" });
            AutoCleanseGroup.AddItem(new Counter() { Title = "Stun", Value = 500, MinValue = 0, MaxValue = 5000, ValueFrequency = 250 });
            AutoCleanseGroup.AddItem(new Counter() { Title = "Snare", Value = 500, MinValue = 0, MaxValue = 5000, ValueFrequency = 250 });
            AutoCleanseGroup.AddItem(new Counter() { Title = "Slow", Value = 5250, MinValue = 0, MaxValue = 10000, ValueFrequency = 250 });
            AutoCleanseGroup.AddItem(new Counter() { Title = "Blind", Value = 500, MinValue = 0, MaxValue = 5000, ValueFrequency = 250 });
            AutoCleanseGroup.AddItem(new Counter() { Title = "Charm", Value = 500, MinValue = 0, MaxValue = 5000, ValueFrequency = 250 });
            AutoCleanseGroup.AddItem(new Counter() { Title = "Taunt", Value = 500, MinValue = 0, MaxValue = 5000, ValueFrequency = 250 });
            AutoCleanseGroup.AddItem(new Counter() { Title = "Fear", Value = 500, MinValue = 0, MaxValue = 5000, ValueFrequency = 250 });
            AutoCleanseGroup.AddItem(new Counter() { Title = "Flee", Value = 500, MinValue = 0, MaxValue = 5000, ValueFrequency = 250 });
            AutoCleanseGroup.AddItem(new Counter() { Title = "Sleep", Value = 1250, MinValue = 0, MaxValue = 5000, ValueFrequency = 250 });
            AutoCleanseGroup.AddItem(new Counter() { Title = "Polymorph", Value = 1250, MinValue = 0, MaxValue = 5000, ValueFrequency = 250 });

            return Task.CompletedTask;
        }

        private static void TabItem_OnTabItemChange(string tabName, TabItem tabItem)
        {
            try
            {
                if (tabItem.TabName == "SIXAIO - Utilities" && tabItem.GroupName == "Auto Cleanse")
                {
                    if (tabItem.Title == "Cleanse On Tick" && tabItem is Switch itemSwitchOnTick)
                    {
                        if (itemSwitchOnTick.IsOn)
                        {
                            CoreEvents.OnCoreMainTick += OnTick;
                        }
                        else
                        {
                            CoreEvents.OnCoreMainTick -= OnTick;
                        }
                    }
                    if (tabItem.Title == "Cleanse On Combo" && tabItem is Switch itemSwitchOnCombo)
                    {
                        if (itemSwitchOnCombo.IsOn)
                        {
                            CoreEvents.OnCoreMainInputAsync += OnCombo;
                        }
                        else
                        {
                            CoreEvents.OnCoreMainInputAsync -= OnCombo;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }

        private static async Task OnTick()
        {
            await InputHandler(true).ConfigureAwait(false);
        }

        private static async Task OnCombo()
        {
            await InputHandler(false).ConfigureAwait(false);
        }

        private static Task InputHandler(bool callFromTick)
        {
            try
            {
                if ((CleanseOnTick && callFromTick) || CleanseOnCombo)
                {
                    if (UseCleanse && GameEngine.GameTime > _lastCleanse + 1f && GameEngine.GameTime > _lastQss + 1f && ShouldUseCleanse() &&
                        CleanseSpellClass is not null && CleanseSpellClass?.IsSpellReady == true)
                    {
                        SpellCastProvider.CastSpell(CleanseCastSlot);
                        _lastCleanse = GameEngine.GameTime;
                    }
                    if (UseItems && GameEngine.GameTime > _lastCleanse + 1f && GameEngine.GameTime > _lastQss + 1f && ShouldUseCleanse(true))
                    {
                        if (UnitManager.MyChampion.Inventory.HasItem(ItemID.Quicksilver_Sash) &&
                            UnitManager.MyChampion.Inventory.GetItemByID(ItemID.Quicksilver_Sash)?.IsReady == true)
                        {
                            ItemCastProvider.CastItem(ItemID.Quicksilver_Sash);
                            _lastQss = GameEngine.GameTime;
                        }
                        else if (UnitManager.MyChampion.Inventory.HasItem(ItemID.Silvermere_Dawn) &&
                                 UnitManager.MyChampion.Inventory.GetItemByID(ItemID.Silvermere_Dawn)?.IsReady == true)
                        {
                            ItemCastProvider.CastItem(ItemID.Silvermere_Dawn);
                            _lastQss = GameEngine.GameTime;
                        }
                        else if (UnitManager.MyChampion.Inventory.HasItem(ItemID.Mercurial_Scimitar) &&
                                 UnitManager.MyChampion.Inventory.GetItemByID(ItemID.Mercurial_Scimitar)?.IsReady == true)
                        {
                            ItemCastProvider.CastItem(ItemID.Mercurial_Scimitar);
                            _lastQss = GameEngine.GameTime;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }

            return Task.CompletedTask;
        }

        private static bool ShouldUseCleanse(bool qss = false)
        {
            return UnitManager.MyChampion.IsAlive &&
                   TargetSelector.IsAttackable(UnitManager.MyChampion, false) &&
                   IsCrowdControlled(qss);
        }

        private static bool IsCrowdControlled(bool qss = false)
        {
            try
            {
                var buffs = UnitManager.MyChampion.BuffManager.GetBuffList();
                if (BuffChecker.IsKnockedUpOrBack(UnitManager.MyChampion))
                {
                    return false;
                }

                var cc = buffs.Where(qss
                                    ? BuffChecker.IsCrowdControllButCanQss
                                    : BuffChecker.IsCrowdControllButCanCleanse).FirstOrDefault(buff =>
                               (float)buff.StartTime + (float)((float)ReactionDelay / 1000f) < GameEngine.GameTime && buff.DurationMs < 10_000 &&
                                buff.DurationMs >= AutoCleanseGroup.GetItem<Counter>(x => x.Title.Contains(buff.EntryType.ToString(), StringComparison.OrdinalIgnoreCase))?.Value);
                //return cc != null;

                if (cc != null && !cc.Name.Contains("Unknown", StringComparison.OrdinalIgnoreCase))
                {
                    if (LogCleanseBuff)
                    {
                        Logger.Log($"[AutoCleanse] Name:{cc.Name} - Active:{cc.IsActive} - Duration:{cc.DurationMs} - Type:{cc.EntryType} - Stacks:{cc.Stacks}");
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
