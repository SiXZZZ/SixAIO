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
using System.Threading;

namespace SixAIO.Champions
{
    internal sealed class LeeSin : Champion
    {
        /*
        [12.36.37 pm - SixAIO]: Q : BlindMonkQOne
        [12.36.37 pm - SixAIO]: W : BlindMonkWOne
        [12.36.37 pm - SixAIO]: E : BlindMonkEOne
        [12.36.37 pm - SixAIO]: R : BlindMonkRKick
         */
        internal Spell SpellQ2;

        private bool IsFirstCast(string spellName) => !spellName.Contains("two", StringComparison.OrdinalIgnoreCase);

        public LeeSin()
        {
            SpellQ2 = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                IsEnabled = () => UseQ && !IsFirstCast(SpellQ.SpellClass.SpellData.SpellName),
                ShouldCast = (mode, target, spellClass, damage) => true
            };
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
                TargetSelect = (mode) => SpellR.GetTargets(mode).FirstOrDefault(x => x.Health + x.NeutralShield + x.PhysicalShield + 50 < RDamage(x))
            };
        }

        private float ComboDamage(GameObjectBase target)
        {
            var dmg = 0f;
            if (SpellQ.SpellClass.IsSpellReady)
            {
                dmg += QDamage(target);
                //Logger.Log($"Combo damage Q: {dmg}");
            }
            if (SpellE.SpellClass.IsSpellReady)
            {
                dmg += EDamage(target);
                //Logger.Log($"Combo damage E: {dmg}");
            }
            if (SpellR.SpellClass.IsSpellReady)
            {
                dmg += RDamage(target);
                //Logger.Log($"Combo damage R: {dmg}");
            }
            //Logger.Log($"Combo damage: {dmg}");
            return dmg;
        }

        private float QDamage(GameObjectBase target)
        {
            var baseDmg = 30f + SpellQ.SpellClass.Level * 25f;
            var scaleDmg = UnitManager.MyChampion.UnitStats.BonusAttackDamage;
            var dmg = baseDmg + scaleDmg;
            if (!IsFirstCast(SpellQ.SpellClass.SpellData.SpellName))
            {
                var missingHealthPercent = 100f - target.HealthPercent;
                dmg *= missingHealthPercent;
            }
            return DamageCalculator.CalculateActualDamage(UnitManager.MyChampion, target, dmg);
        }
        private float EDamage(GameObjectBase target)
        {
            if (!IsFirstCast(SpellQ.SpellClass.SpellData.SpellName))
            {
                return 0;
            }

            var baseDmg = 70 + SpellE.SpellClass.Level * 30f;
            var scaleDmg = UnitManager.MyChampion.UnitStats.BonusAttackDamage;
            var dmg = baseDmg + scaleDmg;
            return DamageCalculator.CalculateActualDamage(UnitManager.MyChampion, target, 0, dmg, 0);
        }

        private float RDamage(GameObjectBase target)
        {
            var baseDmg = -50 + SpellR.SpellClass.Level * 225;
            var scaleDmg = 2f * UnitManager.MyChampion.UnitStats.BonusAttackDamage;

            return DamageCalculator.CalculateActualDamage(UnitManager.MyChampion, target, baseDmg + scaleDmg);
        }

        internal override void OnCoreMainInput()
        {
            //if (UnitManager.EnemyChampions.Any(x => x.IsAlive && x.Distance <= 350))
            //{
            //    var target = SpellQ.GetTargets(Orbwalker.OrbWalkingModeType.Combo).FirstOrDefault(x => x.Distance <= SpellR.Range() && x.Health <= ComboDamage(x));
            //    if (target is not null &&
            //        SpellQ.SpellClass.IsSpellReady &&
            //        SpellE.SpellClass.IsSpellReady &&
            //        SpellR.SpellClass.IsSpellReady)
            //    {
            //        Logger.Log("casting q");
            //        SpellQ.ExecuteCastSpell();
            //        Logger.Log("casting e");
            //        SpellE.ExecuteCastSpell();
            //        Logger.Log("casting r");
            //        SpellR.ExecuteCastSpell();
            //        Logger.Log("casting q2");
            //        SpellQ2.ExecuteCastSpell();
            //    }
            //}

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
