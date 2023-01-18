using Oasys.Common.Enums.GameEnums;
using Oasys.Common.GameObject;
using Oasys.Common.GameObject.Clients;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SixAIO.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SixAIO.Champions
{
    internal sealed class Khazix : Champion
    {
        private static List<GameObjectBase> _isolations = new();
        private static IEnumerable<GameObjectBase> Isolations() => _isolations.Where(x => x.Distance <= 2000).Where(IsIsolation);

        private static bool IsIsolation(GameObjectBase obj)
        {
            return obj.Name.Contains("Khazix", StringComparison.OrdinalIgnoreCase) && obj.Name.Contains("SingleEnemy_Indicator", StringComparison.OrdinalIgnoreCase);
        }

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
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Circle,
                MinimumHitChance = () => EHitChance,
                Range = () => 900,
                Speed = () => 1500,
                Radius = () => 260,
                IsEnabled = () => UseE,
                TargetSelect = (mode) => OnlyEIfCanKill
                                    ? SpellE.GetTargets(mode, x => x.Distance >= EMinimumJumpRange).FirstOrDefault(x => DamageCalculator.GetTargetHealthAfterBasicAttack(UnitManager.MyChampion, x) <= ComboDamage(x))
                                    : SpellE.GetTargets(mode, x => x.Distance >= EMinimumJumpRange).FirstOrDefault()
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
            if (SpellW.SpellClass.IsSpellReady)
            {
                dmg += WDamage(target);
                //Logger.Log($"Combo damage W: {dmg}");
            }
            if (SpellE.SpellClass.IsSpellReady)
            {
                dmg += EDamage(target);
                //Logger.Log($"Combo damage E: {dmg}");
            }
            //Logger.Log($"Combo damage: {dmg}");
            return dmg;
        }

        private float QDamage(GameObjectBase target)
        {
            var baseDmg = 45f + SpellQ.SpellClass.Level * 25f;
            var scaleDmg = 1.15f * UnitManager.MyChampion.UnitStats.BonusAttackDamage;
            var dmg = baseDmg + scaleDmg;
            if (IsIsolated(target))
            {
                dmg *= 2.1f;
            }
            return DamageCalculator.CalculateActualDamage(UnitManager.MyChampion, target, dmg);
        }

        private float WDamage(GameObjectBase target)
        {
            var baseDmg = 55f + SpellW.SpellClass.Level * 30f;
            var scaleDmg = UnitManager.MyChampion.UnitStats.BonusAttackDamage;
            return DamageCalculator.CalculateActualDamage(UnitManager.MyChampion, target, baseDmg + scaleDmg);
        }

        private float EDamage(GameObjectBase target)
        {
            var baseDmg = 30f + SpellE.SpellClass.Level * 35f;
            var scaleDmg = 0.2f * UnitManager.MyChampion.UnitStats.BonusAttackDamage;
            return DamageCalculator.CalculateActualDamage(UnitManager.MyChampion, target, baseDmg + scaleDmg);
        }

        private bool IsIsolated(GameObjectBase target)
        {
            return Isolations().Any(x => x.DistanceTo(target.Position) <= 50);
        }

        internal override void OnCoreMainInput()
        {
            SpellW.ExecuteCastSpell();
            SpellQ.ExecuteCastSpell();
            SpellE.ExecuteCastSpell();
        }

        internal override void OnCreateObject(AIBaseClient obj)
        {
            if (IsIsolation(obj))
            {
                _isolations.Add(obj);
            }
        }

        internal override void OnDeleteObject(AIBaseClient obj)
        {
            _isolations.Remove(obj);
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

        internal bool OnlyEIfCanKill
        {
            get => ESettings.GetItem<Switch>("Only E If Can Kill").IsOn;
            set => ESettings.GetItem<Switch>("Only E If Can Kill").IsOn = value;
        }

        internal int EMinimumJumpRange
        {
            get => ESettings.GetItem<Counter>("E Minimum Jump Range").Value;
            set => ESettings.GetItem<Counter>("E Minimum Jump Range").Value = value;
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Khazix)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("E Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });
            WSettings.AddItem(new ModeDisplay() { Title = "W HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "VeryHigh" });
            WSettings.AddItem(new Counter() { Title = "W Maximum Range", MinValue = 0, MaxValue = 1000, Value = 950, ValueFrequency = 50 });
            WSettings.AddItem(new Counter() { Title = "W If Health Percent Below", MinValue = 0, MaxValue = 100, Value = 80, ValueFrequency = 5 });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });
            ESettings.AddItem(new ModeDisplay() { Title = "E HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "VeryHigh" });
            ESettings.AddItem(new Switch() { Title = "Only E If Can Kill", IsOn = true });
            ESettings.AddItem(new Counter() { Title = "E Minimum Jump Range", MinValue = 0, MaxValue = 900, Value = 500, ValueFrequency = 25 });

        }
    }
}
