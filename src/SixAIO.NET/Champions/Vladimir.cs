using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.Rendering;
using Oasys.SDK.SpellCasting;
using SharpDX;
using SixAIO.Models;
using System;
using System.Linq;
using System.Windows.Forms;

namespace SixAIO.Champions
{
    internal sealed class Vladimir : Champion
    {
        public Vladimir()
        {
            Oasys.SDK.InputProviders.KeyboardProvider.OnKeyPress += KeyboardProvider_OnKeyPress;
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                IsSpellReady = (spellClass, minMana, minCharges) => spellClass.IsSpellReady,
                IsTargetted = () => true,
                Range = () => 600,
                IsEnabled = () => UseQ,
                Damage = (target, spellClass) =>
                            target != null
                            ? DamageCalculator.CalculateActualDamage(UnitManager.MyChampion, target, 0,
                            UnitManager.MyChampion.Mana >= 2
                            ? ((111 + spellClass.Level * 37) + (UnitManager.MyChampion.UnitStats.TotalAbilityPower * 1.11f))
                            : ((60 + spellClass.Level * 20) + (UnitManager.MyChampion.UnitStats.TotalAbilityPower * 0.6f))
                                , 0)
                            : 0,
                TargetSelect = (mode) =>
                {
                    if (mode == Orbwalker.OrbWalkingModeType.Combo)
                    {
                        return SpellQ.GetTargets(mode).FirstOrDefault();
                    }
                    else if (mode == Orbwalker.OrbWalkingModeType.LastHit)
                    {
                        return SpellQ.GetTargets(mode, x => x.Health <= SpellQ.Damage(x, SpellQ.SpellClass)).FirstOrDefault();
                    }
                    else if (mode == Orbwalker.OrbWalkingModeType.Mixed)
                    {
                        return SpellQ.GetTargets(mode, x => x.IsObject(ObjectTypeFlag.AIHeroClient) || x.Health <= SpellQ.Damage(x, SpellQ.SpellClass)).FirstOrDefault();
                    }
                    else
                    {
                        return SpellQ.GetTargets(mode).FirstOrDefault();
                    }
                }
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                IsSpellReady = (spellClass, minMana, minCharges) => spellClass.IsSpellReady,
                IsCharge = () => true,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => Prediction.MenuSelected.HitChance.Low,
                Range = () => SpellE.ChargeTimer.IsRunning
                                        ? Math.Min(600, 0 + SpellE.ChargeTimer.ElapsedMilliseconds * 0.6f)
                                        : 600,
                Radius = () => 120,
                Speed = () => 4000,
                IsEnabled = () => UseE,
                MinimumMana = () => 0,
                MinimumCharges = () => 0,
                AllowCollision = (_, _) => false,
                Delay = () => 0,
                ShouldCast = (mode, target, spellClass, damage) => target != null && (target.Distance < SpellE.Range() || (!SpellE.ChargeTimer.IsRunning && target.Distance <= 600)),
                TargetSelect = (mode) => SpellE.GetTargets(mode).FirstOrDefault()
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                IsSpellReady = (spellClass, minMana, minCharges) => spellClass.IsSpellReady,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Circle,
                MinimumHitChance = () => RHitChance,
                Range = () => 625,
                Speed = () => 15000,
                Radius = () => 375,
                Delay = () => 0,
                IsEnabled = () => UseR,
                TargetSelect = (mode) => SpellR.GetTargets(mode).FirstOrDefault()
            };
        }

        private void KeyboardProvider_OnKeyPress(Keys keyBeingPressed, Oasys.Common.Tools.Devices.Keyboard.KeyPressState pressState)
        {
            if (keyBeingPressed == DisableAAKey && pressState == Oasys.Common.Tools.Devices.Keyboard.KeyPressState.Down)
            {
                Orbwalker.AllowAttacking = !Orbwalker.AllowAttacking;
            }
        }

        internal override void OnCoreMainInput()
        {
            SpellR.ExecuteCastSpell();
            SpellQ.ExecuteCastSpell();
            SpellE.ExecuteCastSpell();
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


        internal override void OnCoreRender()
        {
            if (!Orbwalker.AllowAttacking)
            {
                var pos = UnitManager.MyChampion.W2S;
                pos.Y -= 20;
                RenderFactory.DrawText("AA Disabled", 1, pos, Color.White);
            }
        }

        public Keys DisableAAKey => MenuTab.GetItem<KeyBinding>("Disable AA Key").SelectedKey;

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Vladimir)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));
            MenuTab.AddItem(new KeyBinding() { Title = "Disable AA Key", SelectedKey = Keys.U });

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Use Q Laneclear", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Use Q Harass", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Use Q Lasthit", IsOn = true });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            RSettings.AddItem(new ModeDisplay() { Title = "R HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });
        }
    }
}
