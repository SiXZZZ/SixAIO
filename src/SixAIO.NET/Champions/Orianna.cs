using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Extensions;
using Oasys.Common.GameObject;
using Oasys.Common.GameObject.ObjectClass;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SharpDX;
using SixAIO.Helpers;
using SixAIO.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SixAIO.Champions
{
    internal class Orianna : Champion
    {
        private static GameObjectBase Ball { get; set; }

        private static bool IsBallOnMe()
        {
            var buff = UnitManager.MyChampion.BuffManager.GetBuffByName("orianaghostself", false, true);
            return buff != null && buff.IsActive && buff.Stacks > 0;
        }

        private static Vector3 GetBallPosition() => IsBallOnMe()
                                                    ? UnitManager.MyChampion.Position
                                                    : Ball?.Position ?? Vector3.Zero;

        public Orianna()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => QHitChance,
                From = GetBallPosition,
                Delay = () => 0.1f,
                Range = () => 800,
                Speed = () => 1400,
                Radius = () => 160,
                IsEnabled = () => UseQ,
                MinimumMana = () => QMinMana,
                TargetSelect = (mode) => SpellQ.GetTargets(mode).FirstOrDefault()
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                Delay = () => 0f,
                IsEnabled = () => UseW,
                MinimumMana = () => WMinMana,
                ShouldCast = (mode, target, spellClass, damage) =>
                            UnitManager.EnemyChampions.Any(enemy => enemy.IsAlive && enemy.DistanceTo(GetBallPosition()) < 200 && TargetSelector.IsAttackable(enemy)) ||
                            (WSpeedAlly && UnitManager.AllyChampions.Any(ally => ally.IsAlive && ally.DistanceTo(GetBallPosition()) < 200 && TargetSelector.IsAttackable(ally, false))),
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                Delay = () => 0f,
                IsEnabled = () => UseE,
                MinimumMana = () => EMinMana,
                TargetSelect = (mode) =>
                {
                    Hero target = null;

                    if (target == null && EShieldAlly)
                    {
                        target = UnitManager.AllyChampions
                        .Where(ally => MenuTab.GetItem<Counter>("Buff Ally Prio- " + ally.ModelName).Value > 0)
                        .OrderByDescending(ally => MenuTab.GetItem<Counter>("Buff Ally Prio- " + ally.ModelName).Value)
                        .FirstOrDefault(ally => ally.IsAlive && ally.Distance <= 1100 && TargetSelector.IsAttackable(ally, false) &&
                                        (ally.Health / ally.MaxHealth * 100) < EShieldHealthPercent);
                    }

                    if (target == null)
                    {

                        target = UnitManager.AllyChampions.FirstOrDefault(ally => ally.IsAlive && ally.Distance <= 1100 && TargetSelector.IsAttackable(ally, false) &&
                                                                            EIfMoreThanEnemiesNear < UnitManager.EnemyChampions.Count(x => TargetSelector.IsAttackable(x) &&
                                                                                                                        x.DistanceTo(GetBallPosition()) < EEnemiesCloserThan));
                    }

                    return target;
                }
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                Delay = () => 0.5f,
                IsEnabled = () => UseR,
                MinimumMana = () => RMinMana,
                ShouldCast = (mode, target, spellClass, damage) =>
                            RUltEnemies <= UnitManager.EnemyChampions
                                            .Count(x => x.IsAlive && x.DistanceTo(GetBallPosition()) < 400 &&
                                                        TargetSelector.IsAttackable(x) && !TargetSelector.IsInvulnerable(x, Oasys.Common.Logic.DamageType.Magical, false)),
            };
        }

        internal override void OnCoreMainInput()
        {
            if (SpellR.ExecuteCastSpell() || SpellE.ExecuteCastSpell() || SpellW.ExecuteCastSpell() || SpellQ.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void OnCoreRender()
        {
#if DEBUG
            if (Ball != null && !Ball.W2S.IsZero)
            {
                Oasys.SDK.Rendering.RenderFactory.DrawText("Orianna Ball", 12, GetBallPosition().ToW2S(), Color.Blue);
            }
#endif
        }

        internal override void OnCoreMainTick()
        {
            if (Ball == null || !Ball.IsAlive || Ball.Health < 1)
            {
                Ball = UnitManager.AllNativeObjects.FirstOrDefault(x => x.Name == "TheDoomBall" && x.IsAlive && x.Health >= 1);
            }
        }

        private bool WSpeedAlly
        {
            get => WSettings.GetItem<Switch>("W Speed ally").IsOn;
            set => WSettings.GetItem<Switch>("W Speed ally").IsOn = value;
        }

        private bool EShieldAlly
        {
            get => ESettings.GetItem<Switch>("E Shield ally").IsOn;
            set => ESettings.GetItem<Switch>("E Shield ally").IsOn = value;
        }

        private int EShieldHealthPercent
        {
            get => ESettings.GetItem<Counter>("E Shield Health Percent").Value;
            set => ESettings.GetItem<Counter>("E Shield Health Percent").Value = value;
        }

        private int EIfMoreThanEnemiesNear
        {
            get => ESettings.GetItem<Counter>("E If More Than Enemies Near").Value;
            set => ESettings.GetItem<Counter>("E If More Than Enemies Near").Value = value;
        }

        private int EEnemiesCloserThan
        {
            get => ESettings.GetItem<Counter>("E Enemies Closer Than").Value;
            set => ESettings.GetItem<Counter>("E Enemies Closer Than").Value = value;
        }

        private int RUltEnemies
        {
            get => RSettings.GetItem<Counter>("R Ult enemies").Value;
            set => RSettings.GetItem<Counter>("R Ult enemies").Value = value;
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Orianna)}"));

            MenuTab.AddItem(new InfoDisplay() { Title = "---Allies to buff - 0 to disable---" });
            foreach (var allyChampion in UnitManager.AllyChampions.Where(x => !x.IsTargetDummy))
            {
                MenuTab.AddItem(new Counter() { Title = "Buff Ally Prio- " + allyChampion.ModelName, MinValue = 0, MaxValue = 5, Value = 0 });
            }

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new Counter() { Title = "Q Min Mana", MinValue = 0, MaxValue = 500, Value = 70, ValueFrequency = 10 });
            QSettings.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });
            WSettings.AddItem(new Counter() { Title = "W Min Mana", MinValue = 0, MaxValue = 500, Value = 65, ValueFrequency = 10 });
            WSettings.AddItem(new Switch() { Title = "W Speed ally", IsOn = false });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });
            ESettings.AddItem(new Counter() { Title = "E Min Mana", MinValue = 0, MaxValue = 500, Value = 80, ValueFrequency = 10 });
            ESettings.AddItem(new Switch() { Title = "E Shield ally", IsOn = true });
            ESettings.AddItem(new Counter() { Title = "E Shield Health Percent", MinValue = 0, MaxValue = 100, Value = 70, ValueFrequency = 5 });
            ESettings.AddItem(new Counter() { Title = "E If More Than Enemies Near", MinValue = 0, MaxValue = 5, Value = 2, ValueFrequency = 1 });
            ESettings.AddItem(new Counter() { Title = "E Enemies Closer Than", MinValue = 50, MaxValue = 600, Value = 400, ValueFrequency = 25 });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            RSettings.AddItem(new Counter() { Title = "R Min Mana", MinValue = 0, MaxValue = 500, Value = 100, ValueFrequency = 10 });
            RSettings.AddItem(new Counter() { Title = "R Ult enemies", MinValue = 1, MaxValue = 5, Value = 2, ValueFrequency = 1 });

        }
    }
}
