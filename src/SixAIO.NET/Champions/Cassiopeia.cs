using Oasys.Common.Enums.GameEnums;
using Oasys.Common.GameObject;
using Oasys.Common.GameObject.Clients.ExtendedInstances.Spells;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SixAIO.Models;
using System;
using System.Linq;
using Oasys.Common.Extensions;
using System.Windows.Forms;
using Oasys.SDK.Rendering;
using SharpDX;

namespace SixAIO.Champions
{
    internal sealed class Cassiopeia : Champion
    {
        public Cassiopeia()
        {
            Oasys.SDK.InputProviders.KeyboardProvider.OnKeyPress += KeyboardProvider_OnKeyPress;
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Circle,
                MinimumHitChance = () => QHitChance,
                Range = () => 850,
                Speed = () => 10000,
                Radius = () => 200,
                Delay = () => 0.6f,
                IsEnabled = () => UseQ,
                TargetSelect = (mode) => SpellQ.GetTargets(mode, x => x.HealthPercent >= 10).FirstOrDefault()
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Circle,
                MinimumHitChance = () => WHitChance,
                Range = () => 850,
                Speed = () => 1000,
                Radius = () => 200,
                IsEnabled = () => UseW,
                TargetSelect = (mode) => SpellW.GetTargets(mode, x => x.HealthPercent >= 10).FirstOrDefault()
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                Delay = () => 0.125f,
                IsTargetted = () => true,
                Range = () => 700 + UnitManager.MyChampion.BoundingRadius,
                Damage = GetEDamage,
                IsEnabled = () => UseE,
                TargetSelect = (mode) =>
                {
                    return mode switch
                    {
                        Orbwalker.OrbWalkingModeType.LastHit => SpellE.GetTargets(mode, x => x.PredictHealth(150) <= SpellE.Damage(x, SpellE.SpellClass)).OrderBy(IsPoisoned).ThenBy(x => x.EffectiveMagicHealth).FirstOrDefault(),
                        Orbwalker.OrbWalkingModeType.Mixed => SpellE.GetTargets(mode, x => x.PredictHealth(150) <= SpellE.Damage(x, SpellE.SpellClass)).OrderBy(IsPoisoned).ThenBy(x => x.EffectiveMagicHealth).FirstOrDefault(),
                        _ => SpellE.GetTargets(mode).OrderBy(IsPoisoned).ThenBy(x => x.EffectiveMagicHealth).FirstOrDefault()
                    };
                }
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Cone,
                MinimumHitChance = () => RHitChance,
                Range = () => 800,
                Speed = () => 10000,
                Radius = () => 80,
                Delay = () => 0.5f,
                IsEnabled = () => UseR,
                TargetSelect = (mode) => SpellR.GetTargets(mode, x =>
                                                      x.IsFacing(UnitManager.MyChampion) &&
                                                      !TargetSelector.IsInvulnerable(x, Oasys.Common.Logic.DamageType.Magical, false))
                                                .FirstOrDefault()
            };
            SpellRSemiAuto = new Spell(CastSlot.R, SpellSlot.R)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Cone,
                MinimumHitChance = () => SemiAutoRHitChance,
                Range = () => 800,
                Speed = () => 10000,
                Radius = () => 80,
                Delay = () => 0.5f,
                IsEnabled = () => UseSemiAutoR,
                TargetSelect = (mode) => SpellRSemiAuto.GetTargets(mode, x =>
                                                      x.IsFacing(UnitManager.MyChampion) &&
                                                      !TargetSelector.IsInvulnerable(x, Oasys.Common.Logic.DamageType.Magical, false))
                                                .FirstOrDefault()
            };
        }

        private void KeyboardProvider_OnKeyPress(Keys keyBeingPressed, Oasys.Common.Tools.Devices.Keyboard.KeyPressState pressState)
        {
            if (keyBeingPressed == SemiAutoRKey && pressState == Oasys.Common.Tools.Devices.Keyboard.KeyPressState.Down)
            {
                SpellRSemiAuto.ExecuteCastSpell();
            }
            if (keyBeingPressed == DisableAAKey && pressState == Oasys.Common.Tools.Devices.Keyboard.KeyPressState.Down)
            {
                Orbwalker.AllowAttacking = !Orbwalker.AllowAttacking;
            }
        }

        internal static float GetEDamage(GameObjectBase enemy, SpellClass spellClass)
        {
            if (enemy == null || spellClass == null)
            {
                return 0;
            }

            var magicDamage = 48 + 4 * UnitManager.MyChampion.Level +
                              UnitManager.MyChampion.UnitStats.TotalAbilityPower * 0.1f;

            if (IsPoisoned(enemy))
            {
                magicDamage += (20 * spellClass.Level) +
                                UnitManager.MyChampion.UnitStats.TotalAbilityPower * 0.6f;
            }

            return DamageCalculator.CalculateActualDamage(UnitManager.MyChampion, enemy, 0, magicDamage, 0);
        }

        private static bool IsPoisoned(GameObjectBase target)
        {
            return target.BuffManager.GetBuffList().Any(buff => buff != null && buff.IsActive && buff.OwnerObjectIndex == UnitManager.MyChampion.Index && buff.Stacks >= 1 &&
                    (buff.Name.Contains("cassiopeiaqdebuff", StringComparison.OrdinalIgnoreCase) || buff.Name.Contains("cassiopeiawpoison", StringComparison.OrdinalIgnoreCase)));
        }

        internal override void OnCoreMainInput()
        {
            SpellR.ExecuteCastSpell();
            SpellQ.ExecuteCastSpell();
            SpellW.ExecuteCastSpell();
            SpellE.ExecuteCastSpell();
        }

        internal override void OnCoreHarassInput()
        {
            if (UseQHarass && SpellQ.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.Mixed))
            {
                return;
            }
            if (UseEHarass && SpellE.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.Mixed))
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
            if (UseELaneclear && SpellE.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.LaneClear))
            {
                return;
            }
        }

        internal override void OnCoreLastHitInput()
        {
            if (UseELasthit && SpellE.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.LastHit))
            {
                return;
            }
        }

        internal override void OnCoreRender()
        {
            if (!Orbwalker.AllowAttacking)
            {
                var pos = UnitManager.MyChampion.W2S;
                pos.Y -= 20;
                RenderFactory.DrawText("AA Disabled", 18, pos, Color.White);
            }
            if (ShowERange)
            {
                var color = Oasys.Common.Tools.ColorConverter.GetColor(ERangeColor);
                RenderFactory.DrawNativeCircle(UnitManager.MyChampion.Position, SpellE.Range(), color, 2);
            }
        }

        private bool ShowERange
        {
            get => ESettings.GetItem<Switch>("Show E range").IsOn;
            set => ESettings.GetItem<Switch>("Show E range").IsOn = value;
        }

        private string ERangeColor
        {
            get => ESettings.GetItem<ModeDisplay>("E range color").SelectedModeName;
            set => ESettings.GetItem<ModeDisplay>("E range color").SelectedModeName = value;
        }

        public Keys DisableAAKey => MenuTab.GetItem<KeyBinding>("Disable AA Key").SelectedKey;

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Cassiopeia)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));
            MenuTab.AddItem(new KeyBinding() { Title = "Disable AA Key", SelectedKey = Keys.U });

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Use Q Laneclear", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Use Q Harass", IsOn = true });
            QSettings.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });
            WSettings.AddItem(new ModeDisplay() { Title = "W HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });
            ESettings.AddItem(new Switch() { Title = "Use E Laneclear", IsOn = true });
            ESettings.AddItem(new Switch() { Title = "Use E Lasthit", IsOn = true });
            ESettings.AddItem(new Switch() { Title = "Use E Harass", IsOn = true });
            ESettings.AddItem(new Switch() { Title = "Show E range", IsOn = true });
            ESettings.AddItem(new ModeDisplay() { Title = "E range color", ModeNames = Oasys.Common.Tools.ColorConverter.GetColors(), SelectedModeName = "Blue" });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            RSettings.AddItem(new ModeDisplay() { Title = "R HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            RSettings.AddItem(new Switch() { Title = "Use Semi Auto R", IsOn = true });
            RSettings.AddItem(new KeyBinding() { Title = "Semi Auto R Key", SelectedKey = Keys.T });
            RSettings.AddItem(new ModeDisplay() { Title = "Semi Auto R HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

        }
    }
}
