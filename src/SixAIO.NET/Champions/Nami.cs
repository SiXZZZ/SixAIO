using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Extensions;
using Oasys.Common.GameObject.Clients;
using Oasys.Common.GameObject.ObjectClass;
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
    internal sealed class Nami : Champion
    {
        internal Spell SpellW2;

        public Nami()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Circle,
                MinimumHitChance = () => QHitChance,
                Range = () => 850,
                Radius = () => 200,
                Speed = () => 5000,
                Delay = () => 0.725f,
                IsEnabled = () => UseQ,
                TargetSelect = (mode) => SpellQ.GetTargets(mode).FirstOrDefault()
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                IsTargetted = () => true,
                IsEnabled = () => UseW,
                TargetSelect = (mode) => UnitManager.AllyChampions.Where(x => !x.IsTargetDummy)
                                        .Where(x => WSettings.GetItem<Counter>("W Heal Ally Prio- " + x?.ModelName)?.Value > 0)
                                        .OrderByDescending(x => WSettings.GetItem<Counter>("W Heal Ally Prio- " + x?.ModelName)?.Value)
                                        .FirstOrDefault(x => x.Distance <= 725 && TargetSelector.IsAttackable(x, false) && x.HealthPercent <= WHealthPercent)
            };
            SpellW2 = new Spell(CastSlot.W, SpellSlot.W)
            {
                IsTargetted = () => true,
                IsEnabled = () => UseW && UseWForDamage,
                TargetSelect = (mode) =>
                {
                    var target = UnitManager.AllyChampions.FirstOrDefault(x => x.Distance <= 725 &&
                                                                               TargetSelector.IsAttackable(x, false) &&
                                                                               UnitManager.EnemyChampions.Any(enemy => enemy.DistanceTo(x.Position) <= 700));
                    if (target is null)
                    {
                        target = UnitManager.EnemyChampions.FirstOrDefault(x => x.Distance <= 725 &&
                                                                               TargetSelector.IsAttackable(x, false) &&
                                                                               UnitManager.AllyChampions.Any(enemy => enemy.DistanceTo(x.Position) <= 700));
                    }

                    return target;
                }
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                IsTargetted = () => true,
                IsEnabled = () => UseE,
                TargetSelect = (mode) => UnitManager.AllyChampions
                                            .Where(ally => ESettings.GetItem<Counter>("E Buff Ally Prio- " + ally?.ModelName)?.Value > 0)
                                            .OrderByDescending(ally => ESettings.GetItem<Counter>("E Buff Ally Prio- " + ally?.ModelName)?.Value)
                                            .FirstOrDefault(ally => ally.IsAlive && ally.Distance <= 800 && TargetSelector.IsAttackable(ally, false) && ally.IsCastingSpell)
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => RHitChance,
                Range = () => 2500,
                Radius = () => 500,
                Speed = () => 850,
                IsEnabled = () => UseR,
                TargetSelect = (mode) => SpellR.GetTargets(mode, x => RIfMoreThanEnemiesNear <= UnitManager.EnemyChampions.Count(enemy => enemy.NetworkID != x.NetworkID &&
                                                                      TargetSelector.IsAttackable(enemy) && enemy.Distance(x) < REnemiesCloserThan))
                                                .FirstOrDefault()
            };
        }

        internal override void OnCoreMainInput()
        {
            SpellE.ExecuteCastSpell();
            SpellQ.ExecuteCastSpell();
            SpellW.ExecuteCastSpell();
            SpellW2.ExecuteCastSpell();
            SpellR.ExecuteCastSpell();
        }

        internal bool UseWForDamage
        {
            get => WSettings.GetItem<Switch>("Use W For Damage").IsOn;
            set => WSettings.GetItem<Switch>("Use W For Damage").IsOn = value;
        }

        private int WHealthPercent
        {
            get => WSettings.GetItem<Counter>("W Heal below health percent").Value;
            set => WSettings.GetItem<Counter>("W Heal below health percent").Value = value;
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
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Nami)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "VeryHigh" });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });
            WSettings.AddItem(new Switch() { Title = "Use W For Damage", IsOn = true });
            WSettings.AddItem(new Counter() { Title = "W Heal below health percent", MinValue = 0, MaxValue = 100, Value = 40, ValueFrequency = 5 });
            WSettings.AddItem(new InfoDisplay() { Title = "---Allies to heal - 0 to disable---" });
            foreach (var allyChampion in UnitManager.AllyChampions.Where(x => !x.IsTargetDummy))
            {
                WSettings.AddItem(new Counter() { Title = "W Heal Ally Prio- " + allyChampion.ModelName, MinValue = 0, MaxValue = 5, Value = 1, ValueFrequency = 1 });
            }

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });
            ESettings.AddItem(new InfoDisplay() { Title = "---Allies to buff - 0 to disable---" });
            foreach (var allyChampion in UnitManager.AllyChampions.Where(x => !x.IsTargetDummy))
            {
                ESettings.AddItem(new Counter() { Title = "E Buff Ally Prio- " + allyChampion.ModelName, MinValue = 0, MaxValue = 5, Value = 0 });
            }

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            RSettings.AddItem(new ModeDisplay() { Title = "R HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "VeryHigh" });
            RSettings.AddItem(new Counter() { Title = "R x >= Enemies Near Target", MinValue = 0, MaxValue = 5, Value = 1, ValueFrequency = 1 });
            RSettings.AddItem(new Counter() { Title = "R Enemies Closer Than", MinValue = 50, MaxValue = 400, Value = 200, ValueFrequency = 50 });
        }
    }
}
