using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Extensions;
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
    internal sealed class Lillia : Champion
    {
        private Spell SpellWLaneclear;

        public Lillia()
        {
            Oasys.SDK.InputProviders.KeyboardProvider.OnKeyPress += KeyboardProvider_OnKeyPress;
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                IsEnabled = () => UseQ,
                ShouldCast = (mode, target, spellClass, damage) =>
                {
                    if (mode == Orbwalker.OrbWalkingModeType.LaneClear)
                    {
                        return UnitManager.EnemyMinions.Any(x => x.Distance <= 380 && TargetSelector.IsAttackable(x)) ||
                               UnitManager.EnemyJungleMobs.Any(x => x.Distance <= 380 && TargetSelector.IsAttackable(x)) ||
                               UnitManager.EnemyChampions.Any(x => x.Distance <= 380 && TargetSelector.IsAttackable(x));
                    }
                    return UnitManager.EnemyChampions.Any(x => x.Distance <= 380 && TargetSelector.IsAttackable(x));
                },
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Circle,
                MinimumHitChance = () => WHitChance,
                Range = () => 500,
                Speed = () => 10_000,
                Radius = () => 100,
                Delay = () => 0.759f,
                IsEnabled = () => UseW,
                TargetSelect = (mode) => SpellW.GetTargets(mode).FirstOrDefault()
            };
            SpellWLaneclear = new Spell(CastSlot.W, SpellSlot.W)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Circle,
                MinimumHitChance = () => Prediction.MenuSelected.HitChance.High,
                Range = () => 500,
                Speed = () => 10_000,
                Radius = () => 100,
                Delay = () => 0.759f,
                IsEnabled = () => UseW,
                TargetSelect = (mode) => SpellWLaneclear.GetTargets(mode).FirstOrDefault()
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => EHitChance,
                Speed = () => 1400,
                Radius = () => 120,
                Range = () => 30000,
                Delay = () => 0.4f,
                IsEnabled = () => UseE,
                TargetSelect = (mode) =>
                {
                    if (mode == Orbwalker.OrbWalkingModeType.LaneClear)
                    {
                        return SpellE.GetTargets(mode, x => x.W2S.IsValid()).OrderBy(x => x.Distance).FirstOrDefault();
                    }
                    return SpellE.GetTargets(mode).FirstOrDefault();
                }
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                IsEnabled = () => UseR,
                ShouldCast = (mode, target, spellClass, damage) =>
                        RIfMoreThanEnemiesNear < UnitManager.EnemyChampions.Count(x =>
                                    TargetSelector.IsAttackable(x) &&
                                    x.Distance < REnemiesCloserThan &&
                                    x.BuffManager.ActiveBuffs.Any(buff => buff.Stacks >= 1 && buff.Name == "LilliaPDoT")),
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
            SpellE.ExecuteCastSpell();
            SpellQ.ExecuteCastSpell();
            SpellW.ExecuteCastSpell();
            SpellR.ExecuteCastSpell();
        }

        internal override void OnCoreLaneClearInput()
        {
            if (UseELaneclear && SpellE.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.LaneClear))
            {
                return;
            }
            if (UseWLaneclear && SpellWLaneclear.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.LaneClear))
            {
                return;
            }
            if (UseQLaneclear && SpellQ.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.LaneClear))
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

        public Keys DisableAAKey => MenuTab.GetItem<KeyBinding>("Disable AA Key").SelectedKey;


        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Lillia)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));
            MenuTab.AddItem(new KeyBinding() { Title = "Disable AA Key", SelectedKey = Keys.U });

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Use Q Laneclear", IsOn = true });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });
            WSettings.AddItem(new Switch() { Title = "Use W Laneclear", IsOn = true });
            WSettings.AddItem(new ModeDisplay() { Title = "W HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "VeryHigh" });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });
            ESettings.AddItem(new Switch() { Title = "Use E Laneclear", IsOn = true });
            ESettings.AddItem(new ModeDisplay() { Title = "E HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "VeryHigh" });


            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            RSettings.AddItem(new Counter() { Title = "R If More Than Enemies Near", MinValue = 0, MaxValue = 5, Value = 1, ValueFrequency = 1 });
            RSettings.AddItem(new Counter() { Title = "R Enemies Closer Than", MinValue = 100, MaxValue = 20_000, Value = 1500, ValueFrequency = 100 });
        }
    }
}
