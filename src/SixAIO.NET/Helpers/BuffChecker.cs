using Oasys.Common.Enums.GameEnums;
using Oasys.Common.GameObject;
using Oasys.Common.GameObject.Clients.ExtendedInstances;
using System.Linq;

namespace SixAIO.Helpers
{
    internal static class BuffChecker
    {
        internal static bool IsCrowdControlled<T>(T obj, bool slowIsCC) where T : GameObjectBase
        {
            return IsCrowdControlledButCanQss(obj, slowIsCC) || IsKnockedUpOrBack(obj) || IsGrounded(obj);
        }

        internal static bool IsCrowdControlled<T>(T obj) where T : GameObjectBase
        {
            return IsCrowdControlledButCanQss(obj) || IsKnockedUpOrBack(obj) || IsGrounded(obj);
        }

        private static bool IsGrounded<T>(T obj) where T : GameObjectBase
        {
            return obj.BuffManager.GetBuffList().Any(buff => buff.IsActive && buff.Stacks >= 1 &&
                        (buff.Name.Equals("CassiopeiaWSlow", System.StringComparison.OrdinalIgnoreCase) ||
                         buff.Name.Equals("megaadhesiveslow", System.StringComparison.OrdinalIgnoreCase)));
        }

        internal static bool IsCrowdControlledButCanQss<T>(T obj, bool slowIsCC) where T : GameObjectBase
        {
            return obj.BuffManager.GetBuffList().Any(x => IsCrowdControllButCanQss(x, slowIsCC));
        }

        internal static bool IsCrowdControlledButCanQss<T>(T obj) where T : GameObjectBase
        {
            return obj.BuffManager.GetBuffList().Any(IsCrowdControllButCanQss);
        }

        internal static bool IsCrowdControllButCanQss(this BuffEntry buff, bool slowIsCC)
        {
            return buff.IsActive && buff.Stacks >= 1 && (buff.IsCrowdControllButCanCleanse(slowIsCC) || buff.EntryType == BuffType.Suppression);
        }

        internal static bool IsCrowdControllButCanQss(this BuffEntry buff)
        {
            return buff.IsActive && buff.Stacks >= 1 && (buff.IsCrowdControllButCanCleanse() || buff.EntryType == BuffType.Suppression);
        }

        internal static bool IsCrowdControlledButCanCleanse<T>(T obj) where T : GameObjectBase
        {
            return obj.BuffManager.GetBuffList().Any(IsCrowdControllButCanCleanse);
            //TODO: ADD poppy tahm kench https://leagueoflegends.fandom.com/wiki/Types_of_Crowd_Control#Ground
        }

        internal static bool IsCrowdControllButCanCleanse(this BuffEntry buff)
        {
            return IsCrowdControllButCanCleanse(buff, true);
        }

        internal static bool IsCrowdControllButCanCleanse(this BuffEntry buff, bool slowIsCC)
        {
            return buff.IsActive && buff.Stacks >= 1 &&
                   ((slowIsCC && buff.EntryType == BuffType.Slow) ||
                   buff.EntryType == BuffType.Stun || buff.EntryType == BuffType.Taunt ||
                   buff.EntryType == BuffType.Snare || buff.EntryType == BuffType.Charm ||
                   buff.EntryType == BuffType.Silence || buff.EntryType == BuffType.Blind ||
                   buff.EntryType == BuffType.Fear || buff.EntryType == BuffType.Polymorph ||
                   buff.EntryType == BuffType.Flee || buff.EntryType == BuffType.Sleep) &&
                   !buff.Name.Equals("yonerstun", System.StringComparison.OrdinalIgnoreCase) &&
                   !buff.Name.Equals("landslidedebuff", System.StringComparison.OrdinalIgnoreCase) &&
                   !buff.Name.Equals("CassiopeiaWSlow", System.StringComparison.OrdinalIgnoreCase) &&
                   !buff.Name.Equals("megaadhesiveslow", System.StringComparison.OrdinalIgnoreCase);
        }

        internal static bool IsCrowdControlledOrSlowed<T>(T obj) where T : GameObjectBase
        {
            return IsCrowdControlled(obj) || obj.BuffManager.GetBuffList().Any(buff => buff.IsActive && buff.Stacks >= 1 && buff.EntryType == BuffType.Slow);
        }

        internal static bool IsKnockedUpOrBack<T>(T obj) where T : GameObjectBase
        {
            return obj.BuffManager.GetBuffList().Any(buff => buff.IsActive && buff.Stacks >= 1 && (buff.EntryType == BuffType.Knockup || buff.EntryType == BuffType.Knockback));
        }
    }
}
