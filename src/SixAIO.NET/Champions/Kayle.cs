using Oasys.Common.Enums.GameEnums;
using Oasys.Common.GameObject;
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

        private int WHealBelowPercent
        {
            get => WSettings.GetItem<Counter>("W Heal Below Percent").Value;
            set => WSettings.GetItem<Counter>("W Heal Below Percent").Value = value;
        }

        private bool EOnlyExecute
        {
            get => ESettings.GetItem<Switch>("E Only Execute").IsOn;
            set => ESettings.GetItem<Switch>("E Only Execute").IsOn = value;
        }

        private int RBelowHealthPercent
        {
            get => RSettings.GetItem<Counter>("R Below Health Percent").Value;
            set => RSettings.GetItem<Counter>("R Below Health Percent").Value = value;
        }

        private int RIfMoreThanEnemiesNear
        {
            get => RSettings.GetItem<Counter>("R If More Than Enemies Near").Value;
            set => RSettings.GetItem<Counter>("R If More Than Enemies Near").Value = value;
        }

        private int REnemiesCloserThan
        {
            get => RSettings.GetItem<Counter>("R Enemies Closer Than").Value;
            set => RSettings.GetItem<Counter>("R Enemies Closer Than").Value = value;
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Kayle)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new Counter() { Title = "Q Min Mana", MinValue = 0, MaxValue = 500, Value = 150, ValueFrequency = 10 });
            QSettings.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });
            WSettings.AddItem(new Counter() { Title = "W Min Mana", MinValue = 0, MaxValue = 500, Value = 150, ValueFrequency = 10 });
            WSettings.AddItem(new Counter() { Title = "W Heal Below Percent", Value = 30, MinValue = 0, MaxValue = 100, ValueFrequency = 5 });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });
            ESettings.AddItem(new Switch() { Title = "E Only Execute", IsOn = false });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            RSettings.AddItem(new Counter() { Title = "R Min Mana", MinValue = 0, MaxValue = 500, Value = 150, ValueFrequency = 10 });
            RSettings.AddItem(new Counter() { Title = "R Below Health Percent", Value = 20, MinValue = 0, MaxValue = 100, ValueFrequency = 5 });
            RSettings.AddItem(new Counter() { Title = "R If More Than Enemies Near", MinValue = 0, MaxValue = 5, Value = 2, ValueFrequency = 1 });
            RSettings.AddItem(new Counter() { Title = "R Enemies Closer Than", MinValue = 50, MaxValue = 600, Value = 350, ValueFrequency = 50 });
        }
    }
}
