using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SixAIO.Extensions;
using SixAIO.Models;
using System.Linq;

namespace SixAIO.Champions
{
    internal sealed class Teemo : Champion
    {
        public Teemo()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                ShouldDraw = () => DrawQRange,
                DrawColor = () => DrawQColor,
                IsTargetted = () => true,
                Range = () => 680,
                IsEnabled = () => UseQ,
                TargetSelect = (mode) => SpellQ.GetTargets(mode, x => !TargetSelector.IsInvulnerable(x, Oasys.Common.Logic.DamageType.Magical, false)).FirstOrDefault()
            };
        }

        internal override void OnCoreRender()
        {
            SpellQ.DrawRange();
        }

        internal override void OnCoreMainInput()
        {
            if (SpellQ.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void OnCoreLaneClearInput()
        {
            if (SpellQ.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void OnCoreHarassInput()
        {
            if (SpellQ.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Teemo)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });


            MenuTab.AddDrawOptions(SpellSlot.Q);
        }
    }
}