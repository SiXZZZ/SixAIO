using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Extensions;
using Oasys.Common.GameObject;
using Oasys.Common.GameObject.Clients.ExtendedInstances.Spells;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using Oasys.SDK.Tools;
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
                Range = 850,
                Speed = 5000,
                Width = 200,
                ShouldCast = (target, spellClass, damage) =>
                            UseQ &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 90 &&
                            target != null,
                TargetSelect = () =>
                {
                    var ccTarget = UnitManager.EnemyChampions.FirstOrDefault(x => x.Distance <= 850 && x.IsAlive && x.BuffManager.GetBuffList().Any(BuffChecker.IsCrowdControlledOrSlowed));
                    if (ccTarget != null)
                    {
                        return ccTarget;
                    }
                    return UnitManager.EnemyChampions.FirstOrDefault(x => x.Distance <= 850 && x.IsAlive);
                }
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                Damage = (target, spellClass) => GetEDamage(target, spellClass),
                ShouldCast = (target, spellClass, damage) =>
                            UseE &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 50 &&
                            target != null,
                TargetSelect = () =>
                {
                    var champTarget = UnitManager.EnemyChampions
                                     .Where(x => x.IsAlive && TargetSelector.IsAttackable(x) && x.Distance <= 680)
                                     .OrderBy(x => x.Health)
                                     .FirstOrDefault();
                    if (champTarget != null)
                    {
                        return champTarget;
                    }

                    return Orbwalker.TargetChampionsOnly
                                    ? null
                                    : UnitManager.Enemies.Where(x => x.IsAlive && x.Distance <= 680 && !x.IsObject(ObjectTypeFlag.BuildingProps) && !x.IsObject(ObjectTypeFlag.AITurretClient) && TargetSelector.IsAttackable(x))
                                                         .OrderBy(x => x.Health)
                                                         .FirstOrDefault();
                }
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                Range = 750,
                Speed = 10000,
                Width = 400,
                CastTime = 0.5f,
                ShouldCast = (target, spellClass, damage) =>
                            UseR &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 100 &&
                            target != null,
                TargetSelect = () =>
                                UnitManager.EnemyChampions
                                .FirstOrDefault(x => x.IsAlive &&
                                                     TargetSelector.IsAttackable(x) &&
                                                     x.Distance <= 750 &&
                                                     x.IsFacing(UnitManager.MyChampion) &&
                                                     !TargetSelector.IsInvulnerable(x, Oasys.Common.Logic.DamageType.Magical, false))
            };
        }

        internal static float GetEDamage(GameObjectBase enemy, SpellClass spellClass)
        {
            if (enemy == null || spellClass == null)
            {
                return 0;
            }

            var magicResistMod = DamageCalculator.GetMagicResistMod(UnitManager.MyChampion, enemy);

            var magicDamage = 48 + 4 * UnitManager.MyChampion.Level +
                              UnitManager.MyChampion.UnitStats.TotalAbilityPower * 0.1f;

            if (IsPoisoned(enemy))
            {
                magicDamage += (20 * spellClass.Level) +
                                UnitManager.MyChampion.UnitStats.TotalAbilityPower * 0.6f;
            }

            return (float)magicResistMod * magicDamage;
        }

        private static bool IsPoisoned(GameObjectBase target)
        {
            return target.BuffManager.GetBuffList().Any(buff => buff != null && buff.IsActive && buff.OwnerObjectIndex == UnitManager.MyChampion.Index && buff.Stacks >= 1 &&
                    (buff.Name.Contains("cassiopeiaqdebuff", System.StringComparison.OrdinalIgnoreCase) || buff.Name.Contains("cassiopeiawpoison", System.StringComparison.OrdinalIgnoreCase)));
        }

        internal override void OnCoreMainInput()
        {
            //var something = (float)(UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.E).FinalCooldownExpire / UnitManager.MyChampion.GetAttackCastDelay());
            //Logger.Log("Someething : " + something + "  cd: " + UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.E).FinalCooldownExpire);
            //if (!Orbwalker.AllowAttacking && /*UnitManager.MyChampion.Mana / UnitManager.MyChampion.MaxMana * 100) < 10 || */  something > 2.4f)
            //{
            //    Orbwalker.AllowAttacking = true;
            //}
            //else
            //{
            //    Orbwalker.AllowAttacking = false;
            //}

            if (SpellE.ExecuteCastSpell() || SpellQ.ExecuteCastSpell() || SpellR.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void OnCoreLaneClearInput()
        {
            //var something = (float)(UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.E).FinalCooldownExpire / UnitManager.MyChampion.GetAttackCastDelay());
            //Logger.Log("Someething : " + something + "  cd: " + UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.E).FinalCooldownExpire);
            //if (!Orbwalker.AllowAttacking && /*UnitManager.MyChampion.Mana / UnitManager.MyChampion.MaxMana * 100) < 10 || */  something > 2.4f)
            //{
            //    Orbwalker.AllowAttacking = true;
            //}
            //else
            //{
            //    Orbwalker.AllowAttacking = false;
            //}

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
            MenuTab.AddItem(new Switch() { Title = "Use R", IsOn = true });
        }
    }
}
