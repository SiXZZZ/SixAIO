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
    internal class Syndra : Champion
    {
        public Syndra()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                CastTime = 0f,
                Range = 800,
                Speed = 5000,
                Width = 180,
                ShouldCast = (target, spellClass, damage) =>
                            UseQ &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 80 &&
                            target != null,
                TargetSelect = () =>
                {
                    var ccTarget = UnitManager.EnemyChampions.FirstOrDefault(x => x.Distance <= 800 && x.IsAlive && x.BuffManager.GetBuffList().Any(BuffChecker.IsCrowdControlled));
                    if (ccTarget != null)
                    {
                        return ccTarget;
                    }
                    return UnitManager.EnemyChampions.FirstOrDefault(x => x.Distance <= 800 && x.IsAlive);
                }
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                Range = 800,
                Width = 140,
                Speed = 2500,
                ShouldCast = (target, spellClass, damage) =>
                            UseE &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 50 &&
                            UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.R).Charges > 3 &&
                            target != null,
                TargetSelect = () =>
                {
                    var targets = UnitManager.EnemyChampions
                                                .Where(x => x.IsAlive && x.Distance <= 1000 &&
                                                            TargetSelector.IsAttackable(x) &&
                                                            !TargetSelector.IsInvulnerable(x, Oasys.Common.Logic.DamageType.Magical, false));

                    return targets.FirstOrDefault();
                }
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                CastTime = 0f,
                ShouldCast = (target, spellClass, damage) =>
                            UseR &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 100 &&
                            target != null,
                TargetSelect = () => UnitManager.EnemyChampions.Where(x => x.Distance <= (UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.R).Level >= 3 ? 750 : 675) &&
                                            TargetSelector.IsAttackable(x) &&
                                            !TargetSelector.IsInvulnerable(x, Oasys.Common.Logic.DamageType.Magical, false))
                                            .FirstOrDefault(RCanKill)
            };
        }

        private static bool RCanKill(GameObjectBase target)
        {
            return GetRDamage(target) > target.Health;
        }

        private static float GetRDamage(GameObjectBase target)
        {
            if (target == null)
            {
                return 0;
            }

            var charges = UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.R).Charges;
            charges = charges > 7 ? 7 : charges;
            var chargeDamage = 40 + (50 * UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.R).Level) + (UnitManager.MyChampion.UnitStats.TotalAbilityPower * 0.2f);
            return DamageCalculator.GetMagicResistMod(UnitManager.MyChampion, target) * (charges * chargeDamage);
        }

        internal override void OnCoreMainInput()
        {
            if (SpellR.ExecuteCastSpell() || SpellE.ExecuteCastSpell() || /*SpellW.ExecuteCastSpell() ||*/ SpellQ.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Syndra)}"));
            MenuTab.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            MenuTab.AddItem(new Switch() { Title = "Use W", IsOn = true });
            MenuTab.AddItem(new Switch() { Title = "Use E", IsOn = true });
            MenuTab.AddItem(new Switch() { Title = "Use R", IsOn = true });
        }
    }
}
