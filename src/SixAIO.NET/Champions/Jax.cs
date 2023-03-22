using Oasys.Common.Enums.GameEnums;
using Oasys.Common.GameObject;
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
    internal sealed class Jax : Champion
    {
        public Jax()
        {
            Spell.OnSpellCast += Spell_OnSpellCast;
            Orbwalker.OnOrbwalkerAfterBasicAttack += Orbwalker_OnOrbwalkerAfterBasicAttack;
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                ShouldDraw = () => DrawQRange,
                DrawColor = () => DrawQColor,
                IsTargetted = () => true,
                Range = () => 700,
                IsEnabled = () => UseQ &&
                            !TargetSelector.IsAttackable(Orbwalker.TargetHero) &&
                            !TargetSelector.IsInRange(Orbwalker.TargetHero),
                TargetSelect = (mode) => SpellQ.GetTargets(mode).FirstOrDefault()
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                IsEnabled = () => UseW,
                ShouldCast = (mode, target, spellClass, damage) =>
                            !Orbwalker.CanBasicAttack &&
                            TargetSelector.IsAttackable(Orbwalker.TargetHero) &&
                            TargetSelector.IsInRange(Orbwalker.TargetHero),
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                ShouldDraw = () => DrawERange,
                DrawColor = () => DrawEColor,
                IsEnabled = () => UseE,
                Range = () => 300,
                ShouldCast = (mode, target, spellClass, damage) => UnitManager.EnemyChampions.Any(x => x.Distance <= 300 && TargetSelector.IsAttackable(x))
            };
        }

        private void Spell_OnSpellCast(SDKSpell spell, GameObjectBase target)
        {
            if (spell.CastSlot == CastSlot.W)
            {
                Orbwalker.AttackReset();
            }
        }

        private void Orbwalker_OnOrbwalkerAfterBasicAttack(float gameTime, GameObjectBase target)
        {
            if (target != null)
            {
                SpellW.ExecuteCastSpell();
            }
        }

        internal override void OnCoreRender()
        {
            SpellQ.DrawRange();
            SpellE.DrawRange();
        }

        internal override void OnCoreMainInput()
        {
            if (SpellQ.ExecuteCastSpell() || SpellE.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Jax)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("E Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });


            MenuTab.AddDrawOptions(SpellSlot.Q, SpellSlot.E);


        }
    }
}
