﻿using Oasys.Common.Enums.GameEnums;
using Oasys.Common.GameObject;
using Oasys.Common.GameObject.ObjectClass;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SixAIO.Enums;
using SixAIO.Extensions;
using SixAIO.Models;
using System;
using System.Linq;
using System.Windows.Forms;

namespace SixAIO.Champions
{
    internal sealed class Zeri : Champion
    {
        public Zeri()
        {
            Oasys.SDK.InputProviders.KeyboardProvider.OnKeyPress += KeyboardProvider_OnKeyPress;
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                ShouldDraw = () => DrawQRange,
                DrawColor = () => DrawQColor,
                //AllowCollision = (target, collisions) => !collisions.Any(),
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => QHitChance,
                Range = () => UnitManager.MyChampion.TrueAttackRange + 185,
                Radius = () => 80,
                Speed = () => 2600,
                Delay = () => 0f,
                IsEnabled = () => UseQ,
                IsSpellReady = (spellClass, minMana, minCharges) => spellClass.IsSpellReady,
                TargetSelect = (mode) =>
                {
                    var target = SpellQ.GetTargets(mode, x => TargetSelector.ShouldAttackMinion(x)).FirstOrDefault();
                    if (mode != Orbwalker.OrbWalkingModeType.Combo)
                    {
                        if (target is null)
                        {
                            target = UnitManager.EnemyTowers.FirstOrDefault(x => TargetSelector.IsAttackable(x) && x.Distance <= SpellQ.Range());
                        }
                        if (target is null)
                        {
                            target = UnitManager.EnemyInhibitors.FirstOrDefault(x => TargetSelector.IsAttackable(x) && x.Distance <= SpellQ.Range());
                        }
                        if (target is null && TargetSelector.IsAttackable(UnitManager.EnemyNexus) && UnitManager.EnemyNexus.Distance <= SpellQ.Range())
                        {
                            target = UnitManager.EnemyNexus;
                        }
                    }
                    if (mode == Orbwalker.OrbWalkingModeType.Combo)
                    {
                        return SpellQ.GetTargets(mode).FirstOrDefault();
                    }
                    else if (mode == Orbwalker.OrbWalkingModeType.LastHit)
                    {
                        return SpellQ.GetTargets(mode, x => x.Health <= GetQDamage(x)).FirstOrDefault();
                    }
                    else if (mode == Orbwalker.OrbWalkingModeType.Mixed)
                    {
                        return SpellQ.GetTargets(mode, x => x.IsObject(ObjectTypeFlag.AIHeroClient) || x.Health <= GetQDamage(x)).FirstOrDefault();
                    }

                    return target;
                }
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                ShouldDraw = () => DrawWRange,
                DrawColor = () => DrawWColor,
                AllowCollision = (target, collisions) => !collisions.Any(),
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => WHitChance,
                Range = () => 1200,
                Radius = () => 80,
                Speed = () => 2500,
                IsEnabled = () => UseW,
                TargetSelect = (mode) => SpellW.GetTargets(mode).FirstOrDefault()
            };
            SpellWSemiAuto = new Spell(CastSlot.W, SpellSlot.W)
            {
                AllowCollision = (target, collisions) => !collisions.Any(),
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => SemiAutoWHitChance,
                Range = () => 2700,
                Radius = () => 200,
                Speed = () => 2500,
                IsEnabled = () => UseSemiAutoW,
                TargetSelect = (mode) => SpellWSemiAuto.GetTargets(mode).OrderBy(x => x.Distance).FirstOrDefault()
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                Delay = () => 0f,
                IsEnabled = () => UseE,
                ShouldCast = (mode, target, spellClass, damage) =>
                            DashModeSelected == DashMode.ToMouse &&
                            UnitManager.EnemyChampions.Any(x => x.Distance <= x.TrueAttackRange + 500 && x.IsAlive && TargetSelector.IsAttackable(x)),
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                ShouldDraw = () => DrawRRange,
                DrawColor = () => DrawRColor,
                IsEnabled = () => UseR,
                Range = () => REnemiesCloserThan,
                ShouldCast = (mode, target, spellClass, damage) => UnitManager.EnemyChampions.Count(x => TargetSelector.IsAttackable(x) && x.Distance < REnemiesCloserThan) > RIfMoreThanEnemiesNear,
            };
        }

        private float GetQDamage(GameObjectBase target)
        {
            if (target == null)
            {
                return 0;
            }
            var baseDmg = 12 + SpellQ.SpellClass.Level * 3;
            var scaleDmg = UnitManager.MyChampion.UnitStats.TotalAttackDamage * (1f + SpellQ.SpellClass.Level * 0.05f);
            return DamageCalculator.CalculateActualDamage(UnitManager.MyChampion, target, baseDmg + scaleDmg);
        }

        private void KeyboardProvider_OnKeyPress(Keys keyBeingPressed, Oasys.Common.Tools.Devices.Keyboard.KeyPressState pressState)
        {
            if (keyBeingPressed == SemiAutoWKey && pressState == Oasys.Common.Tools.Devices.Keyboard.KeyPressState.Down)
            {
                SpellWSemiAuto.ExecuteCastSpell();
            }
        }

        private static bool IsQActive()
        {
            var buff = UnitManager.MyChampion.BuffManager.GetBuffByName("zeriqpassiveready", false, true);
            return buff != null && buff.IsActive && buff.Stacks >= 1;
        }

        internal override void OnCoreMainInput()
        {
            if (OnlyBasicAttackOnChampions)
            {
                Orbwalker.AllowAttacking = true;
            }
            if (OnlyBasicAttackOnFullCharge)
            {
                Orbwalker.AllowAttacking = IsQActive();
            }
            if (SpellQ.ExecuteCastSpell() || SpellW.ExecuteCastSpell() || SpellE.ExecuteCastSpell() || SpellR.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void OnCoreLaneClearInput()
        {
            if (OnlyBasicAttackOnChampions)
            {
                Orbwalker.AllowAttacking = false;
            }

            if (UseQLaneclear && SpellQ.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.LaneClear))
            {
                return;
            }
        }

        internal override void OnCoreHarassInput()
        {
            if (UseQHarass && SpellQ.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.Mixed))
            {
                return;
            }
        }

        internal override void OnCoreLastHitInput()
        {
            if (UseQLasthit && SpellQ.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.LastHit))
            {
                return;
            }
        }

        internal override void OnCoreRender()
        {
            SpellQ.DrawRange();
            SpellW.DrawRange();
            SpellR.DrawRange();
        }

        private bool OnlyBasicAttackOnFullCharge
        {
            get => BasicAttackSettings.GetItem<Switch>("Only basic attack on full charge").IsOn;
            set => BasicAttackSettings.GetItem<Switch>("Only basic attack on full charge").IsOn = value;
        }

        private bool OnlyBasicAttackOnChampions
        {
            get => BasicAttackSettings.GetItem<Switch>("Only basic attack on champions").IsOn;
            set => BasicAttackSettings.GetItem<Switch>("Only basic attack on champions").IsOn = value;
        }

        private DashMode DashModeSelected
        {
            get => (DashMode)Enum.Parse(typeof(DashMode), ESettings.GetItem<ModeDisplay>("E Dash Mode").SelectedModeName);
            set => ESettings.GetItem<ModeDisplay>("E Dash Mode").SelectedModeName = value.ToString();
        }

        private int RIfMoreThanEnemiesNear
        {
            get => RSettings.GetItem<Counter>("R If More Than Enemies Near").Value;
            set => RSettings.GetItem<Counter>("R If More Than Enemies Near").Value = value;
        }

        private int REnemiesCloserThan
        {
            get => RSettings.GetItem<Counter>("R Enemies Closer Than").Value;
            set => RSettings.GetItem<Counter>("R Enemies Closer Than").Value = value;
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Zeri)}"));
            MenuTab.AddGroup(new Group("Basic Attack Settings"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            BasicAttackSettings.AddItem(new Switch() { Title = "Only basic attack on full charge", IsOn = true });
            BasicAttackSettings.AddItem(new Switch() { Title = "Only basic attack on champions", IsOn = true });

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Use Q Laneclear", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Use Q Harass", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Use Q Lasthit", IsOn = true });
            QSettings.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });
            WSettings.AddItem(new ModeDisplay() { Title = "W HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            WSettings.AddItem(new Switch() { Title = "Use Semi Auto W", IsOn = true });
            WSettings.AddItem(new KeyBinding() { Title = "Semi Auto W Key", SelectedKey = Keys.T });
            WSettings.AddItem(new ModeDisplay() { Title = "Semi Auto W HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = false });
            ESettings.AddItem(new ModeDisplay() { Title = "E Dash Mode", ModeNames = DashHelper.ConstructDashModeTable(), SelectedModeName = "ToMouse" });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            RSettings.AddItem(new Counter() { Title = "R If More Than Enemies Near", MinValue = 0, MaxValue = 5, Value = 2, ValueFrequency = 1 });
            RSettings.AddItem(new Counter() { Title = "R Enemies Closer Than", MinValue = 50, MaxValue = 850, Value = 750, ValueFrequency = 50 });


            MenuTab.AddDrawOptions(SpellSlot.Q, SpellSlot.W, SpellSlot.R);
        }
    }
}
