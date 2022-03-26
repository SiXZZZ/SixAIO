using Oasys.Common.Enums.GameEnums;
using Oasys.Common.GameObject;
using Oasys.Common.GameObject.ObjectClass;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using Oasys.SDK.Tools;
using SixAIO.Enums;
using SixAIO.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SixAIO.Champions
{
    internal class Veigar : Champion
    {
        public Veigar()
        {
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                IsTargetted = () => true,
                IsEnabled = () => UseR,
                TargetSelect = (mode) => UnitManager.EnemyChampions.Where(x => x.Distance <= 650 && 
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
            var missingHealthPercent = 100f - (target.Health / target.MaxHealth * 100f);
            var extraDamagePercent = missingHealthPercent * 1.5f;
            if (extraDamagePercent > 100f)
            {
                extraDamagePercent = 100f;
            }
            return (1 + (extraDamagePercent / 100f)) * DamageCalculator.GetMagicResistMod(UnitManager.MyChampion, target) *
                   ((UnitManager.MyChampion.UnitStats.TotalAbilityPower * 0.75f) + 100 + 75 * UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.R).Level);
        }

        internal override void OnCoreMainInput()
        {
            if (SpellR.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Veigar)}"));
            //MenuTab.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            //MenuTab.AddItem(new Switch() { Title = "Use W", IsOn = true });
            //MenuTab.AddItem(new Switch() { Title = "Use E", IsOn = true });
            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
        }
    }
}
