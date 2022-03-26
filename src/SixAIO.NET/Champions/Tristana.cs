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
using System.Collections.Generic;
using System.Linq;

namespace SixAIO.Champions
{
    internal class Tristana : Champion
    {
        private static float GetRDamage(GameObjectBase target)
        {
            return DamageCalculator.GetMagicResistMod(UnitManager.MyChampion, target) *
                   (UnitManager.MyChampion.UnitStats.TotalAbilityPower + 200 + 100 * UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.R).Level);
        }

        public Tristana()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                IsEnabled = () => UseQ,
                TargetSelect = (mode) => GetETarget(UnitManager.EnemyChampions)
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                IsTargetted = () => true,
                IsEnabled = () => UseE,
                TargetSelect = (mode) =>
                {
                    var bestTarget = TargetSelector.GetBestChampionTarget(Orbwalker.SelectedHero);
                    if (bestTarget != null && ESettings.GetItem<Switch>("E - " + bestTarget.ModelName).IsOn)
                    {
                        return bestTarget;
                    }

                    var targets = UnitManager.EnemyChampions.Where(x => x.Distance <= UnitManager.MyChampion.TrueAttackRange &&
                                                                     TargetSelector.IsAttackable(x) &&
                                                                     !TargetSelector.IsInvulnerable(x, Oasys.Common.Logic.DamageType.Physical, false) &&
                                                                     ESettings.GetItem<Switch>("E - " + x.ModelName).IsOn)
                                                         .OrderBy(x => x.Health);
                    return targets.FirstOrDefault();
                }
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                IsTargetted = () => true,
                Damage = (target, spellClass) =>
                            target != null
                            ? GetRDamage(target)
                            : 0,
                IsEnabled = () => UseR,
                TargetSelect = (mode) => TargetSelectR()
            };
        }

        private Hero TargetSelectR()
        {
            var targets = UnitManager.EnemyChampions.Where(x => x.Distance <= UnitManager.MyChampion.TrueAttackRange &&
                                                                TargetSelector.IsAttackable(x) &&
                                                                !TargetSelector.IsInvulnerable(x, Oasys.Common.Logic.DamageType.Magical, false) &&
                                                                RSettings.GetItem<Switch>("R - " + x.ModelName).IsOn)
                                                    .OrderBy(x => x.Health);

            var target = targets.FirstOrDefault(x => DamageCalculator.GetTargetHealthAfterBasicAttack(UnitManager.MyChampion, x) + x.NeutralShield + x.MagicalShield + 100 < GetRDamage(x));
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
            Orbwalker.SelectedTarget = GetETarget(UnitManager.EnemyChampions);
            if (SpellQ.ExecuteCastSpell() || SpellE.ExecuteCastSpell() || SpellR.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void OnCoreLaneClearInput()
        {
            Orbwalker.SelectedTarget = GetETarget(UnitManager.Enemies);
            SpellQ.ExecuteCastSpell();
        }

        private static GameObjectBase GetETarget<T>(List<T> enemies) where T : GameObjectBase
        {
            return enemies.FirstOrDefault(x => TargetSelector.IsAttackable(x) && x.Distance <= UnitManager.MyChampion.TrueAttackRange && !x.IsObject(ObjectTypeFlag.BuildingProps) && x.BuffManager.HasActiveBuff("tristanaechargesound"));
        }

        private bool UsePushAway
        {
            get => RSettings.GetItem<Switch>("Use Push Away").IsOn;
            set => RSettings.GetItem<Switch>("Use Push Away").IsOn = value;
        }

        private int PushAwayRange
        {
            get => RSettings.GetItem<Counter>("Push Away Range").Value;
            set => RSettings.GetItem<Counter>("Push Away Range").Value = value;
        }

        private PushAwayMode PushAwayModeSelected
        {
            get => (PushAwayMode)Enum.Parse(typeof(PushAwayMode), RSettings.GetItem<ModeDisplay>("Push Away Mode").SelectedModeName);
            set => RSettings.GetItem<ModeDisplay>("Push Away Mode").SelectedModeName = value.ToString();
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Tristana)}"));

            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });
            foreach (var enemy in UnitManager.EnemyChampions.Where(x => !x.IsTargetDummy))
            {
                ESettings.AddItem(new Switch() { Title = "E - " + enemy.ModelName, IsOn = true });
            }


            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            foreach (var enemy in UnitManager.EnemyChampions.Where(x => !x.IsTargetDummy))
            {
                RSettings.AddItem(new Switch() { Title = "R - " + enemy.ModelName, IsOn = true });
            }

            RSettings.AddItem(new Switch() { Title = "Use Push Away", IsOn = false });
            RSettings.AddItem(new Counter() { Title = "Push Away Range", MinValue = 50, MaxValue = 500, Value = 150, ValueFrequency = 25 });
            RSettings.AddItem(new ModeDisplay() { Title = "Push Away Mode", ModeNames = PushAwayHelper.ConstructPushAwayModeTable(), SelectedModeName = "Melee" });

        }
    }
}
