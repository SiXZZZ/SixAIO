using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SixAIO.Extensions;
using SixAIO.Models;
using System;
using System.Linq;
using System.Windows.Forms;

namespace SixAIO.Champions
{
    internal sealed class Xerath : Champion
    {
        public Xerath()
        {
            Oasys.SDK.InputProviders.KeyboardProvider.OnKeyPress += KeyboardProvider_OnKeyPress;
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                ShouldDraw = () => DrawQRange,
                DrawColor = () => DrawQColor,
                IsCharge = () => true,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => QHitChance,
                Range = () => SpellQ.ChargeTimer.IsRunning
                                        ? SpellQ.SpellClass.IsSpellReady
                                            ? 735 + SpellQ.ChargeTimer.ElapsedMilliseconds / 1000 / 0.25f * 102f
                                            : 0
                                        : QStartChargeRange,
                Radius = () => 140,
                Speed = () => 5000,
                IsEnabled = () => UseQ && !UnitManager.MyChampion.BuffManager.ActiveBuffs.Any(x => x.Name == "XerathLocusOfPower2" && x.Stacks >= 1),
                MinimumMana = () => 120,
                IsSpellReady = (spellClass, minMana, minCharges) => SpellQ.ChargeTimer.IsRunning || UnitManager.MyChampion.Mana > minMana,
                ShouldCast = (mode, target, spellClass, damage) => target != null && (target.Distance < SpellQ.Range() || (!SpellQ.ChargeTimer.IsRunning && target.Distance <= QStartChargeRange)),
                TargetSelect = (mode) => SpellQ.GetTargets(mode).FirstOrDefault()
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                ShouldDraw = () => DrawWRange,
                DrawColor = () => DrawWColor,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Circle,
                MinimumHitChance = () => WHitChance,
                Range = () => 1000,
                Radius = () => 250,
                Speed = () => 5000,
                Delay = () => 0.778f,
                IsEnabled = () => UseW && !UnitManager.MyChampion.BuffManager.ActiveBuffs.Any(x => x.Name == "XerathLocusOfPower2" && x.Stacks >= 1),
                TargetSelect = (mode) => SpellW.GetTargets(mode).FirstOrDefault()
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                ShouldDraw = () => DrawERange,
                DrawColor = () => DrawEColor,
                AllowCollision = (target, collisions) => !collisions.Any(),
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => EHitChance,
                Range = () => EMaximumRange,
                Radius = () => 120,
                Speed = () => 1400,
                IsEnabled = () => UseE && !UnitManager.MyChampion.BuffManager.ActiveBuffs.Any(x => x.Name == "XerathLocusOfPower2" && x.Stacks >= 1),
                TargetSelect = (mode) => SpellE.GetTargets(mode).FirstOrDefault()
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                ShouldDraw = () => DrawRRange,
                DrawColor = () => DrawRColor,
                IsChannel = () => true,
                AllowCastOnMap = () => AllowRCastOnMinimap,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Circle,
                MinimumHitChance = () => RHitChance,
                Range = () => 5000,
                Radius = () => 200,
                Speed = () => RSpeed,
                Delay = () => (float)((float)((float)RDelay) / 1000f),
                IsEnabled = () => UseR && UnitManager.MyChampion.BuffManager.ActiveBuffs.Any(x => x.Name == "xerathrshots" && x.Stacks >= 1),
                IsSpellReady = (spellClass, minMana, minCharges) => UnitManager.MyChampion.BuffManager.ActiveBuffs.Any(x => x.Name == "XerathLocusOfPower2" && x.Stacks >= 1) && spellClass.Charges > minCharges || UnitManager.MyChampion.Mana > minMana,
                TargetSelect = (mode) => RTargetClosestToMouse
                                        ? SpellR.GetTargets(mode).OrderBy(x => x.DistanceTo(GameEngine.WorldMousePosition)).FirstOrDefault()
                                        : SpellR.GetTargets(mode).FirstOrDefault()
            };
            SpellRSemiAuto = new Spell(CastSlot.R, SpellSlot.R)
            {
                IsChannel = () => true,
                AllowCastOnMap = () => AllowRCastOnMinimap,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Circle,
                MinimumHitChance = () => SemiAutoRHitChance,
                Range = () => 5000,
                Radius = () => 180,
                Speed = () => RSpeed,
                Delay = () => (float)((float)((float)RDelay) / 1000f),
                IsEnabled = () => UseSemiAutoR && UnitManager.MyChampion.BuffManager.ActiveBuffs.Any(x => x.Name == "xerathrshots" && x.Stacks >= 1),
                IsSpellReady = (spellClass, minMana, minCharges) => UnitManager.MyChampion.BuffManager.ActiveBuffs.Any(x => x.Name == "XerathLocusOfPower2" && x.Stacks >= 1) && spellClass.Charges > minCharges || UnitManager.MyChampion.Mana > minMana,
                TargetSelect = (mode) => RTargetClosestToMouse
                                        ? SpellRSemiAuto.GetTargets(mode).OrderBy(x => x.DistanceTo(GameEngine.WorldMousePosition)).FirstOrDefault()
                                        : SpellRSemiAuto.GetTargets(mode).FirstOrDefault()
            };
        }
        //XerathLocusOfPower2
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
            SpellE.DrawRange();
            SpellR.DrawRange();
        }

        internal override void OnCoreMainInput()
        {
            if (SpellR.ExecuteCastSpell())
            {
                return;
            }

            if (SpellW.ExecuteCastSpell() || SpellE.ExecuteCastSpell() || SpellQ.ExecuteCastSpell())
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

        private int EMaximumRange
        {
            get => ESettings.GetItem<Counter>("E maximum range").Value;
            set => ESettings.GetItem<Counter>("E maximum range").Value = value;
        }

        private bool RTargetClosestToMouse
        {
            get => RSettings.GetItem<Switch>("R Target Closest To Mouse").IsOn;
            set => RSettings.GetItem<Switch>("R Target Closest To Mouse").IsOn = value;
        }

        private int RSpeed
        {
            get => RSettings.GetItem<Counter>("R Speed").Value;
            set => RSettings.GetItem<Counter>("R Speed").Value = value;
        }

        private int RDelay
        {
            get => RSettings.GetItem<Counter>("R Delay").Value;
            set => RSettings.GetItem<Counter>("R Delay").Value = value;
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
            ESettings.AddItem(new Counter() { Title = "E maximum range", MinValue = 0, MaxValue = 1125, Value = 1100, ValueFrequency = 25 });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            RSettings.AddItem(new Counter() { Title = "R Speed", MinValue = 0, MaxValue = 100_000, Value = 1500, ValueFrequency = 50 });
            RSettings.AddItem(new Counter() { Title = "R Delay", MinValue = 0, MaxValue = 5_000, Value = 630, ValueFrequency = 10 });
            RSettings.AddItem(new Switch() { Title = "Allow R cast on minimap", IsOn = true });
            RSettings.AddItem(new Switch() { Title = "R Target Closest To Mouse", IsOn = true });
            RSettings.AddItem(new ModeDisplay() { Title = "R HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            RSettings.AddItem(new Switch() { Title = "Use Semi Auto R", IsOn = true });
            RSettings.AddItem(new KeyBinding() { Title = "Semi Auto R Key", SelectedKey = Keys.T });
            RSettings.AddItem(new ModeDisplay() { Title = "Semi Auto R HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });


            MenuTab.AddDrawOptions(SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R);

        }
    }
}
