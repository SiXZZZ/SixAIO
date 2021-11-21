using Oasys.Common.EventsProvider;
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
        public static CastSlot CleanseSlot;

        private static Tab _menuTab => MenuManagerProvider.GetTab($"SIXAIO - Auto Cleanse");

        private static bool UseCleanse
        {
            get => _menuTab.GetItem<Switch>("Use Cleanse").IsOn;
            set => _menuTab.GetItem<Switch>("Use Cleanse").IsOn = value;
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
            var spellBook = UnitManager.MyChampion.GetSpellBook();
            if (SummonerSpellsProvider.IHaveSpellOnSlot(SummonerSpellsEnum.Cleanse, SummonerSpellSlot.First))
            {
                CleanseSlot = CastSlot.Summoner1;
            }
            else if (SummonerSpellsProvider.IHaveSpellOnSlot(SummonerSpellsEnum.Cleanse, SummonerSpellSlot.Second))
            {
                CleanseSlot = CastSlot.Summoner2;
            }
            else
            {
                CoreEvents.OnCoreMainTick -= InputHandler;
                CoreEvents.OnCoreLaneclearInputAsync -= InputHandler;
                CoreEvents.OnCoreLasthitInputAsync -= InputHandler;
                return Task.CompletedTask;
            }

            MenuManager.AddTab(new Tab($"SIXAIO - Auto Cleanse"));
            _menuTab.AddItem(new Switch() { Title = "Use Cleanse", IsOn = false });
            _menuTab.AddItem(new Switch() { Title = "Cleanse On Combo", IsOn = false });
            _menuTab.AddItem(new Switch() { Title = "Cleanse On Tick", IsOn = false });
            _menuTab.AddItem(new Counter() { Title = "Reaction Delay", Value = 100, MinValue = 0, MaxValue = 5000, ValueFrequency = 50 });

            _menuTab.AddItem(new InfoDisplay() { Title = "-Only cleanse debuffs longer than ms-" });
            _menuTab.AddItem(new Counter() { Title = "Stun", Value = 500, MinValue = 0, MaxValue = 5000, ValueFrequency = 250 });
            _menuTab.AddItem(new Counter() { Title = "Snare", Value = 500, MinValue = 0, MaxValue = 5000, ValueFrequency = 250 });
            _menuTab.AddItem(new Counter() { Title = "Blind", Value = 500, MinValue = 0, MaxValue = 5000, ValueFrequency = 250 });
            _menuTab.AddItem(new Counter() { Title = "Charm", Value = 500, MinValue = 0, MaxValue = 5000, ValueFrequency = 250 });
            _menuTab.AddItem(new Counter() { Title = "Taunt", Value = 500, MinValue = 0, MaxValue = 5000, ValueFrequency = 250 });
            _menuTab.AddItem(new Counter() { Title = "Fear", Value = 500, MinValue = 0, MaxValue = 5000, ValueFrequency = 250 });
            _menuTab.AddItem(new Counter() { Title = "Flee", Value = 500, MinValue = 0, MaxValue = 5000, ValueFrequency = 250 });
            _menuTab.AddItem(new Counter() { Title = "Sleep", Value = 1250, MinValue = 0, MaxValue = 5000, ValueFrequency = 250 });
            _menuTab.AddItem(new Counter() { Title = "Polymorph", Value = 1250, MinValue = 0, MaxValue = 5000, ValueFrequency = 250 });

            return Task.CompletedTask;
        }

        internal static Task OnCoreMainTick()
        {
            if (CleanseOnTick)
            {
                return InputHandler();
            }

            return Task.CompletedTask;
        }

        internal static Task OnCoreMainInputAsync()
        {
            if (CleanseOnCombo)
            {
                return InputHandler();
            }

            return Task.CompletedTask;
        }

        private static Task InputHandler()
        {
            if (UseCleanse && ShouldUseCleanse())
            {
                SpellCastProvider.CastSpell(CleanseSlot);
            }

            return Task.CompletedTask;
        }

        private static bool ShouldUseCleanse()
        {
            return UnitManager.MyChampion.IsAlive && TargetSelector.IsAttackable(UnitManager.MyChampion, false) && IsCrowdControlled();
        }

        private static bool IsCrowdControlled()
        {
            try
            {
                var buffs = UnitManager.MyChampion.BuffManager.GetBuffList().Where(BuffChecker.IsCrowdControlledButCanCleanse);
                var cc = buffs.FirstOrDefault(buff =>
                               (float)buff.StartTime + (float)((float)ReactionDelay / 1000f) < GameEngine.GameTime &&
                                ((buff.EndTime - buff.StartTime) * 1000f) >=
                                _menuTab.GetItem<Counter>(x => x.Title.Contains(buff.EntryType.ToString(), System.StringComparison.OrdinalIgnoreCase))?.Value);
                //return cc != null;

                if (cc != null)
                {
#if DEBUG
                    Logger.Log($"{cc.Name} {cc.IsActive} {(cc.EndTime - cc.StartTime) * 1000f} {cc.EntryType}");
#endif
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
