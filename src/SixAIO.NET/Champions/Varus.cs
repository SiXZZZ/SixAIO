using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Extensions;
using Oasys.Common.GameObject;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using Oasys.SDK.Tools;
using SixAIO.Models;
using System;
using System.Linq;

namespace SixAIO.Champions
{
    internal sealed class Varus : Champion
    {
        private static int WStacks<T>(T target) where T : GameObjectBase
        {
            var buff = target.BuffManager.GetBuffByName("VarusWDebuff", false, true);
            return buff == null
                ? 0
                : buff.IsActive && buff.Stacks > 0
                    ? (int)buff.Stacks
                    : 0;
        }

        public Varus()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                IsCharge = () => true,
                CastPosition = (pos) =>
                {
                    SpellW.ExecuteCastSpell();
                    return pos;
                },
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => QHitChance,
                Range = () => SpellQ.ChargeTimer.IsRunning
                                    ? SpellQ.SpellClass.IsSpellReady
                                            ? 895 + SpellQ.ChargeTimer.ElapsedMilliseconds / 1000 / 0.25f * 140
                                            : 0
                                    : 1600,
                Radius = () => 140,
                Speed = () => 1900,
                Delay = () => 0f,
                IsEnabled = () => UseQ,
                ShouldCast = (mode, target, spellClass, damage) =>
                            (SpellQ.ChargeTimer.IsRunning || UnitManager.MyChampion.Mana > 85) &&
                            target != null &&
                            (SpellQ.ChargeTimer.IsRunning ? target.Distance < SpellQ.Range() : target.Distance < 1600),
                TargetSelect = (mode) => SpellQ.GetTargets(mode, x => (QOnlyIfXGTEWStacks == 0 || WStacks(x) >= QOnlyIfXGTEWStacks)).FirstOrDefault()
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                IsEnabled = () => UseW,
                ShouldCast = (mode, target, spellClass, damage) =>
                {
                    var enemy = SpellQ.TargetSelect(mode);
                    return enemy != null && enemy.HealthPercent <= UseOnlyWIfXLTEHPPercent;
                },
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Circle,
                MinimumHitChance = () => EHitChance,
                Range = () => 925,
                Radius = () => 300,
                Speed = () => 1600,
                IsEnabled = () => UseE,
                TargetSelect = (mode) => SpellE.GetTargets(mode, x => (EOnlyIfXGTEWStacks == 0 || WStacks(x) >= EOnlyIfXGTEWStacks)).FirstOrDefault()
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => RHitChance,
                Range = () => 1370,
                Radius = () => 240,
                Speed = () => 1500,
                IsEnabled = () => UseR,
                TargetSelect = (mode) => SpellR.GetTargets(mode, x => x.HealthPercent <= UseOnlyRIfXLTEHPPercent &&
                                                                        (ROnlyIfXGTEWStacks == 0 || WStacks(x) >= ROnlyIfXGTEWStacks) &&
                                                                        RIfMoreThanEnemiesNear < UnitManager.EnemyChampions.Count(enemy =>
                                                                        TargetSelector.IsAttackable(enemy) && enemy.Distance(x) < REnemiesCloserThan))
                                                .FirstOrDefault()
            };
        }

        internal override void OnCoreMainInput()
        {
            if (SpellQ.ExecuteCastSpell() || SpellE.ExecuteCastSpell() || SpellR.ExecuteCastSpell())
            {
                return;
            }
        }

        private int QOnlyIfXGTEWStacks
        {
            get => QSettings.GetItem<Counter>("Only Q if x >= W stacks").Value;
            set => QSettings.GetItem<Counter>("Only Q if x >= W stacks").Value = value;
        }

        private int EOnlyIfXGTEWStacks
        {
            get => ESettings.GetItem<Counter>("Only E if x >= W stacks").Value;
            set => ESettings.GetItem<Counter>("Only E if x >= W stacks").Value = value;
        }

        private int ROnlyIfXGTEWStacks
        {
            get => RSettings.GetItem<Counter>("Only R if x >= W stacks").Value;
            set => RSettings.GetItem<Counter>("Only R if x >= W stacks").Value = value;
        }

        private int UseOnlyWIfXLTEHPPercent
        {
            get => WSettings.GetItem<Counter>("Use only W if x <= HP percent").Value;
            set => WSettings.GetItem<Counter>("Use only W if x <= HP percent").Value = value;
        }

        private int UseOnlyRIfXLTEHPPercent
        {
            get => RSettings.GetItem<Counter>("Use only R if x <= HP percent").Value;
            set => RSettings.GetItem<Counter>("Use only R if x <= HP percent").Value = value;
        }

        private int RIfMoreThanEnemiesNear
        {
            get => RSettings.GetItem<Counter>("R x >= Enemies Near Target").Value;
            set => RSettings.GetItem<Counter>("R x >= Enemies Near Target").Value = value;
        }

        private int REnemiesCloserThan
        {
            get => RSettings.GetItem<Counter>("R Enemies Closer Than").Value;
            set => RSettings.GetItem<Counter>("R Enemies Closer Than").Value = value;
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Varus)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });
            QSettings.AddItem(new Counter() { Title = "Only Q if x >= W stacks", MinValue = 0, MaxValue = 3, Value = 0, ValueFrequency = 1 });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });
            WSettings.AddItem(new Counter() { Title = "Use only W if x <= HP percent", MinValue = 0, MaxValue = 100, Value = 50, ValueFrequency = 5 });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });
            ESettings.AddItem(new ModeDisplay() { Title = "E HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });
            ESettings.AddItem(new Counter() { Title = "Only E if x >= W stacks", MinValue = 0, MaxValue = 3, Value = 3, ValueFrequency = 1 });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            RSettings.AddItem(new Counter() { Title = "Use only R if x <= HP percent", MinValue = 0, MaxValue = 100, Value = 50, ValueFrequency = 5 });
            RSettings.AddItem(new ModeDisplay() { Title = "R HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });
            RSettings.AddItem(new Counter() { Title = "R x >= Enemies Near Target", MinValue = 0, MaxValue = 5, Value = 1, ValueFrequency = 1 });
            RSettings.AddItem(new Counter() { Title = "R Enemies Closer Than", MinValue = 200, MaxValue = 800, Value = 600, ValueFrequency = 50 });
            RSettings.AddItem(new Counter() { Title = "Only R if x >= W stacks", MinValue = 0, MaxValue = 3, Value = 0, ValueFrequency = 1 });
        }
    }
}
