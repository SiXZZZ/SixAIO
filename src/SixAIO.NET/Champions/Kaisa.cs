using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SixAIO.Models;
using System;
using System.Linq;

namespace SixAIO.Champions
{
    internal class Kaisa : Champion
    {
        public Kaisa()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                IsEnabled = () => UseQ,
                ShouldCast = (mode, target, spellClass, damage) =>
                {
                    var champs = UnitManager.EnemyChampions.Count(x => x.Distance <= 600 && TargetSelector.IsAttackable(x));
                    var minions = UnitManager.EnemyMinions.Count(x => x.Distance <= 600 && TargetSelector.IsAttackable(x));
                    var jungleMobs = UnitManager.EnemyJungleMobs.Count(x => x.Distance <= 600 && TargetSelector.IsAttackable(x));
                    return champs > 0 && minions <= 1 && jungleMobs <= 1;
                }
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
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

        internal override void OnCoreMainInput()
        {
            if (SpellQ.ExecuteCastSpell() || SpellW.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Kaisa)}"));
            MenuTab.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            MenuTab.AddItem(new Switch() { Title = "Use W", IsOn = true });
            MenuTab.AddItem(new ModeDisplay() { Title = "W HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });
            MenuTab.AddItem(new Switch() { Title = "Allow W cast on minimap", IsOn = true });

        }
    }
}
