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
    internal class Caitlyn : Champion
    {
        public Caitlyn()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => QHitChance,
                Range = () => 1300,
                Radius = () => 120,
                Speed = () => 2200,
                Delay = () => 0.8f,
                Damage = (target, spellClass) =>
                            target != null
                            ? DamageCalculator.GetArmorMod(UnitManager.MyChampion, target) *
                            ((10 + spellClass.Level * 40) +
                            (UnitManager.MyChampion.UnitStats.TotalAttackDamage * (1.15f + 0.15f * spellClass.Level)))
                            : 0,
                IsEnabled = () => UseQ,
                TargetSelect = (mode) => QOnlyOnHeadshotTargets
                        ? SpellQ.GetTargets(mode, x => x.BuffManager.ActiveBuffs.Any(b => b.Stacks >= 1 && b.Name == "CaitlynWSnare" || b.Name == "CaitlynEMissile")).FirstOrDefault()
                        : SpellQ.GetTargets(mode).FirstOrDefault()
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Circle,
                MinimumHitChance = () => WHitChance,
                IsTargetted = () => true,
                Range = () => 780,
                IsEnabled = () => UseW,
                MinimumCharges = () => 1,
                TargetSelect = (mode) => SpellW.GetTargets(mode, x => BuffChecker.IsCrowdControlled(x)).FirstOrDefault()
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                AllowCollision = (target, collisions) => !collisions.Any(),
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => EHitChance,
                Range = () => 750,
                Radius = () => 140,
                Speed = () => 1600,
                IsEnabled = () => UseE && (OnlyEIfCanQ ? SpellQ.SpellClass.IsSpellReady : true),
                TargetSelect = (mode) => SpellE.GetTargets(mode, x => x.Distance <= EMaximumRange).FirstOrDefault()
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                AllowCastOnMap = () => AllowRCastOnMinimap,
                IsTargetted = () => true,
                Delay = () => 1f,
                Range = () => 3500f,
                Damage = (target, spellClass) =>
                            target != null
                            ? DamageCalculator.GetArmorMod(UnitManager.MyChampion, target) *
                            ((75 + spellClass.Level * 225) +
                            (UnitManager.MyChampion.UnitStats.BonusAttackDamage * 2f))
                            : 0,
                IsEnabled = () => UseR,
                IsSpellReady = (spellClass, minimumMana, minimumCharges) => spellClass.IsSpellReady && UnitManager.MyChampion.Mana >= spellClass.SpellData.ResourceCost && !UnitManager.EnemyChampions.Any(x => x.Distance < RSafeRange),
                ShouldCast = (mode, target, spellClass, damage) => target != null && target.Health < damage,
                TargetSelect = (mode) => SpellR.GetTargets(mode).FirstOrDefault()
            };
        }

        internal override void OnCoreMainInput()
        {
            if (SpellW.ExecuteCastSpell() || SpellE.ExecuteCastSpell() || SpellQ.ExecuteCastSpell() || SpellR.ExecuteCastSpell())
            {
                return;
            }
        }

        internal bool QOnlyOnHeadshotTargets
        {
            get => QSettings.GetItem<Switch>("Q Only On Headshot Targets").IsOn;
            set => QSettings.GetItem<Switch>("Q Only On Headshot Targets").IsOn = value;
        }

        internal bool OnlyEIfCanQ
        {
            get => ESettings.GetItem<Switch>("Only E If Can Q").IsOn;
            set => ESettings.GetItem<Switch>("Only E If Can Q").IsOn = value;
        }

        private int EMaximumRange
        {
            get => ESettings.GetItem<Counter>("E Maximum Range").Value;
            set => ESettings.GetItem<Counter>("E Maximum Range").Value = value;
        }

        private int RSafeRange
        {
            get => RSettings.GetItem<Counter>("R Safe Range").Value;
            set => RSettings.GetItem<Counter>("R Safe Range").Value = value;
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Caitlyn)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Q Only On Headshot Targets", IsOn = true });
            QSettings.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });
            WSettings.AddItem(new ModeDisplay() { Title = "W HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "Immobile" });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });
            ESettings.AddItem(new Switch() { Title = "Only E If Can Q", IsOn = false });
            ESettings.AddItem(new ModeDisplay() { Title = "E HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });
            ESettings.AddItem(new Counter() { Title = "E Maximum Range", MinValue = 0, MaxValue = 800, Value = 750, ValueFrequency = 50 });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            RSettings.AddItem(new Switch() { Title = "Allow R cast on minimap", IsOn = false });
            RSettings.AddItem(new Counter() { Title = "R Safe Range", MinValue = 0, MaxValue = 3500, Value = 1000, ValueFrequency = 50 });
        }
    }
}
