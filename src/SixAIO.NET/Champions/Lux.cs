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
    internal class Lux : Champion
    {
        public Lux()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                Range = 1300,
                Width = 140,
                Speed = 1200,
                ShouldCast = (target, spellClass, damage) =>
                            UseQ &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 50 &&
                            target != null,
                TargetSelect = () =>
                            UnitManager.EnemyChampions
                            .FirstOrDefault(x => x.Distance <= 1200 && x.IsAlive &&
                                                 TargetSelector.IsAttackable(x) && !Collision.MinionCollision(x.W2S, 140) &&
                                                 !TargetSelector.IsInvulnerable(x, Oasys.Common.Logic.DamageType.Magical, false))
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                Range = 1100,
                Speed = 1200,
                Width = 310,
                ShouldCast = (target, spellClass, damage) =>
                            UseE &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 110 &&
                            target != null,
                TargetSelect = () => UnitManager.EnemyChampions.FirstOrDefault(x => x.Distance <= 1000 && x.IsAlive &&
                                                                                    x.BuffManager.GetBuffList().Any(BuffChecker.IsCrowdControlledOrSlowed) &&
                                                                                    !TargetSelector.IsInvulnerable(x, Oasys.Common.Logic.DamageType.Magical, false))
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                Range = 3400,
                Width = 200,
                Speed = 1000,
                CastTime = 1f,
                ShouldCast = (target, spellClass, damage) =>
                            UseR &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 100 &&
                            target != null,
                TargetSelect = () =>
                            UnitManager.EnemyChampions
                            .FirstOrDefault(x => x.Distance <= 3400 && x.IsAlive &&
                                                 TargetSelector.IsAttackable(x) &&
                                                 !TargetSelector.IsInvulnerable(x, Oasys.Common.Logic.DamageType.Magical, false) &&
                                                 x.BuffManager.GetBuffList().Any(BuffChecker.IsCrowdControlledOrSlowed))
            };
        }

        internal override void OnCoreMainInput()
        {
            if (SpellE.ExecuteCastSpell() || SpellQ.ExecuteCastSpell() ||/* SpellW.ExecuteCastSpell() ||*/ SpellR.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Lux)}"));
            MenuTab.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            //MenuTab.AddItem(new Switch() { Title = "Use W", IsOn = true });
            MenuTab.AddItem(new Switch() { Title = "Use E", IsOn = true });
            MenuTab.AddItem(new Switch() { Title = "Use R", IsOn = true });
        }
    }
}
