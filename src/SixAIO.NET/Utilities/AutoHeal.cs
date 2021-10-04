using Oasys.Common.Enums.GameEnums;
using Oasys.Common.EventsProvider;
using Oasys.Common.GameObject.Clients.ExtendedInstances.Spells;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using Oasys.SDK.Tools;
using System;
using System.Threading.Tasks;

namespace SixAIO.Utilities
{
    internal class AutoHeal
    {
        public static SpellClass SmiteKey;
        public static CastSlot HealSlot;

        private static Tab _menuTab => MenuManagerProvider.GetTab($"SIXAIO - Auto Heal");

        private static bool UseHeal
        {
            get => _menuTab.GetItem<Switch>("Use Heal").IsOn;
            set => _menuTab.GetItem<Switch>("Use Heal").IsOn = value;
        }

        private static bool HealOnCombo
        {
            get => _menuTab?.GetItem<Switch>("Heal On Combo")?.IsOn ?? false;
            set => _menuTab.GetItem<Switch>("Heal On Combo").IsOn = value;
        }

        private static bool HealOnTick
        {
            get => _menuTab?.GetItem<Switch>("Heal On Tick")?.IsOn ?? false;
            set => _menuTab.GetItem<Switch>("Heal On Tick").IsOn = value;
        }

        internal static Task GameEvents_OnGameLoadComplete()
        {
            var spellBook = UnitManager.MyChampion.GetSpellBook();
            if (SummonerSpellsProvider.IHaveSpellOnSlot(SummonerSpellsEnum.Heal, SummonerSpellSlot.First))
            {
                SmiteKey = spellBook.GetSpellClass(SpellSlot.Summoner1);
                HealSlot = CastSlot.Summoner1;
            }
            else if (SummonerSpellsProvider.IHaveSpellOnSlot(SummonerSpellsEnum.Heal, SummonerSpellSlot.Second))
            {
                SmiteKey = spellBook.GetSpellClass((SpellSlot)5);
                HealSlot = CastSlot.Summoner2;
            }
            else
            {
                CoreEvents.OnCoreMainTick -= InputHandler;
                CoreEvents.OnCoreLaneclearInputAsync -= InputHandler;
                CoreEvents.OnCoreLasthitInputAsync -= InputHandler;
                return Task.CompletedTask;
            }

            MenuManager.AddTab(new Tab($"SIXAIO - Auto Heal"));
            _menuTab.AddItem(new Switch() { Title = "Use Heal", IsOn = true });

            return Task.CompletedTask;
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

        private static bool ShouldUseHeal()
        {
            return false;
        }
    }
}
