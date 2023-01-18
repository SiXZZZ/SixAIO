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
    internal sealed class Kassadin : Champion
    {

        //RiftWalk

        // forcepulsecounter - forcepulsecancast

        // NetherBlade
        public Kassadin()
        {
            Spell.OnSpellCast += Spell_OnSpellCast;
            Orbwalker.OnOrbwalkerAfterBasicAttack += Orbwalker_OnOrbwalkerAfterBasicAttack;
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                IsTargetted = () => true,
                Range = () => 650,
                IsEnabled = () => UseQ,
                TargetSelect = (mode) => SpellQ.GetTargets(mode).FirstOrDefault()
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                IsEnabled = () => UseW,
                ShouldCast = (mode, target, spellClass, damage) =>
                            !Orbwalker.CanBasicAttack &&
                            TargetSelector.IsAttackable(Orbwalker.TargetHero) &&
                            TargetSelector.IsInRange(Orbwalker.TargetHero),
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                AllowCollision = (target, collisions) => !collisions.Any(),
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Cone,
                MinimumHitChance = () => EHitChance,
                Range = () => 600,
                Radius = () => 80,
                Speed = () => 2000,
                IsEnabled = () => UseE,
                TargetSelect = (mode) => SpellE.GetTargets(mode).FirstOrDefault()
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Circle,
                MinimumHitChance = () => RHitChance,
                Range = () => 500,
                Speed = () => 2000,
                Radius = () => 150,
                IsEnabled = () => UseR,
                TargetSelect = (mode) => SpellR.GetTargets(mode).FirstOrDefault()
            };
        }

        private void Spell_OnSpellCast(SDKSpell spell, GameObjectBase target)
        {
            if (spell.CastSlot == CastSlot.W)
            {
                Orbwalker.AttackReset();
            }
        }

        private void Orbwalker_OnOrbwalkerAfterBasicAttack(float gameTime, GameObjectBase target)
        {
            if (target != null)
            {
                SpellW.ExecuteCastSpell();
            }
        }

        internal override void OnCoreMainInput()
        {
            if (SpellR.ExecuteCastSpell() || SpellQ.ExecuteCastSpell() || SpellE.ExecuteCastSpell() || SpellW.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Kassadin)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });
            ESettings.AddItem(new ModeDisplay() { Title = "E HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            RSettings.AddItem(new ModeDisplay() { Title = "R HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });
        }
    }
}
