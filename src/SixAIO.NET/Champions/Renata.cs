using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Extensions;
using Oasys.Common.GameObject.ObjectClass;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SixAIO.Extensions;
using SixAIO.Models;
using System;
using System.Linq;

namespace SixAIO.Champions
{
    internal sealed class Renata : Champion
    {
        public Renata()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                ShouldDraw = () => DrawQRange,
                DrawColor = () => DrawQColor,
                AllowCollision = (target, collisions) => !collisions.Any(),
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => QHitChance,
                Range = () => 900,
                Radius = () => 140,
                Speed = () => 1450,
                IsEnabled = () => UseQ,
                TargetSelect = (mode) => SpellQ.GetTargets(mode).FirstOrDefault()
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                ShouldDraw = () => DrawWRange,
                DrawColor = () => DrawWColor,
                IsTargetted = () => true,
                IsEnabled = () => UseW,
                Range = () => 800,
                TargetSelect = (mode) => UnitManager.AllyChampions.FirstOrDefault(ally => ally.IsAlive && ally.Distance <= 800 && TargetSelector.IsAttackable(ally, false) &&
                                                                    AnyEnemyIsCastingSpell(ally) &&
                                                                    ally.HealthPercent < WShieldHealthPercent)
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                ShouldDraw = () => DrawERange,
                DrawColor = () => DrawEColor,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Circle,
                MinimumHitChance = () => EHitChance,
                Range = () => 800,
                Radius = () => 220,
                Speed = () => 1450,
                IsEnabled = () => UseE,
                TargetSelect = (mode) => SpellE.GetTargets(mode).FirstOrDefault()
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                ShouldDraw = () => DrawRRange,
                DrawColor = () => DrawRColor,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => RHitChance,
                Range = () => 2000,
                Radius = () => 500,
                Speed = () => 700,
                Delay = () => 0.75f,
                IsEnabled = () => UseR,
                TargetSelect = (mode) => SpellR.GetTargets(mode, x => RIfMoreThanEnemiesNear <= UnitManager.EnemyChampions.Count(enemy =>
                                                                      TargetSelector.IsAttackable(enemy) && enemy.Distance(x) < REnemiesCloserThan))
                                                .FirstOrDefault()
            };
        }

        private bool AnyEnemyIsCastingSpell(Hero ally)
        {
            return UnitManager.EnemyChampions.Any(x => x.IsAlive && IsCastingSpellOnAlly(x, ally));
        }

        private bool IsCastingSpellOnAlly(Hero enemy, Hero ally)
        {
            if (enemy.IsCastingSpell)
            {
                var spell = enemy.GetCurrentCastingSpell();
                if (spell != null && spell.Targets.Any(x => ally.NetworkID == x.NetworkID))
                {
                    return true;
                }
            }

            return false;
        }

        internal override void OnCoreRender()
        {
            SpellQ.DrawRange();
            SpellW.DrawRange();
            SpellE.DrawRange();
            SpellR.DrawRange();
        }

        internal override void OnCoreMainInput()
        {
            SpellQ.ExecuteCastSpell();
            SpellW.ExecuteCastSpell();
            SpellE.ExecuteCastSpell();
            SpellR.ExecuteCastSpell();
        }

        private int WShieldHealthPercent
        {
            get => WSettings.GetItem<Counter>("W Health Percent").Value;
            set => WSettings.GetItem<Counter>("W Health Percent").Value = value;
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
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Renata)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });
            WSettings.AddItem(new Counter() { Title = "W Health Percent", MinValue = 0, MaxValue = 100, Value = 15, ValueFrequency = 5 });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });
            ESettings.AddItem(new ModeDisplay() { Title = "E HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            RSettings.AddItem(new ModeDisplay() { Title = "R HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "Immobile" });
            RSettings.AddItem(new Counter() { Title = "R x >= Enemies Near Target", MinValue = 0, MaxValue = 5, Value = 1, ValueFrequency = 1 });
            RSettings.AddItem(new Counter() { Title = "R Enemies Closer Than", MinValue = 25, MaxValue = 800, Value = 200, ValueFrequency = 25 });


            MenuTab.AddDrawOptions(SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R);

        }
    }
}
