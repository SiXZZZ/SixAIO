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
    internal class Ezreal : Champion
    {
        public Ezreal()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                Damage = (target, spellClass) =>
                            target != null
                            ? Helpers.DamageCalculator.GetArmorMod(UnitManager.MyChampion, target) *
                            ((-5 + spellClass.Level * 25) +
                            (UnitManager.MyChampion.UnitStats.TotalAttackDamage * 1.3f) +
                            (UnitManager.MyChampion.UnitStats.TotalAbilityPower * 0.15f))
                            : 0,
                ShouldCast = (target, spellClass, damage) =>
                            UseQ &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 90 &&
                            target != null,
                TargetSelect = () =>
                            UnitManager.EnemyChampions
                            .Where(x => TargetSelector.IsAttackable(x) && x.Distance <= 1000 && x.IsAlive && !Collision.MinionCollision(x.Position, 140))
                            .OrderBy(x => x.Health)
                            .FirstOrDefault()
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                ShouldCast = (target, spellClass, damage) =>
                            UseW &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 30 &&
                            target != null,
                TargetSelect = () =>
                            UnitManager.EnemyChampions
                            .Where(x => TargetSelector.IsAttackable(x))
                            .Where(x => x.Distance <= 1000 && x.IsAlive)
                            //.Where(x => x.BuffManager.GetBuffList().Any(buff => buff.IsActive && buff.EntryType == BuffType.Slow || BuffChecker.IsCrowdControlled(buff)))
                            .FirstOrDefault()
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                CastTime = 1f,
                ShouldCast = (target, spellClass, damage) =>
                            UseE &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 75 &&
                            target != null,
                TargetSelect = () =>
                            UnitManager.EnemyChampions
                            .Where(x => TargetSelector.IsAttackable(x))
                            .Where(x => x.Distance <= 800 && x.IsAlive)
                            .FirstOrDefault()
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                CastTime = 1f,
                Damage = (target, spellClass) =>
                            target != null
                            ? Helpers.DamageCalculator.GetMagicResistMod(UnitManager.MyChampion, target) *
                                        ((200 + spellClass.Level * 150) +
                                        (UnitManager.MyChampion.UnitStats.BonusAttackDamage) +
                                        (UnitManager.MyChampion.UnitStats.TotalAbilityPower))
                            : 0,
                ShouldCast = (target, spellClass, damage) =>
                            UseR &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 100 &&
                            target != null &&
                            target.Health < damage,
                TargetSelect = () =>
                            UnitManager.EnemyChampions
                            .Where(x => TargetSelector.IsAttackable(x))
                            .Where(x => x.Distance <= 30000 && x.IsAlive)
                            //.Where(x => x.BuffManager.GetBuffList().Any(buff => buff.IsActive && buff.EntryType == BuffType.Slow || BuffChecker.IsCrowdControlled(buff)))
                            .FirstOrDefault()
            };

        }

        internal override void OnCoreMainInput()
        {
            if (SpellW.ExecuteCastSpell() || SpellQ.ExecuteCastSpell() || SpellR.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Ezreal)}"));
            MenuTab.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            MenuTab.AddItem(new Switch() { Title = "Use W", IsOn = true });
            MenuTab.AddItem(new Switch() { Title = "Use E", IsOn = false });
            MenuTab.AddItem(new Switch() { Title = "Use R", IsOn = true });
        }
    }
}
