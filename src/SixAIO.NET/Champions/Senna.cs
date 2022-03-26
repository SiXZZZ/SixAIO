using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SixAIO.Models;
using System;
using System.Linq;

namespace SixAIO.Champions
{
    internal class Senna : Champion
    {
        public float PassiveStacks()
        {
            var passiveStacks = UnitManager.MyChampion.BuffManager.GetActiveBuff("SennaPassiveStacks");
            return passiveStacks is null ? 0 : passiveStacks.Stacks;
        }

        public Senna()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                IsTargetted = () => true,
                Delay = () => 0.6f,
                IsEnabled = () => UseQ,
                Range = () => UnitManager.MyChampion.TrueAttackRange,
                TargetSelect = (mode) => SpellQ.GetTargets(mode, x => !TargetSelector.IsInvulnerable(x, Oasys.Common.Logic.DamageType.Physical, false)).FirstOrDefault()
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                AllowCollision = (target, collisions) => false,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => WHitChance,
                Range = () => 1300,
                Radius = () => 140,
                Speed = () => 1200,
                IsEnabled = () => UseW,
                TargetSelect = (mode) => SpellW.GetTargets(mode).FirstOrDefault()
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                AllowCastOnMap = () => AllowRCastOnMinimap,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => RHitChance,
                Range = () => 30000,
                Radius = () => 320,
                Speed = () => 20000,
                Delay = () => 1f,
                Damage = (target, spellClass) =>
                            target != null
                            ? DamageCalculator.GetArmorMod(UnitManager.MyChampion, target) *
                                        ((125 + spellClass.Level * 125) +
                                        (UnitManager.MyChampion.UnitStats.BonusAttackDamage) +
                                        (UnitManager.MyChampion.UnitStats.TotalAbilityPower * 0.7f))
                            : 0,
                IsEnabled = () => UseR,
                MinimumMana = () => RMinMana,
                ShouldCast = (mode, target, spellClass, damage) => target != null && target.Health < damage,
                TargetSelect = (mode) => SpellR.GetTargets(mode, x => x.Distance > RMinimumRange && x.Distance <= RMaximumRange && x.Health <= SpellR.Damage(x, SpellR.SpellClass)).FirstOrDefault()
            };
        }

        private static void TargetSoulsWithOrbwalker()
        {
            var soul = UnitManager.EnemyJungleMobs.FirstOrDefault(x => x.ModelName == "SennaSoul" && TargetSelector.IsInRange(x));
            Orbwalker.SelectedTarget = soul;
        }

        internal override void OnCoreMainInput()
        {
            if (SpellQ.ExecuteCastSpell() || SpellW.ExecuteCastSpell() || SpellR.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void OnCoreHarassInput()
        {
            if (SpellQ.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.Mixed))
            {
                return;
            }
        }

        internal override void OnCoreLaneClearInput()
        {
            if (SpellQ.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.LaneClear))
            {
                return;
            }
        }

        internal override void OnCoreLastHitInput()
        {
            if (SpellQ.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.LastHit))
            {
                return;
            }
        }

        internal override void OnCoreMainTick()
        {
            TargetSoulsWithOrbwalker();
        }

        private int RMinimumRange
        {
            get => RSettings.GetItem<Counter>("R minimum range").Value;
            set => RSettings.GetItem<Counter>("R minimum range").Value = value;
        }

        private int RMaximumRange
        {
            get => RSettings.GetItem<Counter>("R maximum range").Value;
            set => RSettings.GetItem<Counter>("R maximum range").Value = value;
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Senna)}"));
            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });
            WSettings.AddItem(new Counter() { Title = "W Min Mana", MinValue = 0, MaxValue = 500, Value = 80, ValueFrequency = 10 });
            WSettings.AddItem(new ModeDisplay() { Title = "W HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            RSettings.AddItem(new Counter() { Title = "R Min Mana", MinValue = 0, MaxValue = 500, Value = 150, ValueFrequency = 10 });
            RSettings.AddItem(new Counter() { Title = "R Target Max HP Percent", MinValue = 10, MaxValue = 100, Value = 50, ValueFrequency = 5 });
            RSettings.AddItem(new ModeDisplay() { Title = "R HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });
            RSettings.AddItem(new Switch() { Title = "Allow R cast on minimap", IsOn = true });
            RSettings.AddItem(new Counter() { Title = "R minimum range", MinValue = 0, MaxValue = 30_000, Value = 0, ValueFrequency = 50 });
            RSettings.AddItem(new Counter() { Title = "R maximum range", MinValue = 0, MaxValue = 30_000, Value = 30_000, ValueFrequency = 50 });
        }
    }
}
