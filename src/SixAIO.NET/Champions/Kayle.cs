using Oasys.Common;
using Oasys.Common.Enums.GameEnums;
using Oasys.Common.GameObject;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using Oasys.SDK.Tools;
using SixAIO.Helpers;
using SixAIO.Models;
using System;
using System.Linq;

namespace SixAIO.Champions
{
    internal class Kayle : Champion
    {
        public Kayle()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => QHitChance,
                Range = () => 900,
                Radius = () => 150,
                Speed = () => 1600,
                IsEnabled = () => UseQ,
                MinimumMana = () => QMinMana,
                TargetSelect = (mode) => SpellQ.GetTargets(mode).FirstOrDefault()
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                IsEnabled = () => UseW,
                MinimumMana = () => WMinMana,
                ShouldCast = (mode, target, spellClass, damage) => UnitManager.MyChampion.HealthPercent <= WHealBelowPercent,
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                IsEnabled = () => UseE,
                ShouldCast = (mode, target, spellClass, damage) =>
                {
                    if (EOnlyExecute)
                    {
                        var executeTarget = UnitManager.EnemyChampions
                            .Where(x => TargetSelector.IsAttackable(x) &&
                                        ((UnitManager.MyChampion.Level < 6 && x.Distance < 525 + UnitManager.MyChampion.UnitComponentInfo.UnitBoundingRadius) ||
                                         TargetSelector.IsInRange(x)) &&
                                         EDamage(x) > x.Health)
                            .OrderBy(EDamage)
                            .FirstOrDefault();
                        if (executeTarget != null)
                        {
                            Orbwalker.SelectedTarget = executeTarget;
                            return true;
                        }
                    }
                    else
                    {
                        if (UnitManager.MyChampion.Level < 6)
                        {
                            return UnitManager.EnemyChampions.Any(x =>
                                                                x.Distance < 525 + UnitManager.MyChampion.UnitComponentInfo.UnitBoundingRadius &&
                                                                TargetSelector.IsAttackable(x));
                        }
                        else
                        {
                            return TargetSelector.IsAttackable(Orbwalker.TargetHero) && TargetSelector.IsInRange(Orbwalker.TargetHero);
                        }
                    }

                    return false;
                }
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                IsTargetted = () => true,
                TargetSelect = (mode) => UnitManager.MyChampion,
                IsEnabled = () => UseR,
                MinimumMana = () => RMinMana,
                ShouldCast = (mode, target, spellClass, damage) =>
                {
                    return UnitManager.MyChampion.HealthPercent < RBelowHealthPercent &&
                           UnitManager.EnemyChampions.Count(x => TargetSelector.IsAttackable(x) && x.Distance < REnemiesCloserThan) > RIfMoreThanEnemiesNear;
                },
            };
        }

        internal static float EDamage(GameObjectBase target)
        {
            if (target == null)
            {
                return 0;
            }

            var missingHealth = target.MaxHealth - target.Health;
            var percentMissingHealthDamage = 7 + (UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.E).Level) + UnitManager.MyChampion.UnitStats.TotalAbilityPower * 0.02;
            var dmg = missingHealth / percentMissingHealthDamage;
            var result = (float)dmg * DamageCalculator.GetMagicResistMod(UnitManager.MyChampion, target);
            result += DamageCalculator.GetNextBasicAttackDamage(UnitManager.MyChampion, target);
            return result;
        }

        internal override void OnCoreMainInput()
        {
            if (SpellE.ExecuteCastSpell() || SpellQ.ExecuteCastSpell() || SpellW.ExecuteCastSpell() || SpellR.ExecuteCastSpell())
            {
                return;
            }
        }

        private int QMinMana
        {
            get => MenuTab.GetItem<Counter>("Q Min Mana").Value;
            set => MenuTab.GetItem<Counter>("Q Min Mana").Value = value;
        }

        private int WMinMana
        {
            get => MenuTab.GetItem<Counter>("W Min Mana").Value;
            set => MenuTab.GetItem<Counter>("W Min Mana").Value = value;
        }

        private int WHealBelowPercent
        {
            get => MenuTab.GetItem<Counter>("W Heal Below Percent").Value;
            set => MenuTab.GetItem<Counter>("W Heal Below Percent").Value = value;
        }

        private bool EOnlyExecute
        {
            get => MenuTab.GetItem<Switch>("E Only Execute").IsOn;
            set => MenuTab.GetItem<Switch>("E Only Execute").IsOn = value;
        }

        private int RMinMana
        {
            get => MenuTab.GetItem<Counter>("R Min Mana").Value;
            set => MenuTab.GetItem<Counter>("R Min Mana").Value = value;
        }

        private int RBelowHealthPercent
        {
            get => MenuTab.GetItem<Counter>("R Below Health Percent").Value;
            set => MenuTab.GetItem<Counter>("R Below Health Percent").Value = value;
        }

        private int RIfMoreThanEnemiesNear
        {
            get => MenuTab.GetItem<Counter>("R If More Than Enemies Near").Value;
            set => MenuTab.GetItem<Counter>("R If More Than Enemies Near").Value = value;
        }

        private int REnemiesCloserThan
        {
            get => MenuTab.GetItem<Counter>("R Enemies Closer Than").Value;
            set => MenuTab.GetItem<Counter>("R Enemies Closer Than").Value = value;
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Kayle)}"));
            MenuTab.AddItem(new InfoDisplay() { Title = "---Q Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            MenuTab.AddItem(new Counter() { Title = "Q Min Mana", MinValue = 0, MaxValue = 500, Value = 150, ValueFrequency = 10 });
            MenuTab.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            MenuTab.AddItem(new InfoDisplay() { Title = "---W Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use W", IsOn = true });
            MenuTab.AddItem(new Counter() { Title = "W Min Mana", MinValue = 0, MaxValue = 500, Value = 150, ValueFrequency = 10 });
            MenuTab.AddItem(new Counter() { Title = "W Heal Below Percent", Value = 30, MinValue = 0, MaxValue = 100, ValueFrequency = 5 });
            MenuTab.AddItem(new InfoDisplay() { Title = "---E Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use E", IsOn = true });
            MenuTab.AddItem(new Switch() { Title = "E Only Execute", IsOn = false });
            MenuTab.AddItem(new InfoDisplay() { Title = "---R Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use R", IsOn = true });
            MenuTab.AddItem(new Counter() { Title = "R Min Mana", MinValue = 0, MaxValue = 500, Value = 150, ValueFrequency = 10 });
            MenuTab.AddItem(new Counter() { Title = "R Below Health Percent", Value = 20, MinValue = 0, MaxValue = 100, ValueFrequency = 5 });
            MenuTab.AddItem(new Counter() { Title = "R If More Than Enemies Near", MinValue = 0, MaxValue = 5, Value = 2, ValueFrequency = 1 });
            MenuTab.AddItem(new Counter() { Title = "R Enemies Closer Than", MinValue = 50, MaxValue = 600, Value = 350, ValueFrequency = 50 });
        }
    }
}
