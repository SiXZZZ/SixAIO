using Oasys.Common.Enums.GameEnums;
using Oasys.Common.GameObject.Clients.ExtendedInstances;

namespace SixAIO.Helpers
{
    internal class BuffChecker
    {
        internal static bool IsCrowdControlled(BuffEntry buff)
        {
            return buff.IsActive && buff.EntryType != BuffType.Slow &&
                   (buff.EntryType == BuffType.Knockup || buff.EntryType == BuffType.Suppression ||
                   IsCrowdControlledButCanCleanse(buff));
        }

        internal static bool IsCrowdControlledButCanCleanse(BuffEntry buff)
        {
            return buff.IsActive &&
                   (buff.EntryType == BuffType.Stun || buff.EntryType == BuffType.Taunt ||
                   buff.EntryType == BuffType.Snare || buff.EntryType == BuffType.Charm ||
                   buff.EntryType == BuffType.Silence || buff.EntryType == BuffType.Blind ||
                   buff.EntryType == BuffType.Fear || buff.EntryType == BuffType.Polymorph ||
                   buff.EntryType == BuffType.Flee || buff.EntryType == BuffType.Sleep) &&
                   !buff.Name.Equals("CassiopeiaWSlow", System.StringComparison.OrdinalIgnoreCase);
            //TODO: ADD poppy singed tahm kench https://leagueoflegends.fandom.com/wiki/Types_of_Crowd_Control#Ground
        }

        internal static bool IsCrowdControlledOrSlowed(BuffEntry buff)
        {
            return buff.IsActive &&
                   (buff.EntryType == BuffType.Slow || IsCrowdControlled(buff));
        }
    }
}
