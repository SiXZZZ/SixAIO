using Oasys.Common.Enums.GameEnums;
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
    internal class Kaisa : Champion
    {
        public Kaisa()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                ShouldCast = (target, spellClass, damage) =>
                {
                    if (UseQ && spellClass.IsSpellReady && UnitManager.MyChampion.Mana > 55)
                    {
                        var champs = UnitManager.EnemyChampions.Count(x => x.Distance <= 600 && TargetSelector.IsAttackable(x));
                        var minions = UnitManager.EnemyMinions.Count(x => x.Distance <= 600 && TargetSelector.IsAttackable(x));
                        var jungleMobs = UnitManager.EnemyJungleMobs.Count(x => x.Distance <= 600 && TargetSelector.IsAttackable(x));
                        return champs > 0 && minions <= 1 && jungleMobs <= 1;
                    }

                    return false;
                }
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                Range = () => 3000,
                Radius = () => 200,
                Speed = () => 1750,
                Delay = () => 0.4f,
                ShouldCast = (target, spellClass, damage) =>
                            UseW &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 90 &&
                            target != null,
                TargetSelect = (mode) =>
                            UnitManager.EnemyChampions
                            .FirstOrDefault(x => x.Distance <= 2800 && x.IsAlive &&
                                                TargetSelector.IsAttackable(x) &&
                                                BuffChecker.IsCrowdControlledOrSlowed(x) &&
                                                !Collision.MinionCollision(x.W2S, 220))
            };
        }

        internal override void OnCoreMainInput()
        {
            if (SpellQ.ExecuteCastSpell() || SpellW.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Kaisa)}"));
            MenuTab.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            MenuTab.AddItem(new Switch() { Title = "Use W", IsOn = true });
        }
    }
}
