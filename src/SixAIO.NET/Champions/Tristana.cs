using Oasys.Common.Enums.GameEnums;
using Oasys.Common.GameObject;
using Oasys.Common.GameObject.ObjectClass;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SixAIO.Enums;
using SixAIO.Models;
using System;
using System.Linq;

namespace SixAIO.Champions
{
    internal class Tristana : Champion
    {

        private static float GetRDamage(GameObjectBase target)
        {
            return Helpers.DamageCalculator.GetMagicResistMod(UnitManager.MyChampion, target) *
                   (UnitManager.MyChampion.UnitStats.TotalAbilityPower + 200 + 100 * UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.R).Level);
        }

        public Tristana()
        {
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                CastTime = 1f,
                ShouldCast = (target, spellClass, damage) =>
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 90 &&
                            UseE &&
                            target != null,
                TargetSelect = () => TargetSelector.GetBestChampionTarget(Orbwalker.SelectedHero)
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                CastTime = 1f,
                Damage = (target, spellClass) =>
                            target != null
                            ? GetRDamage(target)
                            : 0,
                ShouldCast = (target, spellClass, damage) =>
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 100 &&
                            UseR &&
                            target != null,
                TargetSelect = () => TargetSelectR()
            };
        }

        private Hero TargetSelectR()
        {
            var targets = UnitManager.EnemyChampions.Where(x => TargetSelector.IsAttackable(x) && x.Distance <= UnitManager.MyChampion.TrueAttackRange).OrderBy(x => x.Health);
            var target = targets.FirstOrDefault(x => DamageCalculator.GetTargetHealthAfterBasicAttack(UnitManager.MyChampion, x) + 100 < GetRDamage(x));
            if (target != null)
            {
                return target;
            }
            if (UsePushAway)
            {
                return PushAwayModeSelected switch
                {
                    PushAwayMode.Melee => targets.FirstOrDefault(x => x.CombatType == CombatTypes.Melee && x.Distance < PushAwayRange),
                    PushAwayMode.LowerThanMyRange => targets.FirstOrDefault(x => x.AttackRange < UnitManager.MyChampion.AttackRange && x.Distance < PushAwayRange),
                    PushAwayMode.DashNearMe => targets.FirstOrDefault(x => x.AIManager.IsDashing && UnitManager.MyChampion.DistanceTo(x.AIManager.NavEndPosition) < 300 && x.Distance < PushAwayRange),
                    PushAwayMode.Everything => targets.FirstOrDefault(x => x.Distance < PushAwayRange),
                    _ => null,
                };
            }

            return null;
        }

        internal override void OnCoreMainInput()
        {
            if (SpellE.ExecuteCastSpell() || SpellR.ExecuteCastSpell())
            {
                return;
            }
        }

        private bool UsePushAway
        {
            get => MenuTab.GetItem<Switch>("Use Push Away").IsOn;
            set => MenuTab.GetItem<Switch>("Use Push Away").IsOn = value;
        }

        private int PushAwayRange
        {
            get => MenuTab.GetItem<Counter>("Push Away Range").Value;
            set => MenuTab.GetItem<Counter>("Push Away Range").Value = value;
        }

        private PushAwayMode PushAwayModeSelected
        {
            get => (PushAwayMode)Enum.Parse(typeof(PushAwayMode), MenuTab.GetItem<ModeDisplay>("Push Away Mode").SelectedModeName);
            set => MenuTab.GetItem<ModeDisplay>("Push Away Mode").SelectedModeName = value.ToString();
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Tristana)}"));
            MenuTab.AddItem(new InfoDisplay() { Title = "---E Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use E", IsOn = true });
            MenuTab.AddItem(new InfoDisplay() { Title = "---R Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use R", IsOn = true });
            MenuTab.AddItem(new Switch() { Title = "Use Push Away", IsOn = false });
            MenuTab.AddItem(new Counter() { Title = "Push Away Range", MinValue = 50, MaxValue = 500, Value = 150, ValueFrequency = 50 });
            MenuTab.AddItem(new ModeDisplay() { Title = "Push Away Mode", ModeNames = PushAwayHelper.ConstructPushAwayModeTable(), SelectedModeName = "Melee" });

        }
    }
}
