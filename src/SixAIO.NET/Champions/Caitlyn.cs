using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SixAIO.Helpers;
using SixAIO.Models;
using System;
using System.Linq;

namespace SixAIO.Champions
{
    internal class Caitlyn : Champion
    {
        public Caitlyn()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => QHitChance,
                Range = () => 1300,
                Radius = () => 120,
                Speed = () => 2200,
                Delay = () => 0.8f,
                Damage = (target, spellClass) =>
                            target != null
                            ? DamageCalculator.GetArmorMod(UnitManager.MyChampion, target) *
                            ((10 + spellClass.Level * 40) +
                            (UnitManager.MyChampion.UnitStats.TotalAttackDamage * (1.15f + 0.15f * spellClass.Level)))
                            : 0,
                IsEnabled = () => UseQ,
                TargetSelect = (mode) => SpellQ.GetTargets(mode).FirstOrDefault()
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                IsTargetted = () => true,
                Range = () => 780,
                IsEnabled = () => UseW,
                MinimumCharges = () => 1,
                TargetSelect = (mode) => SpellW.GetTargets(mode, x => BuffChecker.IsCrowdControlled(x)).FirstOrDefault()
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                AllowCollision = (target, collisions) => !collisions.Any(),
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => EHitChance,
                Range = () => 800,
                Radius = () => 140,
                Speed = () => 1600,
                IsEnabled = () => UseE,
                TargetSelect = (mode) => SpellE.GetTargets(mode).FirstOrDefault()
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                AllowCastOnMap = () => AllowRCastOnMinimap,
                IsTargetted = () => true,
                Delay = () => 1f,
                Range = () => 3500f,
                Damage = (target, spellClass) =>
                            target != null
                            ? DamageCalculator.GetArmorMod(UnitManager.MyChampion, target) *
                            ((75 + spellClass.Level * 225) +
                            (UnitManager.MyChampion.UnitStats.BonusAttackDamage * 2f))
                            : 0,
                IsEnabled = () => UseR,
                IsSpellReady = (spellClass, minimumMana, minimumCharges) => spellClass.IsSpellReady && UnitManager.MyChampion.Mana >= spellClass.SpellData.ResourceCost && !UnitManager.EnemyChampions.Any(x => x.Distance < 1000),
                ShouldCast = (mode, target, spellClass, damage) => target != null && target.Health < damage,
                TargetSelect = (mode) => SpellR.GetTargets(mode).FirstOrDefault()
            };
        }

        internal override void OnCoreMainInput()
        {
            if (SpellW.ExecuteCastSpell() || SpellE.ExecuteCastSpell() || SpellQ.ExecuteCastSpell() || SpellR.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Caitlyn)}"));
            MenuTab.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            MenuTab.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            MenuTab.AddItem(new Switch() { Title = "Use W", IsOn = true });
            MenuTab.AddItem(new Switch() { Title = "Use E", IsOn = true });
            MenuTab.AddItem(new ModeDisplay() { Title = "E HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            MenuTab.AddItem(new Switch() { Title = "Use R", IsOn = true });
            MenuTab.AddItem(new Switch() { Title = "Allow R cast on minimap", IsOn = false });
        }
    }
}
