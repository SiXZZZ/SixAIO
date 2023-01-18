using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Extensions;
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
    internal sealed class Tryndamere : Champion
    {
        public Tryndamere()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                IsSpellReady = (spellClass, minMana, minCharges) => spellClass.IsSpellReady,
                Delay = () => 0f,
                IsEnabled = () => UseQ,
                ShouldCast = (mode, target, spellClass, damage) => UnitManager.MyChampion.IsAlive && UnitManager.MyChampion.HealthPercent < QHealthPercent
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                IsSpellReady = (spellClass, minMana, minCharges) => spellClass.IsSpellReady,
                IsTargetted = () => true,
                Range = () => 850,
                IsEnabled = () => UseW,
                TargetSelect = (mode) => SpellW.GetTargets(mode, x => !x.IsFacing(UnitManager.MyChampion)).FirstOrDefault()
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                IsSpellReady = (spellClass, minMana, minCharges) => spellClass.IsSpellReady,
                AllowCollision = (target, collisions) => true,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => Prediction.MenuSelected.HitChance.High,
                Range = () => 660,
                Speed = () => 1000,
                Radius = () => 225,
                Delay = () => 0,
                IsEnabled = () => UseE && Orbwalker.TargetHero is null || !TargetSelector.IsInRange(Orbwalker.TargetHero),
                TargetSelect = (mode) => SpellE.GetTargets(mode).FirstOrDefault()
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                IsSpellReady = (spellClass, minMana, minCharges) => spellClass.IsSpellReady,
                Delay = () => 0f,
                IsEnabled = () => UseR,
                ShouldCast = (mode, target, spellClass, damage) => UnitManager.MyChampion.IsAlive && UnitManager.MyChampion.HealthPercent < RHealthPercent
            };
        }

        internal override void OnCoreMainInput()
        {
            SpellQ.ExecuteCastSpell();
            SpellR.ExecuteCastSpell();
            SpellW.ExecuteCastSpell();
            SpellE.ExecuteCastSpell();
        }


        private int QHealthPercent
        {
            get => QSettings.GetItem<Counter>("Q Health Percent").Value;
            set => QSettings.GetItem<Counter>("Q Health Percent").Value = value;
        }

        private int RHealthPercent
        {
            get => RSettings.GetItem<Counter>("R Health Percent").Value;
            set => RSettings.GetItem<Counter>("R Health Percent").Value = value;
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Tryndamere)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new Counter() { Title = "Q Health Percent", MinValue = 0, MaxValue = 100, Value = 20, ValueFrequency = 5 });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });
            ESettings.AddItem(new ModeDisplay() { Title = "E HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            RSettings.AddItem(new Counter() { Title = "R Health Percent", MinValue = 0, MaxValue = 100, Value = 10, ValueFrequency = 5 });

        }
    }
}
