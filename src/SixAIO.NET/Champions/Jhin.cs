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
    internal sealed class Jhin : Champion
    {
        public Jhin()
        {
            Oasys.SDK.InputProviders.KeyboardProvider.OnKeyPress += KeyboardProvider_OnKeyPress;
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                ShouldDraw = () => DrawQRange,
                DrawColor = () => DrawQColor,
                IsTargetted = () => true,
                IsEnabled = () => UseQ,
                Range = () => 550,
                TargetSelect = (mode) => Orbwalker.TargetHero,
                ShouldCast = (mode, target, spellClass, damage) => target is not null && target.Distance <= 550
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                ShouldDraw = () => DrawWRange,
                DrawColor = () => DrawWColor,
                AllowCollision = (target, collisions) => !collisions.Any(),
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => WHitChance,
                Range = () => 2500,
                Radius = () => 90,
                Speed = () => 2500,
                Delay = () => 0.75f,
                IsEnabled = () => UseW && (!WOnlyOutsideOfAttackRange || !UnitManager.EnemyChampions.Any(TargetSelector.IsInRange)) && (UnitManager.EnemyChampions.All(x => x.Distance >= WMinimumRange || !TargetSelector.IsAttackable(x))),
                TargetSelect = (mode) => SpellW.GetTargets(mode, x => x.BuffManager.ActiveBuffs.Any(buff => buff.Name == "jhinespotteddebuff" && buff.Stacks >= 1)).FirstOrDefault()
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                ShouldDraw = () => DrawRRange,
                DrawColor = () => DrawRColor,
                IsChannel = () => true,
                AllowCastOnMap = () => AllowRCastOnMinimap,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => RHitChance,
                MinimumCharges = () => 1,
                Range = () => 3500,
                Radius = () => 160,
                Speed = () => 5000,
                IsEnabled = () => UseR && SpellR.SpellClass.SpellData.SpellName == "JhinRShot",
                IsSpellReady = (spellClass, minMana, minCharges) => spellClass.Charges > minCharges || UnitManager.MyChampion.Mana > minMana,
                TargetSelect = (mode) => SpellR.GetTargets(mode).OrderBy(x => x.DistanceTo(GameEngine.WorldMousePosition)).FirstOrDefault()
            };
            SpellRSemiAuto = new Spell(CastSlot.R, SpellSlot.R)
            {
                IsChannel = () => true,
                AllowCastOnMap = () => AllowRCastOnMinimap,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => SemiAutoRHitChance,
                MinimumCharges = () => 1,
                Range = () => 3500,
                Radius = () => 160,
                Speed = () => 5000,
                IsEnabled = () => UseSemiAutoR && SpellRSemiAuto.SpellClass.SpellData.SpellName == "JhinRShot",
                IsSpellReady = (spellClass, minMana, minCharges) => spellClass.Charges > minCharges || UnitManager.MyChampion.Mana > minMana,
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

        internal override void OnCoreRender()
        {
            SpellQ.DrawRange();
            SpellW.DrawRange();
            SpellR.DrawRange();
        }

        internal override void OnCoreMainInput()
        {
            if (SpellQ.ExecuteCastSpell() || SpellW.ExecuteCastSpell() || SpellR.ExecuteCastSpell())
            {
                return;
            }
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

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Jhin)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });
            WSettings.AddItem(new ModeDisplay() { Title = "W HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });
            WSettings.AddItem(new Switch() { Title = "W only outside of attack range", IsOn = false });
            WSettings.AddItem(new Counter() { Title = "W minimum range", MinValue = 0, MaxValue = 1500, Value = 0, ValueFrequency = 50 });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            RSettings.AddItem(new Switch() { Title = "Allow R cast on minimap", IsOn = true });
            RSettings.AddItem(new ModeDisplay() { Title = "R HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            RSettings.AddItem(new Switch() { Title = "Use Semi Auto R", IsOn = true });
            RSettings.AddItem(new KeyBinding() { Title = "Semi Auto R Key", SelectedKey = Keys.T });
            RSettings.AddItem(new ModeDisplay() { Title = "Semi Auto R HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });


            MenuTab.AddDrawOptions(SpellSlot.Q, SpellSlot.W, SpellSlot.R);

        }
    }
}
