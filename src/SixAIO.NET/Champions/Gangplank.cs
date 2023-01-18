using Oasys.Common.Enums.GameEnums;
using Oasys.Common.GameObject;
using Oasys.Common.GameObject.Clients;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SixAIO.Helpers;
using SixAIO.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using static Oasys.Common.Logic.Orbwalker;

namespace SixAIO.Champions
{
    internal sealed class Gangplank : Champion
    {
        private bool _originalTargetChampsOnlySetting;

        private static List<GameObjectBase> _barrels = new();
        private static IEnumerable<GameObjectBase> Barrels() => _barrels.Where(x => x.Distance <= 2000).Where(IsBarrel);

        private static bool IsBarrel(GameObjectBase x)
        {
            return x.Name.Contains("Barrel", StringComparison.OrdinalIgnoreCase);
        }

        public Gangplank()
        {
            Oasys.SDK.InputProviders.KeyboardProvider.OnKeyPress += KeyboardProvider_OnKeyPress;
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                Range = () => 625,
                IsTargetted = () => true,
                IsEnabled = () => UseQ,
                TargetSelect = (mode) =>
                {
                    var targetBarrels = Barrels().Where(x => UnitManager.EnemyChampions.Any(enemy => enemy.DistanceTo(x.Position) <= 330));
                    if (targetBarrels is not null && targetBarrels.Any())
                    {
                        return targetBarrels.FirstOrDefault(x => x.Distance <= 625);
                    }

                    return SpellQ.GetTargets(mode).FirstOrDefault();
                }
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                IsEnabled = () => UseW,
                ShouldCast = (mode, target, spellClass, damage) => BuffChecker.IsCrowdControlled(UnitManager.MyChampion),
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Circle,
                MinimumHitChance = () => EHitChance,
                Range = () => 1000,
                Radius = () => 340,
                Speed = () => 2000,
                IsEnabled = () => UseE,
                MinimumCharges = () => 1,
                TargetSelect = (mode) => SpellE.GetTargets(mode).FirstOrDefault()
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Circle,
                MinimumHitChance = () => RHitChance,
                Range = () => 30_000,
                Radius = () => 550,
                Speed = () => 5000,
                IsEnabled = () => UseR,
                TargetSelect = (mode) => SpellR.GetTargets(mode).FirstOrDefault()
            };
            SpellRSemiAuto = new Spell(CastSlot.R, SpellSlot.R)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Circle,
                MinimumHitChance = () => SemiAutoRHitChance,
                Range = () => 30_000,
                Radius = () => 550,
                Speed = () => 5000,
                IsEnabled = () => UseSemiAutoR,
                TargetSelect = (mode) => SpellRSemiAuto.GetTargets(mode).FirstOrDefault()
            };
        }

        private void KeyboardProvider_OnKeyPress(Keys keyBeingPressed, Oasys.Common.Tools.Devices.Keyboard.KeyPressState pressState)
        {
            if (keyBeingPressed == SemiAutoRKey && pressState == Oasys.Common.Tools.Devices.Keyboard.KeyPressState.Down)
            {
                SpellRSemiAuto.ExecuteCastSpell();
            }
        }

        internal override void OnCreateObject(AIBaseClient obj)
        {
            if (IsBarrel(obj))
            {
                _barrels.Add(obj);
            }
        }

        internal override void OnDeleteObject(AIBaseClient obj)
        {
            _barrels.Remove(obj);
        }

        internal override void OnCoreMainInput()
        {
            if (Orbwalker.TargetChampionsOnly && SpellQ.CanExecuteCastSpell())
            {
                var tempTargetChamps = OrbSettings.TargetChampionsOnly;
                OrbSettings.TargetChampionsOnly = false;
                var casted = SpellQ.ExecuteCastSpell();
                OrbSettings.TargetChampionsOnly = tempTargetChamps;
                if (casted)
                {
                    return;
                }
            }
            else
            {
                if (SpellQ.ExecuteCastSpell())
                {
                    return;
                }
            }

            if (SpellE.ExecuteCastSpell() || SpellW.ExecuteCastSpell() || SpellR.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void OnCoreLaneClearInput()
        {
            if (SpellQ.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.LaneClear))
            {
                return;
            }
        }

        internal override void OnCoreHarassInput()
        {
            if (SpellQ.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.Mixed))
            {
                return;
            }
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Gangplank)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });
            ESettings.AddItem(new ModeDisplay() { Title = "E HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "VeryHigh" });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            RSettings.AddItem(new ModeDisplay() { Title = "R HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "VeryHigh" });

            RSettings.AddItem(new Switch() { Title = "Use Semi Auto R", IsOn = true });
            RSettings.AddItem(new KeyBinding() { Title = "Semi Auto R Key", SelectedKey = Keys.T });
            RSettings.AddItem(new ModeDisplay() { Title = "Semi Auto R HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

        }
    }
}
