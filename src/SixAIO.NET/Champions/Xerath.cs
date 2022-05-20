using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SixAIO.Models;
using System;
using System.Linq;
using System.Windows.Forms;

namespace SixAIO.Champions
{
    internal class Xerath : Champion
    {
        public Xerath()
        {
            Oasys.SDK.InputProviders.KeyboardProvider.OnKeyPress += KeyboardProvider_OnKeyPress;
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                IsCharge = () => true,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => QHitChance,
                Range = () => SpellQ.ChargeTimer.IsRunning
                                        ? SpellQ.SpellClass.IsSpellReady
                                            ? 735 + SpellQ.ChargeTimer.ElapsedMilliseconds / 1000 / 0.25f * 102f
                                            : 0
                                        : QStartChargeRange,
                Radius = () => 140,
                Speed = () => 1900,
                IsEnabled = () => UseQ,
                MinimumMana = () => 120,
                IsSpellReady = (spellClass, minMana, minCharges) => SpellQ.ChargeTimer.IsRunning || UnitManager.MyChampion.Mana > minMana,
                ShouldCast = (mode, target, spellClass, damage) => target != null && (target.Distance < SpellQ.Range() || (!SpellQ.ChargeTimer.IsRunning && target.Distance <= QStartChargeRange)),
                TargetSelect = (mode) => SpellQ.GetTargets(mode).FirstOrDefault()
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Circle,
                MinimumHitChance = () => WHitChance,
                Range = () => 1000,
                Radius = () => 250,
                Speed = () => 2500,
                IsEnabled = () => UseW,
                TargetSelect = (mode) => SpellW.GetTargets(mode).FirstOrDefault()
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                AllowCollision = (target, collisions) => !collisions.Any(),
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => EHitChance,
                Range = () => 1125,
                Radius = () => 120,
                Speed = () => 1400,
                IsEnabled = () => UseE,
                TargetSelect = (mode) => SpellE.GetTargets(mode).FirstOrDefault()
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                IsChannel = () => true,
                AllowCastOnMap = () => AllowRCastOnMinimap,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Circle,
                MinimumHitChance = () => RHitChance,
                Range = () => 5000,
                Radius = () => 200,
                Speed = () => 1500,
                Delay = () => 0.63f,
                IsEnabled = () => UseR && !SpellQ.ChargeTimer.IsRunning && UnitManager.MyChampion.BuffManager.ActiveBuffs.Any(x => x.Name == "xerathrshots" && x.Stacks >= 1),
                IsSpellReady = (spellClass, minMana, minCharges) => spellClass.IsToggled && spellClass.Charges > minCharges || UnitManager.MyChampion.Mana > minMana,
                TargetSelect = (mode) => SpellR.GetTargets(mode).OrderBy(x => x.DistanceTo(GameEngine.WorldMousePosition)).FirstOrDefault()
            };
            SpellRSemiAuto = new Spell(CastSlot.R, SpellSlot.R)
            {
                AllowCastOnMap = () => AllowRCastOnMinimap,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => SemiAutoRHitChance,
                Range = () => 5000,
                Radius = () => 200,
                Speed = () => 1500,
                Delay = () => 0.63f,
                IsEnabled = () => UseR && !SpellQ.ChargeTimer.IsRunning && UnitManager.MyChampion.BuffManager.ActiveBuffs.Any(x => x.Name == "xerathrshots" && x.Stacks >= 1),
                TargetSelect = (mode) => SpellRSemiAuto.GetTargets(mode).OrderBy(x => x.DistanceTo(GameEngine.WorldMousePosition)).FirstOrDefault()
            };
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
            if (SpellW.ExecuteCastSpell() || SpellE.ExecuteCastSpell() || SpellQ.ExecuteCastSpell() || SpellR.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void OnCoreHarassInput()
        {
            if (SpellW.ExecuteCastSpell() || SpellQ.ExecuteCastSpell())
            {
                return;
            }
        }

        private int QStartChargeRange
        {
            get => QSettings.GetItem<Counter>("Q start charge range").Value;
            set => QSettings.GetItem<Counter>("Q start charge range").Value = value;
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Xerath)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });
            QSettings.AddItem(new Counter() { Title = "Q start charge range", MinValue = 0, MaxValue = 2000, Value = 1450, ValueFrequency = 50 });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });
            WSettings.AddItem(new ModeDisplay() { Title = "W HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });
            ESettings.AddItem(new ModeDisplay() { Title = "E HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            RSettings.AddItem(new Switch() { Title = "Allow R cast on minimap", IsOn = true });
            RSettings.AddItem(new ModeDisplay() { Title = "R HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            RSettings.AddItem(new Switch() { Title = "Use Semi Auto R", IsOn = true });
            RSettings.AddItem(new KeyBinding() { Title = "Semi Auto R Key", SelectedKey = Keys.T });
            RSettings.AddItem(new ModeDisplay() { Title = "Semi Auto R HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

        }
    }
}
