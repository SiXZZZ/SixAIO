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
    internal class KogMaw : Champion
    {
        public KogMaw()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                CastTime = 0.3f,
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
                            UnitManager.EnemyChampions.All(x => x.Distance > UnitManager.MyChampion.AttackRange - 150) &&
                            target != null,
                TargetSelect = () =>
                            UnitManager.EnemyChampions
                            .Where(x => TargetSelector.IsAttackable(x) && x.Distance <= 1000 && x.IsAlive)
                            .OrderBy(x => x.Health)
                            .FirstOrDefault()
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                CastTime = 0.3f,
                ShouldCast = (target, spellClass, damage) =>
                            UseW &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 90 &&
                            UnitManager.EnemyChampions.Any(x => x.Distance < UnitManager.MyChampion.AttackRange + 100) &&
                            target != null,
                TargetSelect = () =>
                {
                    var target = Orbwalker.TargetHero;
                    return TargetSelector.IsAttackable(target) && TargetSelector.IsInRange(target) ? target : null;
                }
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                CastTime = 0.3f,
                ShouldCast = (target, spellClass, damage) =>
                            UseE &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 75 &&
                            UnitManager.EnemyChampions.All(x => x.Distance > UnitManager.MyChampion.AttackRange - 150) &&
                            target != null,
                TargetSelect = () =>
                            UnitManager.EnemyChampions
                            .Where(x => TargetSelector.IsAttackable(x))
                            .Where(x => x.Distance <= 800 && x.IsAlive)
                            .FirstOrDefault()
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                CastTime = 0.3f,
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
                            UnitManager.EnemyChampions.All(x => x.Distance > UnitManager.MyChampion.AttackRange) &&
                            target != null &&
                            target.Health < damage,
                TargetSelect = () =>
                            UnitManager.EnemyChampions
                            .Where(x => TargetSelector.IsAttackable(x))
                            .Where(x => x.BuffManager.GetBuffList().Any(buff => buff.IsActive && buff.EntryType == BuffType.Slow || BuffChecker.IsCrowdControlled(buff)))
                            .FirstOrDefault()
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
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(KogMaw)}"));
            MenuTab.AddItem(new Switch() { Title = "Use Q", IsOn = false });
            MenuTab.AddItem(new Switch() { Title = "Use W", IsOn = false });
            MenuTab.AddItem(new Switch() { Title = "Use E", IsOn = false });
            MenuTab.AddItem(new Switch() { Title = "Use R", IsOn = false });
        }
    }
}
