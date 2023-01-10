using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SixAIO.Models;
using System;
using System.Linq;

namespace SixAIO.Champions
{
    internal sealed class Lux : Champion
    {
        public Lux()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                AllowCollision = (target, collisions) => collisions.Count() <= 1,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => QHitChance,
                Range = () => 1250,
                Radius = () => 140,
                Speed = () => 1200,
                IsEnabled = () => UseQ,
                TargetSelect = (mode) => SpellQ.GetTargets(mode, x => !TargetSelector.IsInvulnerable(x, Oasys.Common.Logic.DamageType.Magical, false)).FirstOrDefault()
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Circle,
                MinimumHitChance = () => EHitChance,
                Range = () => 1100,
                Speed = () => 5000,
                Radius = () => 310,
                IsEnabled = () => UseE,
                TargetSelect = (mode) => SpellE.GetTargets(mode).FirstOrDefault()
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => RHitChance,
                Range = () => 3400,
                Radius = () => 200,
                Speed = () => 1000,
                Delay = () => 1f,
                IsEnabled = () => UseR,
                TargetSelect = (mode) => SpellR.GetTargets(mode).FirstOrDefault()
            };
        }

        internal override void OnCoreMainInput()
        {
            if (SpellE.ExecuteCastSpell() || SpellQ.ExecuteCastSpell() ||/* SpellW.ExecuteCastSpell() ||*/ SpellR.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Lux)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });
            ESettings.AddItem(new ModeDisplay() { Title = "E HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            RSettings.AddItem(new ModeDisplay() { Title = "R HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

        }
    }
}
