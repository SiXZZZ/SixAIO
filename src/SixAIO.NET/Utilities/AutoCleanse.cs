using Oasys.Common.Enums.GameEnums;
using Oasys.Common.EventsProvider;
using Oasys.Common.GameObject.Clients.ExtendedInstances;
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
    internal sealed class AutoCleanse
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

        private static int CleanseActionIntervalMS
        {
            get => AutoCleanseGroup?.GetItem<Counter>("Cleanse Action Interval MS")?.Value ?? 1000;
            set => AutoCleanseGroup.GetItem<Counter>("Cleanse Action Interval MS").Value = value;
        }

        private static int SummonersReactionDelay
        {
            get => AutoCleanseGroup?.GetItem<Counter>("Summoners Reaction Delay")?.Value ?? 100;
            set => AutoCleanseGroup.GetItem<Counter>("Summoners Reaction Delay").Value = value;
        }

        private static bool Exhaust
        {
            get => AutoCleanseGroup?.GetItem<Switch>("Exhaust")?.IsOn ?? false;
            set => AutoCleanseGroup.GetItem<Switch>("Exhaust").IsOn = value;
        }

        private static bool Ignite
        {
            get => AutoCleanseGroup?.GetItem<Switch>("Ignite")?.IsOn ?? false;
            set => AutoCleanseGroup.GetItem<Switch>("Ignite").IsOn = value;
        }

        private static bool RedSmite
        {
            get => AutoCleanseGroup?.GetItem<Switch>("Red Smite")?.IsOn ?? false;
            set => AutoCleanseGroup.GetItem<Switch>("Red Smite").IsOn = value;
        }

        private static bool BlueSmite
        {
            get => AutoCleanseGroup?.GetItem<Switch>("Blue Smite")?.IsOn ?? false;
            set => AutoCleanseGroup.GetItem<Switch>("Blue Smite").IsOn = value;
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
            AutoCleanseGroup.AddItem(new Counter() { Title = "Cleanse Action Interval MS", Value = 1000, MinValue = 50, MaxValue = 5000, ValueFrequency = 50 });
            AutoCleanseGroup.AddItem(new Counter() { Title = "Summoners Reaction Delay", Value = 200, MinValue = 0, MaxValue = 5000, ValueFrequency = 50 });
            AutoCleanseGroup.AddItem(new Switch() { Title = "Exhaust", IsOn = true });
            AutoCleanseGroup.AddItem(new Switch() { Title = "Ignite", IsOn = false });
            AutoCleanseGroup.AddItem(new Switch() { Title = "Blue Smite", IsOn = false });
            AutoCleanseGroup.AddItem(new Switch() { Title = "Red Smite", IsOn = false });

            AutoCleanseGroup.AddItem(new Counter() { Title = "Reaction Delay", Value = 50, MinValue = 0, MaxValue = 5000, ValueFrequency = 50 });
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
                var gameTime = GameEngine.GameTime;
                if ((CleanseOnTick && callFromTick) || CleanseOnCombo)
                {
                    if (UseCleanse && CleanseSpellClass is not null && CleanseSpellClass?.IsSpellReady == true &&
                        gameTime > _lastCleanse + CleanseActionIntervalMS / 1000f && gameTime > _lastQss + CleanseActionIntervalMS / 1000f &&
                        ShouldUseCleanse())
                    {
                        SpellCastProvider.CastSpell(CleanseCastSlot);
                        _lastCleanse = gameTime;
                    }
                    if (UseItems && gameTime > _lastCleanse + CleanseActionIntervalMS / 1000f && gameTime > _lastQss + CleanseActionIntervalMS / 1000f)
                    {
                        if (UnitManager.MyChampion.Inventory.HasItem(ItemID.Quicksilver_Sash) &&
                            UnitManager.MyChampion.Inventory.GetItemByID(ItemID.Quicksilver_Sash)?.IsReady == true &&
                                 ShouldUseCleanse(true))
                        {
                            ItemCastProvider.CastItem(ItemID.Quicksilver_Sash);
                            _lastQss = gameTime;
                        }
                        else if (UnitManager.MyChampion.Inventory.HasItem(ItemID.Silvermere_Dawn) &&
                                 UnitManager.MyChampion.Inventory.GetItemByID(ItemID.Silvermere_Dawn)?.IsReady == true &&
                                 ShouldUseCleanse(true))
                        {
                            ItemCastProvider.CastItem(ItemID.Silvermere_Dawn);
                            _lastQss = gameTime;
                        }
                        else if (UnitManager.MyChampion.Inventory.HasItem(ItemID.Mercurial_Scimitar) &&
                                 UnitManager.MyChampion.Inventory.GetItemByID(ItemID.Mercurial_Scimitar)?.IsReady == true &&
                                 ShouldUseCleanse(true))
                        {
                            ItemCastProvider.CastItem(ItemID.Mercurial_Scimitar);
                            _lastQss = gameTime;
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
            if (BuffChecker.IsKnockedUpOrBack(UnitManager.MyChampion))
            {
                return false;
            }

            return UnitManager.MyChampion.IsAlive &&
                   TargetSelector.IsAttackable(UnitManager.MyChampion, false) &&
                   (IsCrowdControlled(qss) ||
                   SummonerDebuffed(qss));
        }

        private static bool SummonerDebuffed(bool qss)
        {
            var gameTime = GameEngine.GameTime;
            return qss
                ? false
                : CheckBuff(Exhaust, "SummonerExhaust", gameTime) ||
                  CheckBuff(Ignite, "SummonerDot", gameTime) ||
                  CheckBuff(RedSmite, "itemsmitechallenge", gameTime) ||
                  CheckBuff(BlueSmite, "itemsmiteslow", gameTime);
        }

        private static bool CheckBuff(bool shouldCheck, string buffName, float gameTime)
        {
            if (shouldCheck)
            {
                var buff = UnitManager.MyChampion.BuffManager.ActiveBuffs.FirstOrDefault(x => x.Stacks >= 1 && x.IsActive && x.Name == buffName && (float)x.StartTime + (float)((float)SummonersReactionDelay / 1000f) < gameTime);
                return LogBuff(buff);
            }

            return false;
        }

        private static bool IsCrowdControlled(bool qss = false)
        {
            try
            {
                var gameTime = GameEngine.GameTime;
                var cc = UnitManager.MyChampion.BuffManager.
                    ActiveBuffs
                    .Where(qss
                           ? BuffChecker.IsCrowdControllButCanQss
                           : BuffChecker.IsCrowdControllButCanCleanse)
                    .FirstOrDefault(buff =>
                           buff.StartTime + (float)((float)ReactionDelay / 1000f) < gameTime &&
                           buff.DurationMs < 10_000 &&
                           Math.Min(buff.DurationMs, buff.RemainingDurationMs + ReactionDelay + 100) >= AutoCleanseGroup.GetItem<Counter>(x => x.Title.Contains(buff.EntryType.ToString(), StringComparison.OrdinalIgnoreCase))?.Value);

                return LogBuff(cc);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool LogBuff(BuffEntry buff)
        {
            if (buff?.Name.Contains("Unknown", StringComparison.OrdinalIgnoreCase) == false)
            {
                if (LogCleanseBuff)
                {
                    Logger.Log($"[AutoCleanse] Name:{buff.Name} - Active:{buff.IsActive} - Duration:{buff.DurationMs} - Type:{buff.EntryType} - Stacks:{buff.Stacks}");
                }

                return true;
            }

            return false;
        }
    }
}
