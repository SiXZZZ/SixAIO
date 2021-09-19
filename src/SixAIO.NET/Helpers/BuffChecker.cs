using Oasys.Common.Enums.GameEnums;
using Oasys.Common.GameObject.Clients.ExtendedInstances;

namespace SixAIO.Helpers
{
    internal class BuffChecker
    {
        internal static bool IsCrowdControlled(BuffEntry buff)
        {
            return buff.IsActive &&
                   (buff.EntryType == BuffType.Knockup || buff.EntryType == BuffType.Stun ||
                   buff.EntryType == BuffType.Snare || buff.EntryType == BuffType.Sleep ||
                   buff.EntryType == BuffType.Suppression || buff.EntryType == BuffType.Taunt);
        }
    }
}
