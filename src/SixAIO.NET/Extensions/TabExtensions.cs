using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Menu;
using SixAIO.Helpers;

namespace SixAIO.Extensions
{
    internal static class TabExtensions
    {
        public static void AddDrawOptions(this Tab tab, params SpellSlot[] spellSlots)
        {
            OptionsBuilder.BuildDrawOptions(tab, spellSlots);
        }
    }
}
