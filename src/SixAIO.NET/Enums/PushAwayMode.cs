using System;
using System.Collections.Generic;
using System.Linq;

namespace SixAIO.Enums
{
    internal enum PushAwayMode
    {
        Melee,
        LowerThanMyRange,
        DashNearMe,
        Everything
    }

    internal static class PushAwayHelper
    {
        internal static List<string> ConstructPushAwayModeTable()
        {
            var keyTable = Enum.GetNames(typeof(PushAwayMode)).ToList();
            return keyTable;
        }
    }

}
