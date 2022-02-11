using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SixAIO.Helpers;
using SixAIO.Models;
using System.Linq;

namespace SixAIO.Champions
{
    internal class Caitlyn : Champion
    {
        public Caitlyn()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
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
                ShouldCast = (target, spellClass, damage) =>
                            UseQ &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 90 &&
                            target != null,
                TargetSelect = (mode) => 
                            UnitManager.EnemyChampions
                            .FirstOrDefault(x => x.Distance <= 1250 && x.IsAlive && TargetSelector.IsAttackable(x) && BuffChecker.IsCrowdControlledOrSlowed(x))
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                ShouldCast = (target, spellClass, damage) =>
                            UseW &&
                            spellClass.IsSpellReady &&
                            spellClass.Charges >= 1 &&
                            UnitManager.MyChampion.Mana > 30 &&
                            target != null,
                TargetSelect = (mode) => 
                            UnitManager.EnemyChampions
                            .FirstOrDefault(x => x.Distance <= 780 && x.IsAlive && TargetSelector.IsAttackable(x) && BuffChecker.IsCrowdControlled(x))
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                Range = () => 800,
                Radius = () => 140,
                Speed = () => 1600,
                ShouldCast = (target, spellClass, damage) =>
                            UseE &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 75 &&
                            target != null,
                TargetSelect = (mode) => 
                            UnitManager.EnemyChampions
                            .FirstOrDefault(x => x.Distance <= 700 && x.IsAlive && TargetSelector.IsAttackable(x) && !Collision.MinionCollision(x.W2S, 140))
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                Delay = () => 1f,
                Damage = (target, spellClass) =>
                            target != null
                            ? DamageCalculator.GetArmorMod(UnitManager.MyChampion, target) *
                            ((75 + spellClass.Level * 225) +
                            (UnitManager.MyChampion.UnitStats.BonusAttackDamage * 2f))
                            : 0,
                ShouldCast = (target, spellClass, damage) =>
                            UseR &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 100 &&
                            !UnitManager.EnemyChampions.Any(x => x.Distance < 1000) &&
                            target != null &&
                            target.Health < damage,
                TargetSelect = (mode) => 
                            UnitManager.EnemyChampions
                            .FirstOrDefault(x => x.Distance <= 3500 && x.IsAlive && TargetSelector.IsAttackable(x))
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
            MenuTab.AddItem(new Switch() { Title = "Use W", IsOn = true });
            MenuTab.AddItem(new Switch() { Title = "Use E", IsOn = true });
            MenuTab.AddItem(new Switch() { Title = "Use R", IsOn = true });
        }
    }
}
