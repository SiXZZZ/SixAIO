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
    internal sealed class ChoGath : Champion
    {
        public ChoGath()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Circle,
                MinimumHitChance = () => QHitChance,
                Range = () => 950,
                Speed = () => 5000,
                Delay = () => 1.1f,
                Radius = () => 250,
                IsEnabled = () => UseQ,
                TargetSelect = (mode) => SpellQ.GetTargets(mode).FirstOrDefault()
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Cone,
                MinimumHitChance = () => WHitChance,
                Range = () => 650,
                Speed = () => 1500,
                Radius = () => 60,
                Delay = () => 0.5f,
                IsEnabled = () => UseW,
                TargetSelect = (mode) => SpellW.GetTargets(mode).FirstOrDefault()
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                IsEnabled = () => UseE,
                ShouldCast = (mode, target, spellClass, damage) => TargetSelector.IsAttackable(Orbwalker.TargetHero) && TargetSelector.IsInRange(Orbwalker.TargetHero),
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                IsTargetted = () => true,
                Range = () => UnitManager.MyChampion.TrueAttackRange,
                IsEnabled = () => UseR,
                TargetSelect = (mode) => SpellR.GetTargets(mode, x => x.Health < RDamage(x)).FirstOrDefault()
            };
        }

        private float RDamage(GameObjectBase target)
        {
            var isChampion = target.IsObject(ObjectTypeFlag.AIHeroClient);
            var baseDamage = isChampion
                ? 125 + SpellR.SpellClass.Level * 175
                : 1200;
            var magicScaleDamage = 0.5f * UnitManager.MyChampion.UnitStats.TotalAbilityPower;
            var healthScaleDamage = 0.1f * UnitManager.MyChampion.BonusHealth;
            var damage = baseDamage + magicScaleDamage + healthScaleDamage;
            var totalDamage = DamageCalculator.CalculateActualDamage(UnitManager.MyChampion, target, 0, 0, damage);
            return totalDamage;
        }

        internal override void OnCoreMainInput()
        {
            SpellR.ExecuteCastSpell();
            SpellQ.ExecuteCastSpell();
            SpellW.ExecuteCastSpell();
            SpellE.ExecuteCastSpell();
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(ChoGath)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });
            WSettings.AddItem(new ModeDisplay() { Title = "W HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
        }
    }
}
