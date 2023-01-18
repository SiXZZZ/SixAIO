using Oasys.Common.Enums.GameEnums;
using Oasys.Common.GameObject;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SixAIO.Models;
using System;
using System.Linq;

namespace SixAIO.Champions
{
    internal sealed class Darius : Champion
    {
        private bool IsQActive => UnitManager.MyChampion.BuffManager.ActiveBuffs.Any(x => x.IsActive && x.Stacks >= 1 && x.Name.Equals("dariusqcast", StringComparison.OrdinalIgnoreCase));
        
        public Darius()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                IsEnabled = () => UseQ && !IsQActive,
                ShouldCast = (mode, target, spellClass, damage) => UnitManager.EnemyChampions.Any(x => x.Distance <= 460 && TargetSelector.IsAttackable(x)),
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Cone,
                MinimumHitChance = () => EHitChance,
                Range = () => 500,
                Radius = () => 50,
                Speed = () => 2000,
                IsEnabled = () => UseE && !IsQActive,
                TargetSelect = (mode) => SpellE.GetTargets(mode).FirstOrDefault()
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                IsTargetted = () => true,
                IsEnabled = () => UseR && !IsQActive,
                TargetSelect = (mode) => UnitManager.EnemyChampions.Where(x => x.Distance <= 475 && TargetSelector.IsAttackable(x)).FirstOrDefault(RCanKill)
            };
        }

        private static float PassiveStacks(GameObjectBase target)
        {
            var buff = target.BuffManager.GetActiveBuff("DariusHemo");
            return buff != null && buff.IsActive
                ? buff.Stacks
                : 0;
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
            var stacks = PassiveStacks(target);
            var stackMod = Math.Min(1, 0.2f * stacks);
            var baseDamage = 125 * UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.R).Level;
            var scaleDamage = 0.75f * UnitManager.MyChampion.UnitStats.BonusAttackDamage;
            return stackMod * (baseDamage + scaleDamage);
        }

        internal override void OnCoreMainInput()
        {
            if (SpellQ.ExecuteCastSpell() || SpellE.ExecuteCastSpell() || SpellR.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Darius)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });
            ESettings.AddItem(new ModeDisplay() { Title = "E HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
        }
    }
}
