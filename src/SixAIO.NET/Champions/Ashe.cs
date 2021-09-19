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
    internal class Ashe : Champion
    {
        public Ashe()
        {
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                CastTime = 1f,
                ShouldCast = (target, spellClass, damage) =>
                            UseW &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 70 &&
                            target != null,
                TargetSelect = () =>
                            UnitManager.EnemyChampions
                            .Where(x => x.Distance <= 1150 && x.IsAlive)
                            .Where(x => TargetSelector.IsAttackable(x))
                            .FirstOrDefault()
            };
        }

        internal override void OnCoreMainInput()
        {
            if (SpellW.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Ashe)}"));
            //MenuTab.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            MenuTab.AddItem(new Switch() { Title = "Use W", IsOn = true });
            //MenuTab.AddItem(new Switch() { Title = "Use E", IsOn = true });
            //MenuTab.AddItem(new Switch() { Title = "Use R", IsOn = true });
        }
    }
}
