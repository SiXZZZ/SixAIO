using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SixAIO.Models;
using System;
using System.Linq;
using Oasys.Common.Menu;
using Oasys.SDK;
using Oasys.Common.GameObject;

namespace SixAIO.Champions
{
    internal sealed class Sett : Champion
    {
        public Sett()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                IsSpellReady = (spellClass, minMana, minCharges) => spellClass.IsSpellReady,
                IsEnabled = () => UseQ,
                ShouldCast = (mode, target, spellClass, damage) =>
                            !Orbwalker.CanBasicAttack &&
                            !UnitManager.MyChampion.BuffManager.ActiveBuffs.Any(x => x.Name == "SettQ" && x.Stacks >= 1) &&
                            TargetSelector.IsAttackable(Orbwalker.TargetHero) &&
                            TargetSelector.IsInRange(Orbwalker.TargetHero),
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                IsSpellReady = (spellClass, minMana, minCharges) => spellClass.IsSpellReady,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => WHitChance,
                MinimumMana = () => WMinimumGrit,
                Delay = () => 0.75f,
                Range = () => 700,
                Radius = () => 280,
                Speed = () => 5000,
                IsEnabled = () => UseW,
                TargetSelect = (mode) => SpellW.GetTargets(mode, x => x.Health <= WDamage(x)).FirstOrDefault(),
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                IsSpellReady = (spellClass, minMana, minCharges) => spellClass.IsSpellReady,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => EHitChance,
                Range = () => 420,
                Radius = () => 280,
                Speed = () => 5000,
                IsEnabled = () => UseE,
                TargetSelect = (mode) => SpellE.GetTargets(mode).FirstOrDefault(),
            };
        }

        private float WDamage(GameObjectBase target)
        {
            var result = 0f;

            if (target is not null)
            {
                var baseDamage = 60 + SpellW.SpellClass.Level * 20;
                var grit = UnitManager.MyChampion.Mana;
                var gritScale = 0.25f + (0.25f * UnitManager.MyChampion.UnitStats.BonusAttackDamage / 100);
                var gritDamage = grit * gritScale;
                var damage = gritDamage + baseDamage;
                result = DamageCalculator.CalculateActualDamage(UnitManager.MyChampion, target, damage);
            }

            return result;
        }

        internal override void OnCoreMainInput()
        {
            if (SpellQ.ExecuteCastSpell() || SpellE.ExecuteCastSpell() || SpellW.ExecuteCastSpell())
            {
                return;
            }
        }

        private int WMinimumGrit
        {
            get => WSettings.GetItem<Counter>("W Minimum Grit").Value;
            set => WSettings.GetItem<Counter>("W Minimum Grit").Value = value;
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Sett)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("E Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });
            WSettings.AddItem(new ModeDisplay() { Title = "W HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });
            WSettings.AddItem(new Counter() { Title = "W Minimum Grit", MinValue = 0, MaxValue = 2000, Value = 0, ValueFrequency = 25 });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });
            ESettings.AddItem(new ModeDisplay() { Title = "E HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

        }
    }
}
