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
    internal class Twitch : Champion
    {
        private enum Mode
        {
            Champs,
            Jungle,
            Everything,
        }

        private static Mode _mode = Mode.Everything;

        public Twitch()
        {
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                ShouldCast = (target, spellClass, damage) => ShouldCastE(spellClass),
            };
        }

        private bool ShouldCastE(SpellClass spellClass)
        {
            if (UseE && spellClass.IsSpellReady && UnitManager.MyChampion.Mana > 100)
            {
                switch (_mode)
                {
                    case Mode.Champs:
                        {
                            return UnitManager.EnemyChampions
                                .Where(x => TargetSelector.IsAttackable(x))
                                .Where(x => x.Distance <= 1200 && x.IsAlive)
                                .Where(x => x.Health < GetEDamage(x))
                                .Count() >= 1;
                        }
                    case Mode.Jungle:
                        {
                            return UnitManager.EnemyJungleMobs
                                .Where(x => TargetSelector.IsAttackable(x))
                                .Where(x => x.Distance <= 1200 && x.IsAlive)
                                .Where(x => x.Health < GetEDamage(x))
                                .Count() >= 1;
                        }
                    case Mode.Everything:
                        {
                            return (UnitManager.EnemyChampions.Where(x => TargetSelector.IsAttackable(x))
                                .Where(x => x.Distance <= 1200 && x.IsAlive)
                                .Where(x => x.Health < GetEDamage(x))
                                .Count() >= 1) ||
                                (UnitManager.EnemyJungleMobs.Where(x => TargetSelector.IsAttackable(x))
                                .Where(x => x.Distance <= 1200 && x.IsAlive)
                                .Where(x => x.Health < GetEDamage(x))
                                .Where(x => x.UnitComponentInfo.SkinName.ToLower().Contains("dragon") || x.UnitComponentInfo.SkinName.ToLower().Contains("baron") || x.UnitComponentInfo.SkinName.ToLower().Contains("herald"))
                                .Count() >= 1) ||
                                (UnitManager.EnemyJungleMobs.Where(x => TargetSelector.IsAttackable(x))
                                .Where(x => x.Distance <= 1200 && x.IsAlive)
                                .Where(x => x.Health < GetEDamage(x))
                                .Count() >= 3) ||
                                (UnitManager.EnemyMinions.Where(x => TargetSelector.IsAttackable(x))
                                .Where(x => x.Distance <= 1200 && x.IsAlive)
                                .Where(x => x.Health < GetEDamage(x))
                                .Count() >= 6);
                        }
                }
            }

            return false;
        }

        internal override void OnCoreMainInput()
        {
            _mode = Mode.Champs;
            if (SpellE.ExecuteCastSpell())
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

        internal static float GetEDamage(GameObjectBase enemy)
        {
            var twitchEBuff = enemy.BuffManager.GetBuffByName("TwitchDeadlyVenom");
            if (twitchEBuff == null || !twitchEBuff.IsActive)
            {
                return 0;
            }
            var armorMod = Helpers.DamageCalculator.GetArmorMod(UnitManager.MyChampion, enemy);
            var magicResistMod = Helpers.DamageCalculator.GetMagicResistMod(UnitManager.MyChampion, enemy);
            var physicalDamage = armorMod * ((10 + UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.E).Level * 10) +
                                            ((UnitManager.MyChampion.UnitStats.BonusAttackDamage * 0.35 + (10 + UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.E).Level * 5)) *
                                            twitchEBuff.Stacks));
            var magicDamage = magicResistMod * ((0.333 * UnitManager.MyChampion.UnitStats.TotalAbilityPower) * twitchEBuff.Stacks);
            return (float)(physicalDamage + magicDamage);
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Twitch)}"));
            //MenuTab.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            //MenuTab.AddItem(new Switch() { Title = "Use W", IsOn = true });
            MenuTab.AddItem(new Switch() { Title = "Use E", IsOn = true });
            //MenuTab.AddItem(new Switch() { Title = "Use R", IsOn = true });
        }
    }
}
