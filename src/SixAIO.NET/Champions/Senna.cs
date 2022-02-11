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
    internal class Senna : Champion
    {
        public Senna()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                Delay = () => 1f,
                ShouldCast = (target, spellClass, damage) =>
                            UseQ &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 110 &&
                            target != null,
                TargetSelect = (mode) => 
                            UnitManager.EnemyChampions
                            .FirstOrDefault(x => x.Distance <= UnitManager.MyChampion.TrueAttackRange && x.IsAlive &&
                                        TargetSelector.IsAttackable(x) &&
                                        !TargetSelector.IsInvulnerable(x, Oasys.Common.Logic.DamageType.Physical, false))
            };
        }

        private static void TargetSoulsWithOrbwalker()
        {
            var soul = UnitManager.EnemyJungleMobs.FirstOrDefault(x => x.ModelName == "SennaSoul" && TargetSelector.IsInRange(x));
            Orbwalker.SelectedTarget = soul;
        }

        internal override void OnCoreMainInput()
        {
            if (SpellQ.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void OnCoreMainTick()
        {
            TargetSoulsWithOrbwalker();
        }

        internal override void OnCoreLaneClearInput()
        {
            if (SpellQ.ExecuteCastSpell())
            {
                return;
            }
            //var enemies = UnitManager.EnemyMinions.AsEnumerable<GameObjectBase>().ToList();
            //enemies.AddRange(UnitManager.EnemyJungleMobs.AsEnumerable<GameObjectBase>());
            //enemies.AddRange(UnitManager.EnemyChampions.AsEnumerable<GameObjectBase>());
            //enemies.AddRange(UnitManager.EnemyTowers.AsEnumerable<GameObjectBase>());
            //var enemy = enemies.Where(x => TargetSelector.IsAttackable(x) && TargetSelector.ShouldAttackMinion(x))
            //                   .FirstOrDefault(x => x.Distance <= UnitManager.MyChampion.TrueAttackRange + 5 && x.IsAlive);
            //CastQ(enemy, Oasys.Common.Logic.OrbwalkingMode.LaneClear);
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Senna)}"));
            MenuTab.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            //MenuTab.AddItem(new Switch() { Title = "Use W", IsOn = true });
            //MenuTab.AddItem(new Switch() { Title = "Use E", IsOn = true });
            //MenuTab.AddItem(new Switch() { Title = "Use R", IsOn = true });
        }
    }
}
