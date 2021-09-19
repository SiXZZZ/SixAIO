using Oasys.Common.Enums.GameEnums;
using Oasys.Common.GameObject;
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
    internal class Cassiopeia : Champion
    {
        public Cassiopeia()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                CastTime = 0.5f,
                ShouldCast = (target, spellClass, damage) =>
                            UseQ &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 90 &&
                            target != null,
                TargetSelect = () =>
                            UnitManager.EnemyChampions
                            .Where(x => x.IsAlive && TargetSelector.IsAttackable(x) && x.Distance <= 850)
                            .Where(x => x.BuffManager.GetBuffList().Any(buff => buff.IsActive && buff.EntryType == BuffType.Slow || BuffChecker.IsCrowdControlled(buff)))
                            .OrderBy(x => x.Health)
                            .FirstOrDefault()
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                CastTime = 0.5f,
                ShouldCast = (target, spellClass, damage) =>
                            UseE &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 50 &&
                            target != null,
                TargetSelect = () =>
                {
                    var champTarget = UnitManager.EnemyChampions
                                     .Where(x => x.IsAlive && TargetSelector.IsAttackable(x) && x.Distance <= 700)
                                     .OrderBy(x => x.Health)
                                     .FirstOrDefault();
                    if (champTarget != null)
                    {
                        return champTarget;
                    }

                    return Orbwalker.TargetChampionsOnly
                                    ? null
                                    : UnitManager.Enemies.Where(x => !x.IsObject(ObjectTypeFlag.BuildingProps) && !x.IsObject(ObjectTypeFlag.AITurretClient))
                                                         .Where(x => x.IsAlive && TargetSelector.IsAttackable(x) && x.Distance <= 700)
                                                         .OrderBy(x => x.Health)
                                                         .FirstOrDefault();
                }
            };
        }

        internal static float GetEDamage(GameObjectBase enemy)
        {
            var magicResistMod = Helpers.DamageCalculator.GetMagicResistMod(UnitManager.MyChampion, enemy);

            var magicDamage = 48 + 4 * UnitManager.MyChampion.Level +
                              UnitManager.MyChampion.UnitStats.TotalAbilityPower * 0.1f;

            var cassEBuff = enemy.BuffManager.GetBuffByName("findout", false, true);
            if (cassEBuff != null && cassEBuff.IsActive)
            {
                //add bonus dmg
            }

            return (float)magicResistMod * magicDamage;
        }

        internal override void OnCoreMainInput()
        {
            if (SpellE.ExecuteCastSpell() || SpellQ.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void OnCoreLaneClearInput()
        {
            if (SpellE.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Cassiopeia)}"));
            MenuTab.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            //MenuTab.AddItem(new Switch() { Title = "Use W", IsOn = true });
            MenuTab.AddItem(new Switch() { Title = "Use E", IsOn = true });
            //MenuTab.AddItem(new Switch() { Title = "Use R", IsOn = true });
        }
    }
}
