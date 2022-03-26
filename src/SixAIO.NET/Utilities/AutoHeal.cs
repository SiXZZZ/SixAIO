using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using System;
using System.Threading.Tasks;
using System.Linq;
using Oasys.Common.EventsProvider;

namespace SixAIO.Utilities
{
    internal class AutoHeal
    {
        public static CastSlot HealSlot;

        private static Tab Tab => MenuManagerProvider.GetTab($"SIXAIO - Utilities");
        private static Group AutoHealGroup => Tab.GetGroup("Auto Heal");

        private static bool UseHeal
        {
            get => AutoHealGroup.GetItem<Switch>("Use Heal").IsOn;
            set => AutoHealGroup.GetItem<Switch>("Use Heal").IsOn = value;
        }

        private static bool HealOnCombo
        {
            get => AutoHealGroup?.GetItem<Switch>("Heal On Combo")?.IsOn ?? false;
            set => AutoHealGroup.GetItem<Switch>("Heal On Combo").IsOn = value;
        }

        private static bool HealOnTick
        {
            get => AutoHealGroup?.GetItem<Switch>("Heal On Tick")?.IsOn ?? false;
            set => AutoHealGroup.GetItem<Switch>("Heal On Tick").IsOn = value;
        }

        internal static Task GameEvents_OnGameLoadComplete()
        {
            if (SummonerSpellsProvider.IHaveSpellOnSlot(SummonerSpellsEnum.Heal, SummonerSpellSlot.First))
            {
                HealSlot = CastSlot.Summoner1;
            }
            else if (SummonerSpellsProvider.IHaveSpellOnSlot(SummonerSpellsEnum.Heal, SummonerSpellSlot.Second))
            {
                HealSlot = CastSlot.Summoner2;
            }
            else
            {
                CoreEvents.OnCoreMainTick -= OnCoreMainTick;
                CoreEvents.OnCoreMainInputAsync -= OnCoreMainInputAsync;
                return Task.CompletedTask;
            }

            Tab.AddGroup(new Group("Auto Heal"));
            AutoHealGroup.AddItem(new Switch() { Title = "Use Heal", IsOn = true });
            AutoHealGroup.AddItem(new Switch() { Title = "Heal On Combo", IsOn = false });
            AutoHealGroup.AddItem(new Switch() { Title = "Heal On Tick", IsOn = false });

            LoadAllyHealthPercents();

            return Task.CompletedTask;
        }

        private static void LoadAllyHealthPercents()
        {
            try
            {
                AutoHealGroup.AddItem(new InfoDisplay() { Title = "-Heal ally health percent is lower than-" });
                foreach (var ally in UnitManager.AllyChampions)
                {
                    AutoHealGroup.AddItem(new Counter() { Title = "Ally - " + ally.ModelName, MinValue = 0, MaxValue = 100, Value = 20, ValueFrequency = 5 });
                }
            }
            catch (Exception)
            {
            }
        }

        internal static Task OnCoreMainTick()
        {
            if (HealOnTick)
            {
                return InputHandler();
            }

            return Task.CompletedTask;
        }

        internal static Task OnCoreMainInputAsync()
        {
            if (HealOnCombo)
            {
                return InputHandler();
            }

            return Task.CompletedTask;
        }

        private static Task InputHandler()
        {
            if (UseHeal && ShouldUseHeal())
            {
                SpellCastProvider.CastSpell(HealSlot);
            }

            return Task.CompletedTask;
        }

        private static bool IsAnyAllyLow()
        {
            try
            {
                return UnitManager.AllyChampions.Where(x => x.Distance <= 850)
                        .Any(ally =>
                            ally.IsAlive && ally.HealthPercent <= AutoHealGroup.GetItem<Counter>(item => item.Title == "Ally - " + ally.ModelName).Value);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool ShouldUseHeal()
        {
            return UnitManager.MyChampion.IsAlive &&
                   TargetSelector.IsAttackable(UnitManager.MyChampion, false) &&
                   IsAnyAllyLow();
        }
    }
}
