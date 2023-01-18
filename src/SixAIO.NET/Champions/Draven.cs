using Oasys.Common.Enums.GameEnums;
using Oasys.Common.GameObject;
using Oasys.Common.GameObject.Clients;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.Rendering;
using Oasys.SDK.SpellCasting;
using SharpDX;
using SixAIO.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace SixAIO.Champions
{
    internal sealed class Draven : Champion
    {
        private static List<GameObjectBase> _axes = new();
        private static IEnumerable<GameObjectBase> Axes() => _axes.Where(x => x.Distance <= 2000).Where(IsAxe);

        private static bool IsAxe(GameObjectBase x)
        {
            return x.Name.Contains("reticle_self", StringComparison.OrdinalIgnoreCase);
        }

        private static int PassiveStacks()
        {
            var buff = UnitManager.MyChampion.BuffManager.GetActiveBuff("dravenpassivestacks");
            return buff != null ? (int)buff.Stacks : 0;
        }

        private static int QStacks()
        {
            var buff1 = UnitManager.MyChampion.BuffManager.GetActiveBuff("DravenSpinning");
            var buff2 = UnitManager.MyChampion.BuffManager.GetActiveBuff("dravenspinningleft");

            var stacks = buff1 != null && buff1.IsActive ? (int)buff1.Stacks : 0;
            stacks += buff2 != null && buff2.IsActive ? (int)buff2.Stacks : 0;
            //Logger.Log("Q Stacks " + stacks);
            return stacks;
        }

        private static float GetRDamage(GameObjectBase target)
        {
            var rLevel = UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.R).Level;
            var dmg = (0.9f + rLevel * 0.2f) * UnitManager.MyChampion.UnitStats.BonusAttackDamage + 75 + 100 * rLevel;
            var effectiveDamage = DamageCalculator.GetArmorMod(UnitManager.MyChampion, target) * dmg;
            return target.Health - PassiveStacks() - effectiveDamage < 0
                    ? PassiveStacks() + effectiveDamage
                    : effectiveDamage;
        }

        public Draven()
        {
            Oasys.SDK.InputProviders.KeyboardProvider.OnKeyPress += KeyboardProvider_OnKeyPress;
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                Delay = () => 0f,
                IsEnabled = () => UseQ,
                ShouldCast = (mode, target, spellClass, damage) =>
                            TargetSelector.IsAttackable(Orbwalker.TargetHero) &&
                            TargetSelector.IsInRange(Orbwalker.TargetHero) &&
                            QStacks() <= 1,
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                Delay = () => 0f,
                IsEnabled = () => UseW,
                ShouldCast = (mode, target, spellClass, damage) =>
                            TargetSelector.IsAttackable(Orbwalker.TargetHero) &&
                            TargetSelector.IsInRange(Orbwalker.TargetHero) &&
                            QStacks() > 0,
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => EHitChance,
                Range = () => EMaximumRange,
                Radius = () => 260,
                Speed = () => 1400,
                IsEnabled = () => UseE,
                TargetSelect = (mode) => SpellE.GetTargets(mode).FirstOrDefault()
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                AllowCastOnMap = () => AllowRCastOnMinimap,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => RHitChance,
                Delay = () => 0.5f,
                Range = () => 30000,
                Radius = () => 320,
                Speed = () => 2000,
                IsEnabled = () => UseR,
                TargetSelect = (mode) => SpellR.GetTargets(mode, x => x.Distance > RMinimumRange && x.Distance <= RMaximumRange).FirstOrDefault(x => x.Health < GetRDamage(x) || x.HealthPercent <= RTargetMaxHPPercent)
            };
            SpellRSemiAuto = new Spell(CastSlot.R, SpellSlot.R)
            {
                AllowCastOnMap = () => AllowRCastOnMinimap,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => SemiAutoRHitChance,
                Delay = () => 0.5f,
                Range = () => 30000,
                Radius = () => 320,
                Speed = () => 2000,
                IsEnabled = () => UseSemiAutoR,
                TargetSelect = (mode) => SpellRSemiAuto.GetTargets(mode, x => x.Distance > RMinimumRange && x.Distance <= RMaximumRange).FirstOrDefault()
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
            CatchAxe();

            if (SpellQ.ExecuteCastSpell() || SpellE.ExecuteCastSpell() || SpellW.ExecuteCastSpell() || SpellR.ExecuteCastSpell())
            {
                return;
            }
        }

        private void CatchAxe()
        {
            if (UseQCatchRange)
            {
                var axes = Axes();
                if (axes.Any())
                {
                    var catchAxe = axes.Where(x => (QCatchMode == CatchMode.Mouse && x.DistanceTo(GameEngine.WorldMousePosition) <= QCatchRange) ||
                                                   (QCatchMode == CatchMode.Self && x.Distance <= QCatchRange))
                                       .OrderBy(x => x.DistanceTo(GameEngine.WorldMousePosition))
                                       .FirstOrDefault();
                    if (catchAxe != null)
                    {
                        Orbwalker.ForceMovePosition = catchAxe.W2S;
                    }
                    else
                    {
                        Orbwalker.ForceMovePosition = Vector2.Zero;
                    }
                }
                else
                {
                    Orbwalker.ForceMovePosition = Vector2.Zero;
                }
            }
            //try something with if w is ready =/= axe is catched and store the current axe to catch to filter it out from the rest
        }

        internal override void OnCreateObject(AIBaseClient obj)
        {
            if (IsAxe(obj))
            {
                _axes.Add(obj);
            }
        }

        internal override void OnDeleteObject(AIBaseClient obj)
        {
            _axes.Remove(obj);
        }

        internal override void OnCoreMainInputRelease()
        {
            Orbwalker.ForceMovePosition = Vector2.Zero;
        }

        private int _cycle = 0;
        internal override void OnCoreMainTick()
        {
            _cycle++;
            if (_cycle % 10 != 0)
            {
                return;
            }

            if (!Oasys.SDK.InputProviders.KeyboardProvider.IsKeyPressed(Oasys.Common.Settings.Orbwalker.GetComboKey()) &&
                !Oasys.SDK.InputProviders.KeyboardProvider.IsKeyPressed(Oasys.Common.Settings.Orbwalker.GetHarassKey()) &&
                !Oasys.SDK.InputProviders.KeyboardProvider.IsKeyPressed(Oasys.Common.Settings.Orbwalker.GetLaneclearKey()) &&
                !Oasys.SDK.InputProviders.KeyboardProvider.IsKeyPressed(Oasys.Common.Settings.Orbwalker.GetLasthitKey()))
            {
                Orbwalker.ForceMovePosition = Vector2.Zero;
            }
        }

        internal override void OnCoreRender()
        {
            try
            {
                if (DrawQCatchRange && !Oasys.Common.Tools.Devices.Mouse.InUse)
                {
                    switch (QCatchMode)
                    {
                        case CatchMode.Mouse:
                            RenderFactory.DrawNativeCircle(GameEngine.WorldMousePosition, QCatchRange, Color.White, 2);
                            break;
                        case CatchMode.Self:
                            RenderFactory.DrawNativeCircle(UnitManager.MyChampion.Position, QCatchRange, Color.White, 2);
                            break;
                    }
                }

                if (DrawAxes)
                {
                    foreach (var item in Axes())
                    {
                        try
                        {
                            var color = Oasys.Common.Tools.ColorConverter.GetColor(DrawAxesColor, 255);
                            RenderFactory.DrawNativeCircle(item.Position, 120, color, 2);
                            //RenderFactory.DrawText(item.Name, 18, item.W2S, Color.White);
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        internal override void OnCoreLaneClearInput()
        {
            CatchAxe();

            if (SpellQ.ExecuteCastSpell())
            {
                return;
            }
        }

        private bool UseQCatchRange
        {
            get => QSettings.GetItem<Switch>("Use Q Catch Range").IsOn;
            set => QSettings.GetItem<Switch>("Use Q Catch Range").IsOn = value;
        }

        private bool DrawQCatchRange
        {
            get => QSettings.GetItem<Switch>("Draw Q Catch Range").IsOn;
            set => QSettings.GetItem<Switch>("Draw Q Catch Range").IsOn = value;
        }

        private bool DrawAxes
        {
            get => QSettings.GetItem<Switch>("Draw Axes").IsOn;
            set => QSettings.GetItem<Switch>("Draw Axes").IsOn = value;
        }

        private string DrawAxesColor
        {
            get => QSettings.GetItem<ModeDisplay>("Draw Axes Color").SelectedModeName;
            set => QSettings.GetItem<ModeDisplay>("Draw Axes Color").SelectedModeName = value;
        }

        private int QCatchRange
        {
            get => QSettings.GetItem<Counter>("Q Catch Range").Value;
            set => QSettings.GetItem<Counter>("Q Catch Range").Value = value;
        }

        private CatchMode QCatchMode
        {
            get => (CatchMode)Enum.Parse(typeof(CatchMode), QSettings.GetItem<ModeDisplay>("Q Catch Mode").SelectedModeName);
            set => QSettings.GetItem<ModeDisplay>("Q Catch Mode").SelectedModeName = value.ToString();
        }

        private enum CatchMode
        {
            Mouse,
            Self
        }

        private int EMaximumRange
        {
            get => ESettings.GetItem<Counter>("E maximum range").Value;
            set => ESettings.GetItem<Counter>("E maximum range").Value = value;
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
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Draven)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Use Q Catch Range", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Draw Q Catch Range", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Draw Axes", IsOn = true });
            QSettings.AddItem(new ModeDisplay() { Title = "Draw Axes Color", ModeNames = Oasys.Common.Tools.ColorConverter.GetColors(), SelectedModeName = "Blue" });
            QSettings.AddItem(new Counter() { Title = "Q Catch Range", MinValue = 0, MaxValue = 800, Value = 500, ValueFrequency = 10 });
            QSettings.AddItem(new ModeDisplay() { Title = "Q Catch Mode", ModeNames = Enum.GetNames(typeof(CatchMode)).ToList(), SelectedModeName = "Mouse" });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });
            ESettings.AddItem(new ModeDisplay() { Title = "E HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });
            ESettings.AddItem(new Counter() { Title = "E maximum range", MinValue = 0, MaxValue = 1100, Value = 1100, ValueFrequency = 50 });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            RSettings.AddItem(new ModeDisplay() { Title = "R HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            RSettings.AddItem(new Switch() { Title = "Use Semi Auto R", IsOn = true });
            RSettings.AddItem(new KeyBinding() { Title = "Semi Auto R Key", SelectedKey = Keys.T });
            RSettings.AddItem(new ModeDisplay() { Title = "Semi Auto R HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            RSettings.AddItem(new Switch() { Title = "Allow R cast on minimap", IsOn = true });
            RSettings.AddItem(new Counter() { Title = "R minimum range", MinValue = 0, MaxValue = 30_000, Value = 0, ValueFrequency = 50 });
            RSettings.AddItem(new Counter() { Title = "R maximum range", MinValue = 0, MaxValue = 30_000, Value = 30_000, ValueFrequency = 50 });
            RSettings.AddItem(new Counter() { Title = "R Target Max HP Percent", MinValue = 10, MaxValue = 100, Value = 50, ValueFrequency = 5 });
        }
    }
}
