using Oasys.Common.Enums.GameEnums;
using Oasys.Common.GameObject;
using Oasys.Common.GameObject.ObjectClass;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using Oasys.SDK.Tools;
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
                Orbwalker.OrbWalkingModeType.Combo => ShouldEOnCombo(),
                Orbwalker.OrbWalkingModeType.LaneClear =>
                            ShouldEOnCombo() ||
                            (UnitManager.EnemyJungleMobs.Count(x => IsValidTarget(x) && IsEpicJungleMonster(x) && CanKill(x)) >= 1) ||
                            (UnitManager.EnemyJungleMobs.Count(x => IsValidTarget(x) && CanKill(x)) >= 3) ||
                            (UnitManager.EnemyMinions.Count(x => IsValidTarget(x) && CanKill(x)) >= 6),
                _ => false
            };
        }

        private bool ShouldEOnCombo()
        {
            return EWhenCanKill
                ? UnitManager.EnemyChampions.Count(x => IsValidTarget(x) && CanKill(x)) >= 1
                : UnitManager.EnemyChampions.Count(x => IsValidTarget(x) && TwitchEStacks(x) >= MinimumStacks) >= TargetsWithStacks;
        }

        private static bool IsEpicJungleMonster(JungleMob target)
        {
            return target.UnitComponentInfo.SkinName.ToLower().Contains("dragon") ||
                   target.UnitComponentInfo.SkinName.ToLower().Contains("baron") ||
                   target.UnitComponentInfo.SkinName.ToLower().Contains("herald");
        }

        private static bool IsValidTarget(GameObjectBase target)
        {
            return target.Distance <= 1200 && target.IsAlive && TargetSelector.IsAttackable(target);
        }

        private static bool CanKill(GameObjectBase target)
        {
            var dmg = GetEDamage(target);
            return target.Health <= dmg ||
                   (UnitManager.MyChampion.Inventory.HasItem(ItemID.The_Collector) && (target.Health - dmg) / target.MaxHealth * 100f < 5f);
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
            if (!UseELaneclear || SpellE.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.LaneClear))
            {
                return;
            }
        }

        private static float TwitchEStacks(GameObjectBase enemy)
        {
            return enemy.BuffManager.ActiveBuffs.FirstOrDefault(x => x.Name == "TwitchDeadlyVenom")?.Stacks ?? 0;
        }

        internal static float GetEDamage(GameObjectBase enemy)
        {
            var stacks = TwitchEStacks(enemy);
            var eLevel = UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.E).Level;
            var armorMod = DamageCalculator.GetArmorMod(UnitManager.MyChampion, enemy);
            var magicResistMod = DamageCalculator.GetMagicResistMod(UnitManager.MyChampion, enemy);
            var physicalBaseDamage = 10 + eLevel * 10;
            var physicalDamage = armorMod * (physicalBaseDamage +
                                            ((UnitManager.MyChampion.UnitStats.BonusAttackDamage * 0.35f + (10 + eLevel * 5)) * stacks));
            var magicDamage = magicResistMod * ((0.3f * UnitManager.MyChampion.UnitStats.TotalAbilityPower) * stacks);
            //Logger.Log($"Physical: {physicalDamage} - Magic: {magicDamage}");
            return (float)(((physicalDamage - enemy.PhysicalShield) + (magicDamage - enemy.MagicalShield)) - enemy.NeutralShield);
        }

        private bool EWhenCanKill
        {
            get => ESettings.GetItem<Switch>("E when can kill").IsOn;
            set => ESettings.GetItem<Switch>("E when can kill").IsOn = value;
        }

        private int TargetsWithStacks
        {
            get => ESettings.GetItem<Counter>("Targets with stacks").Value;
            set => ESettings.GetItem<Counter>("Targets with stacks").Value = value;
        }

        private int MinimumStacks
        {
            get => ESettings.GetItem<Counter>("Minimum stacks").Value;
            set => ESettings.GetItem<Counter>("Minimum stacks").Value = value;
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Twitch)}"));
            MenuTab.AddGroup(new Group("E Settings"));

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });
            ESettings.AddItem(new Switch() { Title = "Use E Laneclear", IsOn = true });
            ESettings.AddItem(new Counter() { Title = "Targets with stacks", MinValue = 1, MaxValue = 5, Value = 1, ValueFrequency = 1 });
            ESettings.AddItem(new Counter() { Title = "Minimum stacks", MinValue = 1, MaxValue = 6, Value = 6, ValueFrequency = 1 });
            ESettings.AddItem(new Switch() { Title = "E when can kill", IsOn = true });
        }
    }
}
