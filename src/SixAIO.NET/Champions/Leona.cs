using Oasys.Common.Enums.GameEnums;
using Oasys.Common.GameObject;
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
    internal sealed class Leona : Champion
    {
        public Leona()
        {
            Orbwalker.OnOrbwalkerAfterBasicAttack += Orbwalker_OnOrbwalkerAfterBasicAttack;
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                IsEnabled = () => UseQ,
                ShouldCast = (mode, target, spellClass, damage) => TargetSelector.IsAttackable(Orbwalker.TargetHero) && TargetSelector.IsInRange(Orbwalker.TargetHero),
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                IsEnabled = () => UseE,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => EHitChance,
                Range = () => 875,
                Radius = () => 140,
                Speed = () => 2000,
                TargetSelect = (mode) => SpellE.GetTargets(mode).FirstOrDefault()
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                IsEnabled = () => UseR,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Circle,
                MinimumHitChance = () => RHitChance,
                Range = () => 1200,
                Speed = () => 1000,
                Radius = () => 180,
                TargetSelect = (mode) => SpellR.GetTargets(mode).FirstOrDefault()
            };
        }

        private void Orbwalker_OnOrbwalkerAfterBasicAttack(float gameTime, GameObjectBase target)
        {
            if (SpellQ.ExecuteCastSpell())
            {
                Orbwalker.AttackReset();
            }
        }

        internal override void OnCoreMainInput()
        {
            if (SpellE.ExecuteCastSpell() || SpellR.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Leona)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });
            ESettings.AddItem(new ModeDisplay() { Title = "E HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            RSettings.AddItem(new ModeDisplay() { Title = "R HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });
        }
    }
}
