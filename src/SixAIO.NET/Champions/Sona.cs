using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Extensions;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SixAIO.Enums;
using SixAIO.Helpers;
using SixAIO.Models;
using System;
using System.Linq;

namespace SixAIO.Champions
{
    internal class Sona : Champion
    {
        public Sona()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                Delay = () => 0f,
                ShouldCast = (target, spellClass, damage) =>
                            UseQ &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 70 &&
                            UnitManager.EnemyChampions.FirstOrDefault(x => x.Distance <= 800 && x.IsAlive && TargetSelector.IsAttackable(x)) != null,
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                Delay = () => 0f,
                ShouldCast = (target, spellClass, damage) =>
                            UseW &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 40 &&
                            UnitManager.MyChampion.Mana > WMinMana &&
                            UnitManager.AllyChampions.FirstOrDefault(x => x.Distance <= 800 && x.IsAlive && (x.Health / x.MaxHealth * 100) < WBuffHealthPercent) != null,
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                Delay = () => 0f,
                ShouldCast = (target, spellClass, damage) =>
                            UseE &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 65 &&
                            UnitManager.AllyChampions.FirstOrDefault(x => x.Distance <= 400 && x.IsAlive) != null,
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                Range = () => 1000,
                Radius = () => 280,
                Speed = () => 2400,
                ShouldCast = (target, spellClass, damage) =>
                            UseR &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 100 &&
                            target != null,
                TargetSelect = (mode) => 
                            UnitManager.EnemyChampions
                            .FirstOrDefault(x => x.Distance <= 850 && x.IsAlive &&
                                                 TargetSelector.IsAttackable(x) && BuffChecker.IsCrowdControlledOrSlowed(x) &&
                                                 !TargetSelector.IsInvulnerable(x, Oasys.Common.Logic.DamageType.Magical, false))
            };
        }

        internal override void OnCoreMainInput()
        {
            if (SpellQ.ExecuteCastSpell() || SpellW.ExecuteCastSpell() || SpellE.ExecuteCastSpell() || SpellR.ExecuteCastSpell())
            {
                return;
            }
        }

        private int WBuffHealthPercent
        {
            get => MenuTab.GetItem<Counter>("W Buff Health Percent").Value;
            set => MenuTab.GetItem<Counter>("W Buff Health Percent").Value = value;
        }

        private int WMinMana
        {
            get => MenuTab.GetItem<Counter>("W Min Mana").Value;
            set => MenuTab.GetItem<Counter>("W Min Mana").Value = value;
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Sona)}"));
            MenuTab.AddItem(new InfoDisplay() { Title = "---Q Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use Q", IsOn = true });

            MenuTab.AddItem(new InfoDisplay() { Title = "---W Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use W", IsOn = true });
            MenuTab.AddItem(new Counter() { Title = "W Min Mana", MinValue = 0, MaxValue = 500, Value = 100, ValueFrequency = 10 });
            MenuTab.AddItem(new Counter() { Title = "W Buff Health Percent", MinValue = 0, MaxValue = 100, Value = 50, ValueFrequency = 5 });

            MenuTab.AddItem(new InfoDisplay() { Title = "---E Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use E", IsOn = true });

            MenuTab.AddItem(new InfoDisplay() { Title = "---R Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use R", IsOn = true });
        }
    }
}
