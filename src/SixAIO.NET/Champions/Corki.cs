using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Extensions;
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
    internal class Corki : Champion
    {
        public Corki()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                Range = () => 825,
                Speed = () => 1000,
                Width = () => 250,
                ShouldCast = (target, spellClass, damage) =>
                            UseQ &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 100 &&
                            target != null,
                TargetSelect = (mode) => 
                {
                    var ccTarget = UnitManager.EnemyChampions.FirstOrDefault(x => x.Distance <= 825 && x.IsAlive && x.BuffManager.GetBuffList().Any(BuffChecker.IsCrowdControlledOrSlowed));
                    if (ccTarget != null)
                    {
                        return ccTarget;
                    }
                    return UnitManager.EnemyChampions.FirstOrDefault(x => x.Distance <= 825 && x.IsAlive);
                }
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                ShouldCast = (target, spellClass, damage) =>
                            UseE &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 50 &&
                            UnitManager.EnemyChampions.FirstOrDefault(x => x.Distance <= UnitManager.MyChampion.TrueAttackRange && x.IsAlive && TargetSelector.IsAttackable(x)) != null,
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                Range = () => 1300,
                Speed = () => 2000,
                Width = () => 80,
                CastTime = () => 0.2f,
                ShouldCast = (target, spellClass, damage) =>
                            UseR &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 20 &&
                            spellClass.Charges >= 1 &&
                            target != null,
                TargetSelect = (mode) => UnitManager.EnemyChampions.FirstOrDefault(x => x.Distance <= 1150 && x.IsAlive &&
                                                                                    TargetSelector.IsAttackable(x) &&
                                                                                    !TargetSelector.IsInvulnerable(x, Oasys.Common.Logic.DamageType.Physical, false) &&
                                                                                    x.BuffManager.GetBuffList().Any(BuffChecker.IsCrowdControlledOrSlowed))
            };
        }

        internal override void OnCoreMainInput()
        {
            if (SpellE.ExecuteCastSpell() || SpellQ.ExecuteCastSpell() || SpellR.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Corki)}"));
            MenuTab.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            MenuTab.AddItem(new Switch() { Title = "Use E", IsOn = true });
            MenuTab.AddItem(new Switch() { Title = "Use R", IsOn = true });
        }
    }
}
