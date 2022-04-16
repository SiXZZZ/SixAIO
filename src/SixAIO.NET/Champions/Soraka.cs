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
    internal class Soraka : Champion
    {
        public Soraka()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Circle,
                MinimumHitChance = () => QHitChance,
                Range = () => 800,
                Speed = () => 800,
                Radius = () => 240,
                IsEnabled = () => UseQ,
                TargetSelect = (mode) => SpellQ.GetTargets(mode).FirstOrDefault()
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                IsTargetted = () => true,
                IsEnabled = () => UseW,
                TargetSelect = (mode) => UnitManager.AllyChampions.Where(x => !x.IsTargetDummy && !x.IsMe)
                                        .OrderByDescending(x => WSettings.GetItem<Counter>("Heal Ally Prio- " + x.ModelName).Value)
                                        .FirstOrDefault(x => x.Distance <= 550 && TargetSelector.IsAttackable(x, false) && x.HealthPercent <= WHealthPercent)
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Circle,
                MinimumHitChance = () => EHitChance,
                Range = () => 930,
                Speed = () => 10_000,
                Radius = () => 250,
                IsEnabled = () => UseE,
                TargetSelect = (mode) => SpellE.GetTargets(mode).FirstOrDefault()
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                IsEnabled = () => UseR,
                ShouldCast = (mode, target, spellClass, damage) =>
                            UnitManager.AllyChampions
                            .Where(x => !x.IsTargetDummy && RSettings?.GetItem<Switch>("Heal - " + x.ModelName)?.IsOn == true)
                            .Count(x => TargetSelector.IsAttackable(x, false) && x.HealthPercent <= RHealthPercent) >= RAlliesToHeal
            };
        }

        internal override void OnCoreMainInput()
        {
            if (SpellE.ExecuteCastSpell() || SpellQ.ExecuteCastSpell() || SpellW.ExecuteCastSpell() || SpellR.ExecuteCastSpell())
            {
                return;
            }
        }

        private int WHealthPercent
        {
            get => WSettings.GetItem<Counter>("Heal below health percent").Value;
            set => WSettings.GetItem<Counter>("Heal below health percent").Value = value;
        }

        private int RHealthPercent
        {
            get => RSettings.GetItem<Counter>("Heal below health percent").Value;
            set => RSettings.GetItem<Counter>("Heal below health percent").Value = value;
        }

        private int RAlliesToHeal
        {
            get => RSettings.GetItem<Counter>("Allies to heal").Value;
            set => RSettings.GetItem<Counter>("Allies to heal").Value = value;
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Soraka)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });
            WSettings.AddItem(new Counter() { Title = "Heal below health percent", MinValue = 0, MaxValue = 100, Value = 40, ValueFrequency = 5 });
            WSettings.AddItem(new InfoDisplay() { Title = "---Allies to heal - 0 to disable---" });
            foreach (var allyChampion in UnitManager.AllyChampions.Where(x => !x.IsTargetDummy && !x.IsMe))
            {
                WSettings.AddItem(new Counter() { Title = "Heal Ally Prio- " + allyChampion.ModelName, MinValue = 0, MaxValue = 5, Value = 1, ValueFrequency = 1 });
            }

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });
            ESettings.AddItem(new ModeDisplay() { Title = "E HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            RSettings.AddItem(new Counter() { Title = "Heal below health percent", MinValue = 0, MaxValue = 100, Value = 40, ValueFrequency = 5 });
            RSettings.AddItem(new Counter() { Title = "Allies to heal", MinValue = 1, MaxValue = 5, Value = 2 });
            RSettings.AddItem(new InfoDisplay() { Title = "---Allies to heal---" });
            foreach (var allyChampion in UnitManager.AllyChampions.Where(x => !x.IsTargetDummy))
            {
                RSettings.AddItem(new Switch() { Title = "Heal - " + allyChampion.ModelName, IsOn = true });
            }
        }
    }
}
