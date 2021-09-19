using Oasys.Common.GameObject;

namespace SixAIO.Helpers
{
    internal class DamageCalculator
    {
        internal static float GetCombatMagicResist(GameObjectBase attacker, GameObjectBase target)
        {
            var magicResist = target.UnitStats.MagicResist * attacker.UnitStats.MagicPercentPenetration;
            magicResist -= attacker.UnitStats.FlatMagicPenetration;
            return magicResist;
        }

        internal static float GetMagicResistMod(GameObjectBase attacker, GameObjectBase target)
        {
            var magicResist = GetCombatMagicResist(attacker, target);
            var damageMod = magicResist > 0
                                ? 100 / (100 + magicResist)
                                : 2 - 100 / (100 + magicResist);
            return damageMod;
        }

        internal static float GetCombatArmor(GameObjectBase attacker, GameObjectBase target)
        {
            var armor = target?.Armor ?? 1 * attacker.UnitStats.PercentBonusArmorPenetration;
            armor -= attacker.UnitStats.PhysicalLethality;
            return armor;
        }

        internal static float GetArmorMod(GameObjectBase attacker, GameObjectBase target)
        {
            var armor = GetCombatArmor(attacker, target);
            var damageMod = armor > 0
                                ? 100 / (100 + armor)
                                : 2 - 100 / (100 + armor);
            return damageMod;
        }
    }
}
