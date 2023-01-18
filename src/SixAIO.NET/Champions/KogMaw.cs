using Oasys.Common;
using Oasys.Common.Enums.GameEnums;
using Oasys.Common.GameObject;
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
    internal sealed class KogMaw : Champion
    {
        private float UltCost => 40 * UnitManager.MyChampion.BuffManager.ActiveBuffs
                                .FirstOrDefault(x => x.IsActive && x.Name.Equals("kogmawlivingartillerycost", StringComparison.OrdinalIgnoreCase))?.Stacks ?? 0;

        public KogMaw()
        {
            Oasys.SDK.InputProviders.KeyboardProvider.OnKeyPress += KeyboardProvider_OnKeyPress;
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                AllowCollision = (target, collisions) => !collisions.Any(),
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => QHitChance,
                Range = () => QMaxRange,
                Radius = () => 140,
                Speed = () => 1650,
                IsEnabled = () => UseQ,
                MinimumMana = () => QMinMana,
                TargetSelect = (mode) => SpellQ.GetTargets(mode).FirstOrDefault()
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                IsEnabled = () => UseW,
                MinimumMana = () => WMinMana,
                ShouldCast = (mode, target, spellClass, damage) => UnitManager.EnemyChampions.Any(x => x.Distance < GetAttackRangeWithWActive() && TargetSelector.IsAttackable(x))
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => EHitChance,
                Range = () => EMaxRange,
                Radius = () => 240,
                Speed = () => 1400,
                IsEnabled = () => UseE,
                MinimumMana = () => EMinMana,
                TargetSelect = (mode) => SpellE.GetTargets(mode).FirstOrDefault()
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Circle,
                MinimumHitChance = () => RHitChance,
                Range = () => UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.R).Level * 250 + 1050,
                Radius = () => 240,
                Speed = () => 1500,
                Delay = () => 0.9f,
                Damage = (target, spellClass) =>
                            target != null
                            ? DamageCalculator.GetMagicResistMod(UnitManager.MyChampion, target) *
                                        ((200 + spellClass.Level * 150) +
                                        (UnitManager.MyChampion.UnitStats.BonusAttackDamage) +
                                        (UnitManager.MyChampion.UnitStats.TotalAbilityPower))
                            : 0,
                IsEnabled = () => UseR && UltCost <= RMaxManaCost,
                MinimumMana = () => RMinMana,
                TargetSelect = (mode) => SpellR.GetTargets(mode, x => x.HealthPercent <= RTargetMaxHPPercent && ROnlyOutSideAttackRange(x) && x.Distance <= RMaximumRange).FirstOrDefault()
            };
            SpellRSemiAuto = new Spell(CastSlot.R, SpellSlot.R)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Circle,
                MinimumHitChance = () => SemiAutoRHitChance,
                Range = () => UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.R).Level * 250 + 1050,
                Radius = () => 240,
                Speed = () => 1500,
                Delay = () => 0.9f,
                IsEnabled = () => UseSemiAutoR,
                TargetSelect = (mode) => SpellRSemiAuto.GetTargets(mode, x => x.Distance > RMinimumRange && x.Distance <= RMaximumRange).FirstOrDefault()
            };
        }

        private bool ROnlyOutSideAttackRange(GameObjectBase x)
        {
            return ROnlyOutsideOfAttackRange
                ? !TargetSelector.IsInRange(x)
                : x.Distance > RMinimumRange;
        }

        private void KeyboardProvider_OnKeyPress(Keys keyBeingPressed, Oasys.Common.Tools.Devices.Keyboard.KeyPressState pressState)
        {
            if (keyBeingPressed == SemiAutoRKey && pressState == Oasys.Common.Tools.Devices.Keyboard.KeyPressState.Down)
            {
                SpellRSemiAuto.ExecuteCastSpell();
            }
        }

        private static float GetAttackRangeWithWActive()
        {
            return UnitManager.MyChampion.TrueAttackRange + 110 + (20 * UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.W).Level);
        }

        internal override void OnCoreRender()
        {
            if (DrawWTime)
            {
                var wBuff = UnitManager.MyChampion.BuffManager.ActiveBuffs.FirstOrDefault(x => x.Name == "KogMawBioArcaneBarrage" && x.Stacks >= 1);
                if (wBuff != null && wBuff.RemainingDurationMs > 0)
                {
                    var qTimeRemaining = wBuff.RemainingDurationMs / 1000;
                    var w2s = LeagueNativeRendererManager.WorldToScreenSpell(UnitManager.MyChampion.Position);
                    w2s.Y -= 20;
                    RenderFactory.DrawText($"W:{qTimeRemaining:0.##}", 18, w2s, Color.Blue);
                }
            }
        }

        internal override void OnCoreMainInput()
        {
            if (SpellW.ExecuteCastSpell() || SpellQ.ExecuteCastSpell() || SpellR.ExecuteCastSpell() || SpellE.ExecuteCastSpell())
            {
                return;
            }
        }

        private int QMaxRange
        {
            get => QSettings.GetItem<Counter>("Q Max Range").Value;
            set => QSettings.GetItem<Counter>("Q Max Range").Value = value;
        }

        private bool DrawWTime
        {
            get => WSettings.GetItem<Switch>("Draw W Time").IsOn;
            set => WSettings.GetItem<Switch>("Draw W Time").IsOn = value;
        }

        private int EMaxRange
        {
            get => ESettings.GetItem<Counter>("E Max Range").Value;
            set => ESettings.GetItem<Counter>("E Max Range").Value = value;
        }

        private int RTargetMaxHPPercent
        {
            get => RSettings.GetItem<Counter>("R Target Max HP Percent").Value;
            set => RSettings.GetItem<Counter>("R Target Max HP Percent").Value = value;
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

        internal int RMaxManaCost
        {
            get => RSettings.GetItem<Counter>("R Max Mana Cost").Value;
            set => RSettings.GetItem<Counter>("R Max Mana Cost").Value = value;
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(KogMaw)}"));

            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new Counter() { Title = "Q Min Mana", MinValue = 0, MaxValue = 500, Value = 40, ValueFrequency = 10 });
            QSettings.AddItem(new Counter() { Title = "Q Max Range", MinValue = 0, MaxValue = 1200, Value = 1000, ValueFrequency = 25 });
            QSettings.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "VeryHigh" });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });
            WSettings.AddItem(new Counter() { Title = "W Min Mana", MinValue = 0, MaxValue = 500, Value = 0, ValueFrequency = 10 });
            WSettings.AddItem(new Switch() { Title = "Draw W Time", IsOn = true });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });
            ESettings.AddItem(new Counter() { Title = "E Min Mana", MinValue = 0, MaxValue = 500, Value = 150, ValueFrequency = 10 });
            ESettings.AddItem(new Counter() { Title = "E Max Range", MinValue = 0, MaxValue = 1350, Value = 1000, ValueFrequency = 25 });
            ESettings.AddItem(new ModeDisplay() { Title = "E HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "VeryHigh" });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            RSettings.AddItem(new Counter() { Title = "R Min Mana", MinValue = 0, MaxValue = 500, Value = 150, ValueFrequency = 10 });
            RSettings.AddItem(new Counter() { Title = "R Max Mana Cost", MinValue = 0, MaxValue = 500, Value = 150, ValueFrequency = 10 });
            RSettings.AddItem(new Counter() { Title = "R Target Max HP Percent", MinValue = 10, MaxValue = 100, Value = 40, ValueFrequency = 5 });
            RSettings.AddItem(new Switch() { Title = "R only outside of attack range", IsOn = true });
            RSettings.AddItem(new Counter() { Title = "R minimum range", MinValue = 0, MaxValue = 1800, Value = 0, ValueFrequency = 50 });
            RSettings.AddItem(new Counter() { Title = "R maximum range", MinValue = 0, MaxValue = 1800, Value = 1800, ValueFrequency = 50 });
            RSettings.AddItem(new ModeDisplay() { Title = "R HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "Immobile" });

            RSettings.AddItem(new Switch() { Title = "Use Semi Auto R", IsOn = true });
            RSettings.AddItem(new KeyBinding() { Title = "Semi Auto R Key", SelectedKey = Keys.T });
            RSettings.AddItem(new ModeDisplay() { Title = "Semi Auto R HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

        }
    }
}
