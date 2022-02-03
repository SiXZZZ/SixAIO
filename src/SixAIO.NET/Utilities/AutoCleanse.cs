using Oasys.Common.Enums.GameEnums;
using Oasys.Common.EventsProvider;
using Oasys.Common.GameObject.Clients.ExtendedInstances.Spells;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using Oasys.SDK.Tools;
using SixAIO.Helpers;
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

        private static Tab _menuTab => MenuManagerProvider.GetTab($"SIXAIO - Auto Cleanse");

        private static bool LogCleanseBuff
        {
            get => _menuTab.GetItem<Switch>("Log Cleanse Buff").IsOn;
            set => _menuTab.GetItem<Switch>("Log Cleanse Buff").IsOn = value;
        }

        private static bool UseCleanse
        {
            get => _menuTab.GetItem<Switch>("Use Cleanse").IsOn;
            set => _menuTab.GetItem<Switch>("Use Cleanse").IsOn = value;
        }

        private static bool UseItems
        {
            get => _menuTab.GetItem<Switch>("Use Items").IsOn;
            set => _menuTab.GetItem<Switch>("Use Items").IsOn = value;
        }

        private static bool CleanseOnCombo
        {
            get => _menuTab?.GetItem<Switch>("Cleanse On Combo")?.IsOn ?? false;
            set => _menuTab.GetItem<Switch>("Cleanse On Combo").IsOn = value;
        }

        private static bool CleanseOnTick
        {
            get => _menuTab?.GetItem<Switch>("Cleanse On Tick")?.IsOn ?? false;
            set => _menuTab.GetItem<Switch>("Cleanse On Tick").IsOn = value;
        }

        private static int ReactionDelay
        {
            get => _menuTab?.GetItem<Counter>("Reaction Delay")?.Value ?? 100;
            set => _menuTab.GetItem<Counter>("Reaction Delay").Value = value;
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

            MenuManager.AddTab(new Tab($"SIXAIO - Auto Cleanse"));
            _menuTab.AddItem(new Switch() { Title = "Log Cleanse Buff", IsOn = false });
            _menuTab.AddItem(new Switch() { Title = "Use Cleanse", IsOn = false });
            _menuTab.AddItem(new Switch() { Title = "Use Items", IsOn = false });
            _menuTab.AddItem(new Switch() { Title = "Cleanse On Combo", IsOn = false });
            _menuTab.AddItem(new Switch() { Title = "Cleanse On Tick", IsOn = false });
            _menuTab.AddItem(new Counter() { Title = "Reaction Delay", Value = 100, MinValue = 0, MaxValue = 5000, ValueFrequency = 50 });

            _menuTab.AddItem(new InfoDisplay() { Title = "-Only cleanse debuffs longer than ms-" });
            _menuTab.AddItem(new Counter() { Title = "Stun", Value = 500, MinValue = 0, MaxValue = 5000, ValueFrequency = 250 });
            _menuTab.AddItem(new Counter() { Title = "Snare", Value = 500, MinValue = 0, MaxValue = 5000, ValueFrequency = 250 });
            _menuTab.AddItem(new Counter() { Title = "Slow", Value = 5250, MinValue = 0, MaxValue = 10000, ValueFrequency = 250 });
            _menuTab.AddItem(new Counter() { Title = "Blind", Value = 500, MinValue = 0, MaxValue = 5000, ValueFrequency = 250 });
            _menuTab.AddItem(new Counter() { Title = "Charm", Value = 500, MinValue = 0, MaxValue = 5000, ValueFrequency = 250 });
            _menuTab.AddItem(new Counter() { Title = "Taunt", Value = 500, MinValue = 0, MaxValue = 5000, ValueFrequency = 250 });
            _menuTab.AddItem(new Counter() { Title = "Fear", Value = 500, MinValue = 0, MaxValue = 5000, ValueFrequency = 250 });
            _menuTab.AddItem(new Counter() { Title = "Flee", Value = 500, MinValue = 0, MaxValue = 5000, ValueFrequency = 250 });
            _menuTab.AddItem(new Counter() { Title = "Sleep", Value = 1250, MinValue = 0, MaxValue = 5000, ValueFrequency = 250 });
            _menuTab.AddItem(new Counter() { Title = "Polymorph", Value = 1250, MinValue = 0, MaxValue = 5000, ValueFrequency = 250 });


            return Task.CompletedTask;
        }

        private static void TabItem_OnTabItemChange(string tabName, TabItem tabItem)
        {
            if (tabName == "SIXAIO - Auto Cleanse")
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

        //private static int _cycles = 0;
        private static async Task OnTick()
        {
            //_cycles++;
            //if (_cycles % 10 == 0)
            {
                await InputHandler(true).ConfigureAwait(false);
            }
        }

        private static async Task OnCombo()
        {
            await InputHandler(false).ConfigureAwait(false);
        }

        private static Task InputHandler(bool callFromTick)
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
                                buff.DurationMs >= _menuTab.GetItem<Counter>(x => x.Title.Contains(buff.EntryType.ToString(), System.StringComparison.OrdinalIgnoreCase))?.Value);
                //return cc != null;

                if (cc != null && !cc.Name.Contains("Unknown", System.StringComparison.OrdinalIgnoreCase))
                {
                    if (LogCleanseBuff)
                    {
                        Logger.Log($"[AutoCleanse] {cc.Name} {cc.IsActive} {cc.DurationMs} {cc.EntryType}");
                    }
                    return true;
                }
                return false;
            }
            catch (System.Exception)
            {
                return false;
            }
        }
    }
}
