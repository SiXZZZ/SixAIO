using Oasys.Common.Enums.GameEnums;
using Oasys.Common.GameObject;
using Oasys.Common.GameObject.Clients.ExtendedInstances.Spells;
using Oasys.Common.GameObject.ObjectClass;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SixAIO.Models;
using System.Linq;

namespace SixAIO.Champions
{
    internal class Twitch : Champion
    {
        public Twitch()
        {
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                IsEnabled = () => UseE,
                ShouldCast = (mode, target, spellClass, damage) => ShouldCastE(mode),
            };
        }

        internal bool ShouldCastE(Orbwalker.OrbWalkingModeType mode)
        {
            return mode switch
            {
                Orbwalker.OrbWalkingModeType.Combo => UnitManager.EnemyChampions.Count(IsValidHero) >= 1,
                Orbwalker.OrbWalkingModeType.LaneClear =>
                            (UnitManager.EnemyChampions.Count(IsValidHero) >= 1) ||
                            (UnitManager.EnemyJungleMobs.Count(x => IsValidTarget(x) && IsEpicJungleMonster(x)) >= 1) ||
                            (UnitManager.EnemyJungleMobs.Count(IsValidTarget) >= 3) ||
                            (UnitManager.EnemyMinions.Count(IsValidTarget) >= 6),
                _ => false
            };
        }

        private bool IsValidHero(Hero target)
        {
            return IsValidTarget(target) &&
                    !TargetSelector.IsInvulnerable(target, Oasys.Common.Logic.DamageType.Physical, false) &&
                    MenuTab.GetItem<Switch>("E - " + target.ModelName).IsOn;
        }

        private static bool IsEpicJungleMonster(JungleMob target)
        {
            return target.UnitComponentInfo.SkinName.ToLower().Contains("dragon") ||
                   target.UnitComponentInfo.SkinName.ToLower().Contains("baron") ||
                   target.UnitComponentInfo.SkinName.ToLower().Contains("herald");
        }

        private static bool IsValidTarget(GameObjectBase target)
        {
            return target.Distance <= 1200 && target.IsAlive && TargetSelector.IsAttackable(target) && target.Health < GetEDamage(target);
        }


        internal override void OnCoreMainInput()
        {
            if (SpellE.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void OnCoreLaneClearInput()
        {
            if (SpellE.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.LaneClear))
            {
                return;
            }
        }

        internal static float GetEDamage(GameObjectBase enemy)
        {
            var twitchEBuff = enemy.BuffManager.GetBuffByName("TwitchDeadlyVenom");
            if (twitchEBuff == null || !twitchEBuff.IsActive)
            {
                return 0;
            }
            var armorMod = DamageCalculator.GetArmorMod(UnitManager.MyChampion, enemy);
            var magicResistMod = DamageCalculator.GetMagicResistMod(UnitManager.MyChampion, enemy);
            var physicalDamage = armorMod * ((10 + UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.E).Level * 10) +
                                            ((UnitManager.MyChampion.UnitStats.BonusAttackDamage * 0.35 + (10 + UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.E).Level * 5)) *
                                            twitchEBuff.Stacks));
            var magicDamage = magicResistMod * ((0.333 * UnitManager.MyChampion.UnitStats.TotalAbilityPower) * twitchEBuff.Stacks);
            return (float)(((physicalDamage - enemy.PhysicalShield) + (magicDamage - enemy.MagicalShield)) - enemy.NeutralShield);
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Twitch)}"));
            //MenuTab.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            //MenuTab.AddItem(new Switch() { Title = "Use W", IsOn = true });
            MenuTab.AddItem(new InfoDisplay() { Title = "---E Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use E", IsOn = true });
            foreach (var enemy in UnitManager.EnemyChampions.Where(x => !x.IsTargetDummy))
            {
                MenuTab.AddItem(new Switch() { Title = "E - " + enemy.ModelName, IsOn = true });
            }
            //MenuTab.AddItem(new Switch() { Title = "Use R", IsOn = true });
        }
    }
}
