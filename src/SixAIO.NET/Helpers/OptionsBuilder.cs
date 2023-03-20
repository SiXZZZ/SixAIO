using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.Common.Tools;
using SharpDX;
using System.Linq;

namespace SixAIO.Helpers
{
    internal class OptionsBuilder
    {
        private static readonly Color[] _colors = new[] { Color.Blue, Color.Orange, Color.Green, Color.White };

        private static Color GetColor(SpellSlot slot)
        {
            return _colors[(int)slot];
        }

        private static string GetColorName(SpellSlot slot)
        {
            return ColorConverter.GetColors().FirstOrDefault(x => ColorConverter.GetColor(x).Equals(_colors[(int)slot])).ToString();
        }

        internal static void BuildDrawOptions(Tab menuTab, params SpellSlot[] spellSlots)
        {
            menuTab.AddGroup(new Group("Draw Settings"));
            var drawSettings = menuTab.GetGroup("Draw Settings");

            foreach (var slot in spellSlots.Distinct())
            {
                drawSettings.AddItem(new Switch() { Title = $"Draw {slot} Range", IsOn = true });
                drawSettings.AddItem(new ModeDisplay() { Title = $"Draw {slot} Color", ModeNames = ColorConverter.GetColors(), SelectedModeName = GetColorName(slot) });
            }
        }
    }
}
