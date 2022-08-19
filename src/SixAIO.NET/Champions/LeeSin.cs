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
    internal class LeeSin : Champion
    {
        /*
        [12.36.37 pm - SixAIO]: Q : BlindMonkQOne
        [12.36.37 pm - SixAIO]: W : BlindMonkWOne
        [12.36.37 pm - SixAIO]: E : BlindMonkEOne
        [12.36.37 pm - SixAIO]: R : BlindMonkRKick
         */

        private bool IsFirstCast(string spellName) => !spellName.Contains("two", StringComparison.OrdinalIgnoreCase);

        public LeeSin()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                AllowCollision = (target, collisions) => !collisions.Any(),
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => QHitChance,
                Range = () => QMaximumRange,
                Radius = () => 120,
                Speed = () => 1800,
                IsEnabled = () => UseQ && IsFirstCast(SpellQ.SpellClass.SpellData.SpellName),
                TargetSelect = (mode) => SpellQ.GetTargets(mode).FirstOrDefault()
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                IsTargetted = () => true,
                IsEnabled = () => UseW && IsFirstCast(SpellW.SpellClass.SpellData.SpellName),
                TargetSelect = (mode) => UnitManager.MyChampion.HealthPercent <= WIfHealthPercentBelow ? UnitManager.MyChampion : null
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                IsEnabled = () => UseE && IsFirstCast(SpellE.SpellClass.SpellData.SpellName),
                ShouldCast = (mode, target, spellClass, damage) => UnitManager.EnemyChampions.Any(x => TargetSelector.IsAttackable(x) && x.Distance <= 450 && x.IsAlive),
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                IsTargetted = () => true,
                Range = () => 375,
                IsEnabled = () => UseR,
                TargetSelect = (mode) => SpellR.GetTargets(mode).FirstOrDefault(x => x.Health + x.NeutralShield + x.PhysicalShield + 50 < GetRDamage(x))
            };
        }

        private float GetRDamage(GameObjectBase target)
        {
            var baseDmg = -50 + SpellR.SpellClass.Level * 225;
            var scaleDmg = 2f * UnitManager.MyChampion.UnitStats.BonusAttackDamage;

            return DamageCalculator.CalculateActualDamage(UnitManager.MyChampion, target, baseDmg + scaleDmg);
        }

        internal override void OnCoreMainInput()
        {
            if (SpellR.ExecuteCastSpell() || SpellE.ExecuteCastSpell() || SpellQ.ExecuteCastSpell() || SpellW.ExecuteCastSpell())
            {
                return;
            }
        }

        private int QMaximumRange
        {
            get => QSettings.GetItem<Counter>("Q Maximum Range").Value;
            set => QSettings.GetItem<Counter>("Q Maximum Range").Value = value;
        }

        private int WIfHealthPercentBelow
        {
            get => WSettings.GetItem<Counter>("W If Health Percent Below").Value;
            set => WSettings.GetItem<Counter>("W If Health Percent Below").Value = value;
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(LeeSin)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "VeryHigh" });
            QSettings.AddItem(new Counter() { Title = "Q Maximum Range", MinValue = 0, MaxValue = 1200, Value = 1150, ValueFrequency = 50 });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });
            WSettings.AddItem(new Counter() { Title = "W If Health Percent Below", MinValue = 0, MaxValue = 100, Value = 50, ValueFrequency = 5 });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
        }
    }
}
