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
using SixAIO.Extensions;

namespace SixAIO.Champions
{
    internal sealed class Cassiopeia : Champion
    {
        public Cassiopeia()
        {
            Oasys.SDK.InputProviders.KeyboardProvider.OnKeyPress += KeyboardProvider_OnKeyPress;
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                ShouldDraw = () => DrawQRange,
                DrawColor = () => DrawQColor,
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
                ShouldDraw = () => DrawWRange,
                DrawColor = () => DrawWColor,
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
                ShouldDraw = () => DrawERange,
                DrawColor = () => DrawEColor,
                Delay = () => 0.125f,
                IsTargetted = () => true,
                Range = () => 700 + UnitManager.MyChampion.BoundingRadius,
                IsEnabled = () => UseE,
                TargetSelect = (mode) =>
                {
                    if(UseELasthit && mode == Orbwalker.OrbWalkingModeType.LastHit)
                    {
                        return SpellE.GetTargets(mode, x => x.PredictHealth(150) <= GetEDamage(x, SpellE.SpellClass)).OrderBy(IsPoisoned).ThenBy(x => x.EffectiveMagicHealth).FirstOrDefault();
                    }

                    var target = SpellE.GetTargets(mode).OrderBy(IsPoisoned).ThenBy(x => x.EffectiveMagicHealth).FirstOrDefault();
                    if (UseELasthit)
                    {
                        target = SpellE.GetTargets(mode, x => x.PredictHealth(150) <= GetEDamage(x, SpellE.SpellClass)).OrderBy(IsPoisoned).ThenBy(x => x.EffectiveMagicHealth).FirstOrDefault();
                    }
                    if (UseEHarass && mode == Orbwalker.OrbWalkingModeType.Mixed)
                    {
                        target = SpellE.GetTargets(mode, x => x.PredictHealth(150) <= GetEDamage(x, SpellE.SpellClass)).OrderBy(IsPoisoned).ThenBy(x => x.EffectiveMagicHealth).FirstOrDefault();
                    }
                    if (target is null && !UseELaneclear && mode == Orbwalker.OrbWalkingModeType.LaneClear)
                    {
                        return null;
                    }
                    if (target is not null)
                    {
                        return target;
                    }
                    else
                    {
                        return SpellE.GetTargets(mode).OrderBy(IsPoisoned).ThenBy(x => x.EffectiveMagicHealth).FirstOrDefault();
                    }
                }
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                ShouldDraw = () => DrawRRange,
                DrawColor = () => DrawRColor,
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

        internal override void OnCoreRender()
        {
            SpellQ.DrawRange();
            SpellW.DrawRange();
            SpellE.DrawRange();
            SpellR.DrawRange();

            if (!Orbwalker.AllowAttacking)
            {
                var pos = UnitManager.MyChampion.W2S;
                pos.Y -= 20;
                RenderFactory.DrawText("AA Disabled", 18, pos, Color.White);
            }
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
            if (SpellE.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.Mixed))
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
            if (SpellE.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.LaneClear))
            {
                return;
            }
        }

        internal override void OnCoreLastHitInput()
        {
            if (SpellE.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.LastHit))
            {
                return;
            }
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

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            RSettings.AddItem(new ModeDisplay() { Title = "R HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            RSettings.AddItem(new Switch() { Title = "Use Semi Auto R", IsOn = true });
            RSettings.AddItem(new KeyBinding() { Title = "Semi Auto R Key", SelectedKey = Keys.T });
            RSettings.AddItem(new ModeDisplay() { Title = "Semi Auto R HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });


            MenuTab.AddDrawOptions(SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R);

        }
    }
}
