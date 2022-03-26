using Oasys.Common;
using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SixAIO.Helpers;
using SixAIO.Models;
using System;
using System.Linq;

namespace SixAIO.Champions
{
    internal class KogMaw : Champion
    {
        public KogMaw()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                AllowCollision = (target, collisions) => !collisions.Any(),
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => QHitChance,
                Range = () => 1200,
                Radius = () => 140,
                Speed = () => 1650,
                IsEnabled = () => UseQ,
                MinimumMana = () => QMinMana,
                TargetSelect = (mode) => SpellQ.GetTargets(mode).FirstOrDefault()
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                IsEnabled = () => UseW,
                MinimumMana = () => WMinMana,
                ShouldCast = (mode, target, spellClass, damage) => UnitManager.EnemyChampions.Any(x => x.Distance < GetAttackRangeWithWActive() && TargetSelector.IsAttackable(x))
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => EHitChance,
                Range = () => 1360,
                Radius = () => 240,
                Speed = () => 1400,
                IsEnabled = () => UseE,
                MinimumMana = () => EMinMana,
                TargetSelect = (mode) => SpellE.GetTargets(mode).FirstOrDefault()
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Circle,
                MinimumHitChance = () => RHitChance,
                Range = () => UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.R).Level * 250 + 1050,
                Radius = () => 240,
                Speed = () => 5000,
                Delay = () => 0.9f,
                Damage = (target, spellClass) =>
                            target != null
                            ? DamageCalculator.GetMagicResistMod(UnitManager.MyChampion, target) *
                                        ((200 + spellClass.Level * 150) +
                                        (UnitManager.MyChampion.UnitStats.BonusAttackDamage) +
                                        (UnitManager.MyChampion.UnitStats.TotalAbilityPower))
                            : 0,
                IsEnabled = () => UseR,
                MinimumMana = () => RMinMana,
                TargetSelect = (mode) => SpellR.GetTargets(mode, x => x.HealthPercent < RTargetMaxHPPercent).FirstOrDefault()
            };
        }

        private static float GetAttackRangeWithWActive()
        {
            return UnitManager.MyChampion.TrueAttackRange + 110 + (20 * UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.W).Level);
        }

        internal override void OnCoreMainInput()
        {
            if (SpellW.ExecuteCastSpell() || SpellQ.ExecuteCastSpell() || SpellR.ExecuteCastSpell() || SpellE.ExecuteCastSpell())
            {
                return;
            }
        }

        private int RTargetMaxHPPercent
        {
            get => RSettings.GetItem<Counter>("R Target Max HP Percent").Value;
            set => RSettings.GetItem<Counter>("R Target Max HP Percent").Value = value;
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(KogMaw)}"));
            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = false });
            QSettings.AddItem(new Counter() { Title = "Q Min Mana", MinValue = 0, MaxValue = 500, Value = 40, ValueFrequency = 10 });
            QSettings.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });
            WSettings.AddItem(new Counter() { Title = "W Min Mana", MinValue = 0, MaxValue = 500, Value = 0, ValueFrequency = 10 });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = false });
            ESettings.AddItem(new Counter() { Title = "E Min Mana", MinValue = 0, MaxValue = 500, Value = 150, ValueFrequency = 10 });
            ESettings.AddItem(new ModeDisplay() { Title = "E HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = false });
            RSettings.AddItem(new Counter() { Title = "R Min Mana", MinValue = 0, MaxValue = 500, Value = 150, ValueFrequency = 10 });
            RSettings.AddItem(new Counter() { Title = "R Target Max HP Percent", MinValue = 10, MaxValue = 100, Value = 50, ValueFrequency = 5 });
            RSettings.AddItem(new ModeDisplay() { Title = "R HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

        }
    }
}
