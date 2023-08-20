using Oasys.Common.Enums.GameEnums;
using Oasys.Common.GameObject;
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
    internal sealed class Ezreal : Champion
    {
        public Ezreal()
        {
            Oasys.SDK.InputProviders.KeyboardProvider.OnKeyPress += KeyboardProvider_OnKeyPress;
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                ShouldDraw = () => DrawQRange,
                DrawColor = () => DrawQColor,
                AllowCollision = (target, collisions) => !collisions.Any(),
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => QHitChance,
                Range = () => 1100,
                Radius = () => 120,
                Speed = () => 2000,
                IsEnabled = () => UseQ,
                MinimumMana = () => QMinMana,
                TargetSelect = (mode) =>
                {
                    if (mode == Orbwalker.OrbWalkingModeType.Combo)
                    {
                        return PrioTargetsWithW
                                ? SpellQ.GetTargets(mode)
                                        .OrderByDescending(x => x.BuffManager.ActiveBuffs.Any(buff => buff.IsActive && buff.Stacks >= 1 && buff.Name == "ezrealwattach"))
                                        .FirstOrDefault()
                                : SpellQ.GetTargets(mode).FirstOrDefault();
                    }
                    else if (mode == Orbwalker.OrbWalkingModeType.LastHit)
                    {
                        var targets = SpellQ.GetTargets(mode, x => (!Orbwalker.CanBasicAttack || !TargetSelector.IsInRange(x)) && x.Health <= GetQDamage(x));
                        return targets.Count() > 1
                            ? targets.Where(x => x.NetworkID != Oasys.Common.Logic.Orbwalker.OrbSettings.LastHitTarget?.NetworkID).FirstOrDefault()
                            : targets.FirstOrDefault();
                    }
                    else if (mode == Orbwalker.OrbWalkingModeType.Mixed)
                    {
                        var targets = SpellQ.GetTargets(mode, x => (!Orbwalker.CanBasicAttack || !TargetSelector.IsInRange(x)) && x.IsObject(ObjectTypeFlag.AIHeroClient) || x.Health <= GetQDamage(x));
                        return targets.Count() > 1
                            ? targets.Where(x =>
                                        Oasys.Common.Logic.Orbwalker.OrbSettings.MixedTarget?.IsObject(ObjectTypeFlag.AIHeroClient) == true ||
                                        x.NetworkID != Oasys.Common.Logic.Orbwalker.OrbSettings.MixedTarget?.NetworkID)
                                     .FirstOrDefault()
                            : targets.FirstOrDefault();
                    }
                    else
                    {
                        var targets = SpellQ.GetTargets(mode, x => (!Orbwalker.CanBasicAttack || !TargetSelector.IsInRange(x)));
                        return targets.Count() > 1
                            ? targets.Where(x => x.NetworkID != Orbwalker.LaneClearTarget?.NetworkID).FirstOrDefault()
                            : targets.FirstOrDefault();
                    }
                }
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                ShouldDraw = () => DrawWRange,
                DrawColor = () => DrawWColor,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => WHitChance,
                Range = () => WMaximumRange,
                Radius = () => 140,
                Speed = () => 1700,
                IsEnabled = () => UseW,
                MinimumMana = () => WMinMana,
                TargetSelect = (mode) =>
                {
                    if (OnlyWIfCanProc)
                    {
                        if (Orbwalker.TargetHero is not null)
                        {
                            return Orbwalker.TargetHero;
                        }
                        return SpellQ.GetTargets(mode, x => x.Distance <= WMaximumRange).FirstOrDefault();
                    }
                    else
                    {
                        return SpellW.GetTargets(mode).FirstOrDefault();
                    }
                }
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                IsEnabled = () => UseE,
                MinimumMana = () => EMinMana,
                ShouldCast = (mode, target, spellClass, damage) =>
                            DashModeSelected == DashMode.ToMouse &&
                            TargetSelector.IsAttackable(Orbwalker.TargetHero) &&
                            TargetSelector.IsInRange(Orbwalker.TargetHero),
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                ShouldDraw = () => DrawRRange,
                DrawColor = () => DrawRColor,
                AllowCastOnMap = () => AllowRCastOnMinimap,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => RHitChance,
                Range = () => RMaximumRange,
                Radius = () => 320,
                Speed = () => 2000,
                Delay = () => 1f,
                IsEnabled = () => UseR,
                MinimumMana = () => RMinMana,
                TargetSelect = (mode) => PrioTargetsWithW
                                            ? SpellR.GetTargets(mode, x => x.HealthPercent <= RTargetMaxHPPercent && x.Distance > RMinimumRange && x.Distance <= RMaximumRange)
                                                    .OrderByDescending(x => x.BuffManager.ActiveBuffs.Any(buff => buff.IsActive && buff.Stacks >= 1 && buff.Name == "ezrealwattach"))
                                                    .FirstOrDefault()
                                            : SpellR.GetTargets(mode, x => x.HealthPercent <= RTargetMaxHPPercent && x.Distance > RMinimumRange && x.Distance <= RMaximumRange)
                                                    .FirstOrDefault()
            };
            SpellRSemiAuto = new Spell(CastSlot.R, SpellSlot.R)
            {
                AllowCastOnMap = () => AllowRCastOnMinimap,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => SemiAutoRHitChance,
                Range = () => RMaximumRange,
                Radius = () => 320,
                Speed = () => 2000,
                Delay = () => 1f,
                IsEnabled = () => UseSemiAutoR,
                MinimumMana = () => RMinMana,
                TargetSelect = (mode) => PrioTargetsWithW
                                            ? SpellRSemiAuto.GetTargets(mode, x => x.Distance > RMinimumRange && x.Distance <= RMaximumRange)
                                                            .OrderByDescending(x => x.BuffManager.ActiveBuffs.Any(buff => buff.IsActive && buff.Stacks >= 1 && buff.Name == "ezrealwattach"))
                                                            .FirstOrDefault()
                                            : SpellRSemiAuto.GetTargets(mode, x => x.Distance > RMinimumRange && x.Distance <= RMaximumRange)
                                                            .FirstOrDefault()
            };
        }

        private float GetQDamage(GameObjectBase target)
        {
            return target != null
            ? DamageCalculator.CalculateActualDamage(UnitManager.MyChampion, target,
                ((-5 + SpellQ.SpellClass.Level * 25) +
                (UnitManager.MyChampion.UnitStats.TotalAttackDamage * 1.3f) +
                (UnitManager.MyChampion.UnitStats.TotalAbilityPower * 0.15f)))
            : 0;
        }

        private void KeyboardProvider_OnKeyPress(Keys keyBeingPressed, Oasys.Common.Tools.Devices.Keyboard.KeyPressState pressState)
        {
            if (keyBeingPressed == SemiAutoRKey && pressState == Oasys.Common.Tools.Devices.Keyboard.KeyPressState.Down)
            {
                SpellRSemiAuto.ExecuteCastSpell();
            }
        }

        internal override void OnCoreRender()
        {
            SpellQ.DrawRange();
            SpellW.DrawRange();
            SpellR.DrawRange();
        }

        internal override void OnCoreMainInput()
        {
            if (PrioTargetsWithW)
            {
                Orbwalker.SelectedTarget = UnitManager.EnemyChampions.FirstOrDefault(x => x.BuffManager.ActiveBuffs.Any(buff => buff.IsActive && buff.Stacks >= 1 && buff.Name == "ezrealwattach"));
            }

            if (SpellQ.ExecuteCastSpell() ||
                SpellW.ExecuteCastSpell() ||
                SpellR.ExecuteCastSpell() ||
                SpellE.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void OnCoreLaneClearInput()
        {
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

        internal bool PrioTargetsWithW
        {
            get => WSettings.GetItem<Switch>("Prio Targets With W").IsOn;
            set => WSettings.GetItem<Switch>("Prio Targets With W").IsOn = value;
        }

        internal bool OnlyWIfCanProc
        {
            get => WSettings.GetItem<Switch>("Only W If Can Proc").IsOn;
            set => WSettings.GetItem<Switch>("Only W If Can Proc").IsOn = value;
        }

        private int WMaximumRange
        {
            get => WSettings.GetItem<Counter>("W maximum range").Value;
            set => WSettings.GetItem<Counter>("W maximum range").Value = value;
        }

        private DashMode DashModeSelected
        {
            get => (DashMode)Enum.Parse(typeof(DashMode), ESettings.GetItem<ModeDisplay>("Dash Mode").SelectedModeName);
            set => ESettings.GetItem<ModeDisplay>("Dash Mode").SelectedModeName = value.ToString();
        }

        private int RTargetMaxHPPercent
        {
            get => RSettings.GetItem<Counter>("R Target Max HP Percent").Value;
            set => RSettings.GetItem<Counter>("R Target Max HP Percent").Value = value;
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

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Ezreal)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Use Q Laneclear", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Use Q Harass", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Use Q Lasthit", IsOn = true });
            QSettings.AddItem(new Counter() { Title = "Q Min Mana", MinValue = 0, MaxValue = 500, Value = 40, ValueFrequency = 10 });
            QSettings.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });
            WSettings.AddItem(new Counter() { Title = "W Min Mana", MinValue = 0, MaxValue = 500, Value = 80, ValueFrequency = 10 });
            WSettings.AddItem(new ModeDisplay() { Title = "W HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });
            WSettings.AddItem(new Switch() { Title = "Prio Targets With W", IsOn = true });
            WSettings.AddItem(new Switch() { Title = "Only W If Can Proc", IsOn = true });
            WSettings.AddItem(new Counter() { Title = "W maximum range", MinValue = 0, MaxValue = 1000, Value = 1000, ValueFrequency = 50 });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = false });
            ESettings.AddItem(new Counter() { Title = "E Min Mana", MinValue = 0, MaxValue = 500, Value = 150, ValueFrequency = 10 });
            ESettings.AddItem(new ModeDisplay() { Title = "Dash Mode", ModeNames = DashHelper.ConstructDashModeTable(), SelectedModeName = "ToMouse" });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            RSettings.AddItem(new Counter() { Title = "R Min Mana", MinValue = 0, MaxValue = 500, Value = 150, ValueFrequency = 10 });
            RSettings.AddItem(new Counter() { Title = "R Target Max HP Percent", MinValue = 10, MaxValue = 100, Value = 50, ValueFrequency = 5 });
            RSettings.AddItem(new ModeDisplay() { Title = "R HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            RSettings.AddItem(new Switch() { Title = "Use Semi Auto R", IsOn = true });
            RSettings.AddItem(new KeyBinding() { Title = "Semi Auto R Key", SelectedKey = Keys.T });
            RSettings.AddItem(new ModeDisplay() { Title = "Semi Auto R HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            RSettings.AddItem(new Switch() { Title = "Allow R cast on minimap", IsOn = true });
            RSettings.AddItem(new Counter() { Title = "R minimum range", MinValue = 0, MaxValue = 30_000, Value = 0, ValueFrequency = 50 });
            RSettings.AddItem(new Counter() { Title = "R maximum range", MinValue = 0, MaxValue = 30_000, Value = 30_000, ValueFrequency = 50 });


            MenuTab.AddDrawOptions(SpellSlot.Q, SpellSlot.W, SpellSlot.R);

        }
    }
}
