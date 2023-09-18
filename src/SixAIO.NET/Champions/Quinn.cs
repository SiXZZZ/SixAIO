using Oasys.Common.Enums.GameEnums;
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
    internal sealed class Quinn : Champion
    {
        public Quinn()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                ShouldDraw = () => DrawQRange,
                DrawColor = () => DrawQColor,
                AllowCollision = (target, collisions) => !collisions.Any(),
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => QHitChance,
                Range = () => 1050,
                Radius = () => 120,
                Speed = () => 1550,
                IsEnabled = () => UseQ,
                TargetSelect = (mode) => SpellQ.GetTargets(mode).FirstOrDefault()
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                ShouldDraw = () => DrawERange,
                DrawColor = () => DrawEColor,
                IsTargetted = () => true,
                Range = () => ERange + 150,
                IsEnabled = () => UseE,
                TargetSelect = (mode) => SpellE.GetTargets(mode, x => x.Distance <= ERange + x.BoundingRadius).FirstOrDefault()
            };
        }

        internal override void OnCoreRender()
        {
            SpellQ.DrawRange();
            SpellE.DrawRange();
        }

        internal override void OnCoreMainInput()
        {
            if (SpellE.ExecuteCastSpell() || SpellQ.ExecuteCastSpell())
            {
                return;
            }
        }

        private int ERange => ESettings.GetItem<Counter>("Range").Value;

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Quinn)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("E Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });
            ESettings.AddItem(new Counter() { Title = "Range", MinValue = 50, MaxValue = 600, Value = 600, ValueFrequency = 25 });


            MenuTab.AddDrawOptions(SpellSlot.Q, SpellSlot.E);

        }
    }
}
