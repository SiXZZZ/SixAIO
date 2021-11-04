using Oasys.Common.Enums.GameEnums;
using Oasys.Common.GameObject;
using Oasys.Common.GameObject.Clients.ExtendedInstances.Spells;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SixAIO.Models;
using System.Linq;

namespace SixAIO.Champions
{
    internal class Kalista : Champion
    {
        private enum Mode
        {
            Champs,
            Jungle,
            Everything,
        }

        private static Mode _mode = Mode.Everything;

        public Kalista()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                Damage = (target, spellClass) => -45 + (65 * spellClass.Level) + UnitManager.MyChampion.UnitStats.TotalAttackDamage,
                ShouldCast = (target, spellClass, damage) => UseQ && spellClass.IsSpellReady && UnitManager.MyChampion.Mana > 70 && target != null,
                TargetSelect = () => UnitManager.EnemyChampions
                                    .Where(x => TargetSelector.IsAttackable(x) && x.Distance <= 1100 && x.IsAlive)
                                    .OrderBy(x => x.Health)
                                    .FirstOrDefault()
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                ShouldCast = (target, spellClass, damage) => ShouldCastE(spellClass),
            };
        }

        internal bool ShouldCastE(SpellClass spellClass)
        {
            if (UseE && spellClass.IsSpellReady && UnitManager.MyChampion.Mana > 40)
            {
                switch (_mode)
                {
                    case Mode.Champs:
                        {
                            return UnitManager.EnemyChampions
                                .Count(x => x.Distance <= UnitManager.MyChampion.TrueAttackRange && x.IsAlive &&
                                            TargetSelector.IsAttackable(x) && x.Health < GetEDamage(x) &&
                                                                !TargetSelector.IsInvulnerable(x, Oasys.Common.Logic.DamageType.Physical, false) &&
                                                                MenuTab.GetItem<Switch>("E - " + x.ModelName).IsOn)
                                 >= 1;
                        }
                    case Mode.Jungle:
                        {
                            return (UnitManager.EnemyJungleMobs.Count(x => x.Distance <= UnitManager.MyChampion.TrueAttackRange && x.IsAlive &&
                                           TargetSelector.IsAttackable(x) && x.Health < GetEDamage(x) &&
                                           (x.UnitComponentInfo.SkinName.ToLower().Contains("dragon") ||
                                           x.UnitComponentInfo.SkinName.ToLower().Contains("baron") ||
                                           x.UnitComponentInfo.SkinName.ToLower().Contains("herald")))
                                >= 1) ||
                              (UnitManager.EnemyJungleMobs.Count(x => x.Distance <= UnitManager.MyChampion.TrueAttackRange && x.IsAlive &&
                                                                          TargetSelector.IsAttackable(x) && x.Health < GetEDamage(x))
                                 >= 3);
                        }
                    case Mode.Everything:
                        {
                            return (UnitManager.EnemyChampions.Count(x => x.Distance <= UnitManager.MyChampion.TrueAttackRange && x.IsAlive &&
                                                                          TargetSelector.IsAttackable(x) && x.Health < GetEDamage(x) &&
                                                                          !TargetSelector.IsInvulnerable(x, Oasys.Common.Logic.DamageType.Physical, false) &&
                                                                          MenuTab.GetItem<Switch>("E - " + x.ModelName).IsOn) >= 1) ||
                                (UnitManager.EnemyJungleMobs.Count(x => x.Distance <= UnitManager.MyChampion.TrueAttackRange && x.IsAlive &&
                                                                          TargetSelector.IsAttackable(x) && x.Health < GetEDamage(x) &&
                                                                          (x.UnitComponentInfo.SkinName.ToLower().Contains("dragon") ||
                                                                          x.UnitComponentInfo.SkinName.ToLower().Contains("baron") ||
                                                                          x.UnitComponentInfo.SkinName.ToLower().Contains("herald")))
                                >= 1) ||
                                (UnitManager.EnemyJungleMobs.Count(x => x.Distance <= UnitManager.MyChampion.TrueAttackRange && x.IsAlive &&
                                                                          TargetSelector.IsAttackable(x) && x.Health < GetEDamage(x))
                                 >= 3) ||
                                (UnitManager.EnemyMinions.Count(x => x.Distance <= UnitManager.MyChampion.TrueAttackRange && x.IsAlive &&
                                                                          TargetSelector.IsAttackable(x) && x.Health < GetEDamage(x))
                                >= 3);
                        }
                }
            }

            return false;
        }

        internal static float GetEDamage(GameObjectBase enemy)
        {
            var kalistaE = enemy.BuffManager.GetBuffByName("kalistaexpungemarker");
            if (kalistaE == null || !kalistaE.IsActive)
            {
                return 0;
            }
            var armorMod = DamageCalculator.GetArmorMod(UnitManager.MyChampion, enemy);
            var firstSpearDaamage = 10 + (UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.E).Level * 10) + UnitManager.MyChampion.UnitStats.TotalAttackDamage * 0.6;

            var additionalSpearDamage = 4 + UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.E).Level * 6 + UnitManager.MyChampion.UnitStats.TotalAttackDamage * GetAdditionalSpearLevelAttackDamageMod();
            var physicalDamage = firstSpearDaamage + additionalSpearDamage * (kalistaE.Stacks - 1);
            var skin = enemy.UnitComponentInfo.SkinName.ToLower();
            if (skin.Contains("dragon") ||
                skin.Contains("baron") ||
                skin.Contains("herald"))
            {
                physicalDamage *= 0.5;
            }
            physicalDamage *= armorMod;
            return (float)(physicalDamage - enemy.NeutralShield - enemy.PhysicalShield);
        }

        internal static float GetAdditionalSpearLevelAttackDamageMod()
        {
            switch (UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.E).Level)
            {
                case 1:
                    return 0.198f;
                case 2:
                    return 0.23748f;
                case 3:
                    return 0.27498f;
                case 4:
                    return 0.31248f;
                case 5:
                    return 0.34988f;
                default:
                    return 0;
            }
        }

        internal override void OnCoreMainInput()
        {
            _mode = Mode.Champs;
            if (SpellE.ExecuteCastSpell() || SpellQ.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void OnCoreLaneClearInput()
        {
            _mode = Mode.Everything;
            if (SpellE.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Kalista)}"));
            MenuTab.AddItem(new InfoDisplay() { Title = "---Q Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            MenuTab.AddItem(new InfoDisplay() { Title = "---E Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use E", IsOn = true });
            foreach (var enemy in UnitManager.EnemyChampions)
            {
                MenuTab.AddItem(new Switch() { Title = "E - " + enemy.ModelName, IsOn = true });
            }

        }
    }
}
