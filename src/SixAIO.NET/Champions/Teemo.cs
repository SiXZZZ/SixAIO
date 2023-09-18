using Oasys.Common.Enums.GameEnums;
using Oasys.Common.GameObject;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SixAIO.Extensions;
using SixAIO.Models;
using System;
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
                TargetSelect = (mode)
                            => SpellQ
                                .GetTargets(mode,
                                    x =>
                                        !TargetSelector.IsInvulnerable(x, Oasys.Common.Logic.DamageType.Magical, false) &&
                                        (!OnlyQBasicAttackingTarget || IsCastingSpellOnAlly(x)))
                                .FirstOrDefault()
            };
        }

        private bool IsCastingSpellOnAlly(GameObjectBase hero)
        {
            try
            {
                if (hero.IsAlive && hero.IsCastingSpell)
                {
                    var spell = hero.GetCurrentCastingSpell();
                    if (spell != null)
                    {
                        var target = spell.Targets.FirstOrDefault(x => x.IsAlive && x.IsVisible && x.IsTargetable);
                        if (target != null)
                        {
                            return (target.IsAlive && UnitManager.AllyChampions.Any(x => x.IsAlive && x.NetworkID == target.NetworkID)) ||
                                   (hero.ModelName == "Zeri" && spell.SpellSlot == SpellSlot.Q);
                        }
                    }
                }
            }
            catch (Exception)
            {
            }

            return false;
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

        internal bool OnlyQBasicAttackingTarget => QSettings.GetItem<Switch>("Only Q Basic Attacking Target").IsOn;

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Teemo)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Only Q Basic Attacking Target", IsOn = true });


            MenuTab.AddDrawOptions(SpellSlot.Q);
        }
    }
}