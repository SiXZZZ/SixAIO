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
    internal sealed class Kaisa : Champion
    {
        public Kaisa()
        {
            Orbwalker.OnOrbwalkerAfterBasicAttack += Orbwalker_OnOrbwalkerAfterBasicAttack;
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                IsEnabled = () => UseQ,
                ShouldCast = (mode, target, spellClass, damage) =>
                {
                    var champs = UnitManager.EnemyChampions.Count(x => x.Distance <= 600 && TargetSelector.IsAttackable(x));
                    var minions = UnitManager.EnemyMinions.Count(x => x.Distance <= 600 && TargetSelector.IsAttackable(x));
                    var jungleMobs = UnitManager.EnemyJungleMobs.Count(x => x.Distance <= 600 && TargetSelector.IsAttackable(x));
                    return champs > 0 && minions < 1 && jungleMobs < 1;
                }
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                ShouldDraw = () => DrawWRange,
                DrawColor = () => DrawWColor,
                AllowCastOnMap = () => AllowWCastOnMinimap,
                AllowCollision = (target, collisions) => !collisions.Any(),
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => WHitChance,
                Range = () => 3000,
                Radius = () => 200,
                Speed = () => 1750,
                Delay = () => 0.4f,
                IsEnabled = () => UseW,
                TargetSelect = (mode) => SpellW.GetTargets(mode).FirstOrDefault()
            };
        }

        private void Orbwalker_OnOrbwalkerAfterBasicAttack(float gameTime, GameObjectBase target)
        {
            SpellQ.ExecuteCastSpell();
        }

        internal override void OnCoreRender()
        {
            SpellW.DrawRange();
        }

        internal override void OnCoreMainInput()
        {
            if ((!OnlyQAfterAA && SpellQ.ExecuteCastSpell()) || SpellW.ExecuteCastSpell())
            {
                return;
            }
        }

        private bool OnlyQAfterAA
        {
            get => QSettings.GetItem<Switch>("Only Q After AA").IsOn;
            set => QSettings.GetItem<Switch>("Only Q After AA").IsOn = value;
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Kaisa)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Only Q After AA", IsOn = false });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });
            WSettings.AddItem(new ModeDisplay() { Title = "W HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });
            WSettings.AddItem(new Switch() { Title = "Allow W cast on minimap", IsOn = true });


            MenuTab.AddDrawOptions(SpellSlot.W);

        }
    }
}
