using Oasys.Common.Enums.GameEnums;
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
using System.Windows.Forms;

namespace SixAIO.Champions
{
    internal sealed class Jinx : Champion
    {
        private bool IsQActive()
        {
            var buff = UnitManager.MyChampion.BuffManager.ActiveBuffs.FirstOrDefault(x => x.Name == "JinxQ");
            return buff != null && buff.IsActive && buff.Stacks >= 1;
        }
        private bool IsUsingRockets => IsQActive();
        private bool IsUsingMinigun => !IsUsingRockets;
        private float ExtraRange => 50 + (30 * UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.Q).Level);
        private float MinigunRange => Math.Min(QMinigunMaximumRange,
                                        IsUsingRockets
                                        ? UnitManager.MyChampion.TrueAttackRange - ExtraRange
                                        : UnitManager.MyChampion.TrueAttackRange);
        private float RocketRange => Math.Max(QMinigunMaximumRange,
                                        IsUsingRockets
                                        ? UnitManager.MyChampion.TrueAttackRange
                                        : UnitManager.MyChampion.TrueAttackRange + ExtraRange);

        public Jinx()
        {
            Oasys.SDK.InputProviders.KeyboardProvider.OnKeyPress += KeyboardProvider_OnKeyPress;
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                MinimumMana = () => 20,
                IsEnabled = () => UseQ,
                ShouldCast = (mode, target, spellClass, damage) =>
                {
                    Orbwalker.SelectedTarget = null;

                    if ((!UseQLaneclear && mode == Orbwalker.OrbWalkingModeType.LaneClear) ||
                        (!UseQLasthit && mode == Orbwalker.OrbWalkingModeType.LastHit) ||
                        (!UseQHarass && mode == Orbwalker.OrbWalkingModeType.Mixed))
                    {
                        return false;
                    }

                    if (mode == Orbwalker.OrbWalkingModeType.Combo)
                    {
                        var preferRockets = Orbwalker.TargetHero == null
                                    ? QPreferRockets
                                        ? !IsUsingRockets
                                        : UnitManager.EnemyChampions.Any(x => x.Distance < RocketRange + x.BoundingRadius && TargetSelector.IsAttackable(x))
                                    : false;
                        return preferRockets || ShouldSwapGun(mode, Orbwalker.TargetHero);
                    }

                    if (mode == Orbwalker.OrbWalkingModeType.LastHit ||
                        mode == Orbwalker.OrbWalkingModeType.LaneClear ||
                        mode == Orbwalker.OrbWalkingModeType.Mixed)
                    {
                        Orbwalker.SelectedTarget = Oasys.Common.Logic.Orbwalker.GetTarget((Oasys.Common.Logic.OrbwalkingMode)mode, RocketRange);
                        return ShouldSwapGun(mode, Orbwalker.SelectedTarget);
                    }

                    return false;
                }
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                AllowCollision = (target, collisions) => !collisions.Any(),
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => WHitChance,
                Range = () => 1500,
                Radius = () => 120,
                Speed = () => 3300,
                Delay = () => 0.4f,
                MinimumMana = () => 90,
                IsEnabled = () => UseW && (!WOnlyOutsideOfAttackRange || !UnitManager.EnemyChampions.Any(TargetSelector.IsInRange)),
                TargetSelect = (mode) => SpellW.GetTargets(mode, x => x.Distance > WMinimumRange).FirstOrDefault()
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Circle,
                MinimumHitChance = () => EHitChance,
                Radius = () => 120,
                Speed = () => 1500,
                Range = () => 925,
                MinimumMana = () => 90,
                IsEnabled = () => UseE,
                TargetSelect = (mode) => EOnSelf && UnitManager.EnemyChampions.Any(x => x.Distance <= EMaximumRange && TargetSelector.IsAttackable(x))
                                        ? UnitManager.MyChampion
                                        : EOnlyOnSelf
                                            ? null
                                            : SpellE.GetTargets(mode).FirstOrDefault()
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                AllowCastOnMap = () => AllowRCastOnMinimap,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => RHitChance,
                Range = () => 30000,
                Radius = () => 280,
                Speed = () => 2000,
                Delay = () => 0.6f,
                MinimumMana = () => 100,
                IsEnabled = () => UseR,
                IsSpellReady = (spellClass, minimumMana, minimumCharges) =>
                                spellClass.IsSpellReady &&
                                UnitManager.MyChampion.Mana >= spellClass.SpellData.ResourceCost &&
                                UnitManager.MyChampion.Mana >= minimumMana &&
                                spellClass.Charges >= minimumCharges &&
                                !ROnlyOutsideOfAttackRange || !UnitManager.EnemyChampions.Any(TargetSelector.IsInRange),
                TargetSelect = (mode) => SpellR.GetTargets(mode, x => x.HealthPercent <= RTargetMaxHPPercent && x.Distance > RMinimumRange && x.Distance <= RMaximumRange).FirstOrDefault()
            };
            SpellRSemiAuto = new Spell(CastSlot.R, SpellSlot.R)
            {
                AllowCastOnMap = () => AllowRCastOnMinimap,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => SemiAutoRHitChance,
                Range = () => 30000,
                Radius = () => 280,
                Speed = () => 2000,
                Delay = () => 0.6f,
                MinimumMana = () => 100,
                IsEnabled = () => UseSemiAutoR,
                TargetSelect = (mode) => SpellRSemiAuto.GetTargets(mode, x => x.Distance > RMinimumRange && x.Distance <= RMaximumRange).FirstOrDefault()
            };
        }

        private bool ShouldSwapGun(Orbwalker.OrbWalkingModeType mode, GameObjectBase target)
        {
            if (target is null)
            {
                return false;
            }

            if ((IsUsingMinigun && target.Distance > MinigunRange + target.BoundingRadius) ||
                (IsUsingRockets && target.Distance < MinigunRange + target.BoundingRadius))
            {
                return true;
            }

            if (ShouldUseRocketsForAOE(target, mode) ||
                ShouldUseRocketsForRange(target))
            {
                return !IsUsingRockets;
            }

            return false;
        }

        private bool ShouldUseRocketsForRange(GameObjectBase target)
        {
            if (target is null ||
                target.IsObject(ObjectTypeFlag.AITurretClient) ||
                target.IsObject(ObjectTypeFlag.BuildingProps) ||
                (IsUsingRockets && target != null && target.Distance < MinigunRange + target.BoundingRadius))
            {
                return false;
            }

            return IsUsingMinigun && target != null && target.Distance > MinigunRange + target.BoundingRadius;
        }

        private bool ShouldUseRocketsForAOE(GameObjectBase target, Orbwalker.OrbWalkingModeType mode)
        {
            if (target is null ||
                !UseRocketsForAOE ||
                target.IsObject(ObjectTypeFlag.AITurretClient) ||
                target.IsObject(ObjectTypeFlag.BuildingProps))
            {
                return false;
            }

            if (UnitManager.EnemyChampions.Any(x => x.NetworkID != target.NetworkID && x.DistanceTo(target.Position) <= 230 && TargetSelector.IsAttackable(x)))
            {
                return true;
            }

            if (mode != Orbwalker.OrbWalkingModeType.Combo &&
                (UnitManager.EnemyMinions.Any(x => TargetSelector.IsAttackable(x) &&
                                                    x.NetworkID != target.NetworkID &&
                                                    x.DistanceTo(target.Position) < 230 &&
                                                    x.Health <= Oasys.Common.Logic.DamageCalculator.GetMinimumBasicAttackDamage(UnitManager.MyChampion, x)) ||
                 UnitManager.EnemyJungleMobs.Any(x => TargetSelector.IsAttackable(x) &&
                                                        x.NetworkID != target.NetworkID &&
                                                        x.DistanceTo(target.Position) < 230)))
            {
                return mode != Orbwalker.OrbWalkingModeType.LaneClear ||
                       QMinManaPercentForAOELaneclear < UnitManager.MyChampion.ManaPercent;
            }

            return false;
        }

        private void KeyboardProvider_OnKeyPress(Keys keyBeingPressed, Oasys.Common.Tools.Devices.Keyboard.KeyPressState pressState)
        {
            if (keyBeingPressed == SemiAutoRKey && pressState == Oasys.Common.Tools.Devices.Keyboard.KeyPressState.Down)
            {
                SpellRSemiAuto.ExecuteCastSpell();
            }
        }

        internal override void OnCoreMainInput()
        {
            if (SpellQ.ExecuteCastSpell() || SpellE.ExecuteCastSpell() || SpellW.ExecuteCastSpell() || SpellR.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void OnCoreLaneClearInput()
        {
            if (SpellQ.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.LaneClear))
            {
                return;
            }
        }

        internal override void OnCoreLastHitInput()
        {
            if (SpellQ.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.LastHit))
            {
                return;
            }
        }

        internal override void OnCoreHarassInput()
        {
            if (SpellQ.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.Mixed))
            {
                return;
            }
        }

        private bool UseRocketsForAOE
        {
            get => QSettings.GetItem<Switch>("Use Rockets For AOE").IsOn;
            set => QSettings.GetItem<Switch>("Use Rockets For AOE").IsOn = value;
        }

        private int QMinManaPercentForAOELaneclear
        {
            get => QSettings.GetItem<Counter>("Q Min Mana Percent For AOE Laneclear").Value;
            set => QSettings.GetItem<Counter>("Q Min Mana Percent For AOE Laneclear").Value = value;
        }

        private bool QPreferRockets
        {
            get => QSettings.GetItem<Switch>("Q prefer rockets").IsOn;
            set => QSettings.GetItem<Switch>("Q prefer rockets").IsOn = value;
        }

        private int QMinigunMaximumRange
        {
            get => QSettings.GetItem<Counter>("Q Minigun Maximum Range").Value;
            set => QSettings.GetItem<Counter>("Q Minigun Maximum Range").Value = value;
        }

        private bool WOnlyOutsideOfAttackRange
        {
            get => WSettings.GetItem<Switch>("W only outside of attack range").IsOn;
            set => WSettings.GetItem<Switch>("W only outside of attack range").IsOn = value;
        }

        private int WMinimumRange
        {
            get => WSettings.GetItem<Counter>("W minimum range").Value;
            set => WSettings.GetItem<Counter>("W minimum range").Value = value;
        }

        private bool EOnSelf
        {
            get => ESettings.GetItem<Switch>("E on self").IsOn;
            set => ESettings.GetItem<Switch>("E on self").IsOn = value;
        }

        private bool EOnlyOnSelf
        {
            get => ESettings.GetItem<Switch>("E only on self").IsOn;
            set => ESettings.GetItem<Switch>("E only on self").IsOn = value;
        }

        private int EMaximumRange
        {
            get => ESettings.GetItem<Counter>("E enemy maximum range").Value;
            set => ESettings.GetItem<Counter>("E enemy maximum range").Value = value;
        }

        private bool ROnlyOutsideOfAttackRange
        {
            get => RSettings.GetItem<Switch>("R only outside of attack range").IsOn;
            set => RSettings.GetItem<Switch>("R only outside of attack range").IsOn = value;
        }

        private int RMinimumRange
        {
            get => RSettings.GetItem<Counter>("R minimum range").Value;
            set => RSettings.GetItem<Counter>("R minimum range").Value = value;
        }

        private int RMaximumRange
        {
            get => RSettings.GetItem<Counter>("R maximum range").Value;
            set => RSettings.GetItem<Counter>("R maximum range").Value = value;
        }

        private int RTargetMaxHPPercent
        {
            get => RSettings.GetItem<Counter>("R Target Max HP Percent").Value;
            set => RSettings.GetItem<Counter>("R Target Max HP Percent").Value = value;
        }


        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Jinx)}"));

            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Use Q Laneclear", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Use Q Lasthit", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Use Q Harass", IsOn = true });
            QSettings.AddItem(new Counter() { Title = "Q Min Mana Percent For AOE Laneclear", MinValue = 0, MaxValue = 100, Value = 80, ValueFrequency = 5 });
            QSettings.AddItem(new Switch() { Title = "Use Rockets For AOE", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Q prefer rockets", IsOn = false });
            QSettings.AddItem(new Counter() { Title = "Q Minigun Maximum Range", MinValue = 0, MaxValue = 750, Value = 750, ValueFrequency = 25 });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });
            WSettings.AddItem(new ModeDisplay() { Title = "W HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "VeryHigh" });
            WSettings.AddItem(new Switch() { Title = "W only outside of attack range", IsOn = false });
            WSettings.AddItem(new Counter() { Title = "W minimum range", MinValue = 0, MaxValue = 1500, Value = 0, ValueFrequency = 50 });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });
            ESettings.AddItem(new ModeDisplay() { Title = "E HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "Immobile" });
            ESettings.AddItem(new InfoDisplay() { Title = "---E anti melee Settings---" });
            ESettings.AddItem(new Switch() { Title = "E on self", IsOn = true });
            ESettings.AddItem(new Switch() { Title = "E only on self", IsOn = false });
            ESettings.AddItem(new Counter() { Title = "E enemy maximum range", MinValue = 0, MaxValue = 900, Value = 250, ValueFrequency = 50 });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            RSettings.AddItem(new ModeDisplay() { Title = "R HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "VeryHigh" });


            RSettings.AddItem(new Switch() { Title = "Use Semi Auto R", IsOn = true });
            RSettings.AddItem(new KeyBinding() { Title = "Semi Auto R Key", SelectedKey = Keys.T });
            RSettings.AddItem(new ModeDisplay() { Title = "Semi Auto R HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "VeryHigh" });


            RSettings.AddItem(new Switch() { Title = "Allow R cast on minimap", IsOn = true });
            RSettings.AddItem(new Switch() { Title = "R only outside of attack range", IsOn = false });
            RSettings.AddItem(new Counter() { Title = "R minimum range", MinValue = 0, MaxValue = 30_000, Value = 0, ValueFrequency = 50 });
            RSettings.AddItem(new Counter() { Title = "R maximum range", MinValue = 0, MaxValue = 30_000, Value = 30_000, ValueFrequency = 50 });
            RSettings.AddItem(new Counter() { Title = "R Target Max HP Percent", MinValue = 10, MaxValue = 100, Value = 50, ValueFrequency = 5 });
        }
    }
}
