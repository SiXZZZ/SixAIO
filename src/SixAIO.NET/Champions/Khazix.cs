using Oasys.Common.Enums.GameEnums;
using Oasys.Common.GameObject;
using Oasys.Common.GameObject.ObjectClass;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using Oasys.SDK.Tools;
using SixAIO.Models;
using System;
using System.Linq;

namespace SixAIO.Champions
{
    internal class Khazix : Champion
    {
        /*
        [12.36.37 pm - SixAIO]: Q : BlindMonkQOne
        [12.36.37 pm - SixAIO]: W : BlindMonkWOne
        [12.36.37 pm - SixAIO]: E : BlindMonkEOne
        [12.36.37 pm - SixAIO]: R : BlindMonkRKick
         */

        private bool IsFirstCast(string spellName) => !spellName.Contains("two", StringComparison.OrdinalIgnoreCase);

        public Khazix()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                IsTargetted = () => true,
                Range = () => 375,
                IsEnabled = () => UseQ,
                TargetSelect = (mode) => SpellQ.GetTargets(mode).OrderBy(x => x.Health).FirstOrDefault()
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                AllowCollision = (target, collisions) => !collisions.Any(),
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => WHitChance,
                Range = () => WMaximumRange,
                Radius = () => 140,
                Speed = () => 1700,
                IsEnabled = () => UseW && UnitManager.MyChampion.HealthPercent <= WIfHealthPercentBelow,
                TargetSelect = (mode) => SpellW.GetTargets(mode).FirstOrDefault()
            };
        }

        internal override void OnCoreMainInput()
        {
            if (SpellW.ExecuteCastSpell() || SpellQ.ExecuteCastSpell() /*|| SpellE.ExecuteCastSpell()*/)
            {
                return;
            }
        }

        private int WMaximumRange
        {
            get => WSettings.GetItem<Counter>("W Maximum Range").Value;
            set => WSettings.GetItem<Counter>("W Maximum Range").Value = value;
        }

        private int WIfHealthPercentBelow
        {
            get => WSettings.GetItem<Counter>("W If Health Percent Below").Value;
            set => WSettings.GetItem<Counter>("W If Health Percent Below").Value = value;
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Khazix)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });
            WSettings.AddItem(new ModeDisplay() { Title = "W HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "VeryHigh" });
            WSettings.AddItem(new Counter() { Title = "W Maximum Range", MinValue = 0, MaxValue = 1000, Value = 950, ValueFrequency = 50 });
            WSettings.AddItem(new Counter() { Title = "W If Health Percent Below", MinValue = 0, MaxValue = 100, Value = 80, ValueFrequency = 5 });

            //ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });

            //RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
        }
    }
}
