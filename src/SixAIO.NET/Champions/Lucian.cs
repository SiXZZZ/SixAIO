using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SixAIO.Models;
using System.Linq;

namespace SixAIO.Champions
{
    internal class Lucian : Champion
    {
        public Lucian()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                ShouldCast = (target, spellClass, damage) =>
                            UseQ &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 90 &&
                            target != null,
                TargetSelect = () =>
                            UnitManager.EnemyChampions
                            .FirstOrDefault(x => x.Distance <= 500 && x.IsAlive && 
                                                 TargetSelector.IsAttackable(x) &&
                                                 !TargetSelector.IsInvulnerable(x, Oasys.Common.Logic.DamageType.Physical, false))
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                ShouldCast = (target, spellClass, damage) =>
                            UseW &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 60 &&
                            target != null,
                TargetSelect = () =>
                            UnitManager.EnemyChampions
                            .FirstOrDefault(x => x.Distance <= 900 && x.IsAlive && TargetSelector.IsAttackable(x))
            };
        }

        internal override void OnCoreMainInput()
        {
            if (SpellQ.ExecuteCastSpell() || SpellW.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void OnCoreLaneClearInput()
        {
            if (SpellQ.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void OnCoreHarassInput()
        {
            if (SpellQ.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Lucian)}"));
            MenuTab.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            MenuTab.AddItem(new Switch() { Title = "Use W", IsOn = true });
            //MenuTab.AddItem(new Switch() { Title = "Use E", IsOn = true });
            //MenuTab.AddItem(new Switch() { Title = "Use R", IsOn = true });
        }
    }
}
