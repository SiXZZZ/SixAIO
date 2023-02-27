using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Extensions;
using Oasys.Common.GameObject;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SixAIO.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SixAIO.Champions
{
    internal sealed class Hecarim : Champion
    {
        public Hecarim()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                IsEnabled = () => UseQ,
                ShouldCast = (mode, target, spellClass, damage) =>
                {
                    if (mode == Orbwalker.OrbWalkingModeType.LaneClear && UseQLaneclear)
                    {
                        var targets = new List<GameObjectBase>() { };
                        targets.AddRange(UnitManager.EnemyChampions);
                        targets.AddRange(UnitManager.EnemyMinions);
                        targets.AddRange(UnitManager.EnemyJungleMobs);
                        return targets.Any(x => x.Distance <= 360 && TargetSelector.IsAttackable(x));
                    }

                    return UnitManager.EnemyChampions.Any(x => x.Distance <= 360 && TargetSelector.IsAttackable(x));
                }
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                IsEnabled = () => UseW,
                ShouldCast = (mode, target, spellClass, damage) => WIfMoreThanEnemiesNear <= UnitManager.EnemyChampions.Count(enemy =>
                                                                   TargetSelector.IsAttackable(enemy) && enemy.Distance < WEnemiesCloserThan)
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                IsEnabled = () => UseE,
                ShouldCast = (mode, target, spellClass, damage) => UnitManager.EnemyChampions.Any(x => x.Distance <= 350 && TargetSelector.IsAttackable(x))
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => RHitChance,
                Range = () => 1470,
                Radius = () => 460,
                Speed = () => 1100,
                IsEnabled = () => UseR,
                TargetSelect = (mode) => SpellR.GetTargets(mode, x => RIfMoreThanEnemiesNear < UnitManager.EnemyChampions.Count(enemy =>
                                                                      TargetSelector.IsAttackable(enemy) && enemy.Distance(x) < REnemiesCloserThan))
                                                .FirstOrDefault()
            };
        }

        internal override void OnCoreMainInput()
        {
            SpellE.ExecuteCastSpell();
            SpellQ.ExecuteCastSpell();
            SpellW.ExecuteCastSpell();
            SpellR.ExecuteCastSpell();
        }

        internal override void OnCoreLaneClearInput()
        {
            SpellQ.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.LaneClear);
        }

        private int WIfMoreThanEnemiesNear
        {
            get => WSettings.GetItem<Counter>("W x >= Enemies Near").Value;
            set => WSettings.GetItem<Counter>("W x >= Enemies Near").Value = value;
        }

        private int WEnemiesCloserThan
        {
            get => WSettings.GetItem<Counter>("W Enemies Closer Than").Value;
            set => WSettings.GetItem<Counter>("W Enemies Closer Than").Value = value;
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
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Hecarim)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Use Q Laneclear", IsOn = true });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });
            WSettings.AddItem(new Counter() { Title = "W x >= Enemies Near", MinValue = 0, MaxValue = 5, Value = 1, ValueFrequency = 1 });
            WSettings.AddItem(new Counter() { Title = "W Enemies Closer Than", MinValue = 50, MaxValue = 525, Value = 500, ValueFrequency = 25 });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });


            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            RSettings.AddItem(new ModeDisplay() { Title = "R HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "VeryHigh" });
            RSettings.AddItem(new Counter() { Title = "R x >= Enemies Near Target", MinValue = 1, MaxValue = 5, Value = 1, ValueFrequency = 1 });
            RSettings.AddItem(new Counter() { Title = "R Enemies Closer Than", MinValue = 50, MaxValue = 450, Value = 300, ValueFrequency = 50 });
        }
    }
}
