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
    internal class Zilean : Champion
    {
        public Zilean()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Circle,
                MinimumHitChance = () => QHitChance,
                Range = () => 900,
                Speed = () => 1500,
                Radius = () => 140,
                IsEnabled = () => UseQ,
                TargetSelect = (mode) => SpellQ.GetTargets(mode).OrderBy(x => x.BuffManager.HasActiveBuff("ZileanQEnemyBomb")).FirstOrDefault()
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                IsEnabled = () => UseW,
                ShouldCast = (mode, target, spellClass, damage) =>
                            !SpellQ.SpellClass.IsSpellReady &&
                            (!SpellE.SpellClass.IsSpellReady ||
                              UnitManager.EnemyChampions.Any(x => x.Distance <= 900 && x.IsAlive && x.BuffManager.HasActiveBuff("ZileanQEnemyBomb")))
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                IsTargetted = () => true,
                Range = () => 550,
                IsEnabled = () => UseE,
                TargetSelect = (mode) => SpellE.GetTargets(mode).FirstOrDefault()
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                IsTargetted = () => true,
                Delay = () => 0f,
                IsEnabled = () => UseR,
                TargetSelect = (mode) =>
                UnitManager.AllyChampions
                        .Where(ally => RSettings.GetItem<Counter>("Ult Ally - " + ally.ModelName).Value > 0)
                        .OrderByDescending(ally => RSettings.GetItem<Counter>("Ult Ally - " + ally.ModelName).Value)
                        .FirstOrDefault(ally => ally.IsAlive && ally.Distance <= 900 && TargetSelector.IsAttackable(ally, false) && ally.HealthPercent < RBuffHealthPercent)
            };
        }

        internal override void OnCoreMainInput()
        {
            if (SpellW.ExecuteCastSpell() || SpellE.ExecuteCastSpell() || SpellQ.ExecuteCastSpell() || SpellR.ExecuteCastSpell())
            {
                return;
            }
        }

        private int RBuffHealthPercent
        {
            get => RSettings.GetItem<Counter>("R Buff Health Percent").Value;
            set => RSettings.GetItem<Counter>("R Buff Health Percent").Value = value;
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Zilean)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });


            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });


            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });


            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            RSettings.AddItem(new Counter() { Title = "R Buff Health Percent", MinValue = 0, MaxValue = 100, Value = 20, ValueFrequency = 5 });
            RSettings.AddItem(new InfoDisplay() { Title = "---Allies to Ult---" });
            foreach (var allyChampion in UnitManager.AllyChampions)
            {
                RSettings.AddItem(new Counter() { Title = "Ult Ally - " + allyChampion.ModelName, MinValue = 0, MaxValue = 5, Value = 0 });
            }
        }
    }
}
