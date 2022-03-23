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
    internal class Jinx : Champion
    {
        private static bool IsQActive()
        {
            var buff = UnitManager.MyChampion.BuffManager.GetBuffByName("JinxQ", false, true);
            return buff != null && buff.IsActive && buff.Stacks >= 1;
        }

        public Jinx()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                IsEnabled = () => UseQ,
                ShouldCast = (mode, target, spellClass, damage) =>
                {
                    var usingRockets = IsQActive();
                    var extraRange = 50 + (30 * UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.Q).Level);
                    var minigunRange = usingRockets
                                        ? UnitManager.MyChampion.TrueAttackRange - extraRange
                                        : UnitManager.MyChampion.TrueAttackRange;
                    var rocketRange = usingRockets
                                        ? UnitManager.MyChampion.TrueAttackRange
                                        : UnitManager.MyChampion.TrueAttackRange + extraRange;

                    //if (!UnitManager.EnemyChampions.Any(x => x.Distance < 1200 && TargetSelector.IsAttackable(x)))
                    //{
                    //    return usingRockets;
                    //}
                    if (usingRockets && Orbwalker.TargetHero != null && Orbwalker.TargetHero.Distance <= minigunRange)
                    {
                        return usingRockets;
                    }
                    if (!usingRockets && Orbwalker.TargetHero == null)
                    {
                        return QPreferRockets
                                ? !usingRockets
                                : UnitManager.EnemyChampions.Any(x => x.Distance < rocketRange && TargetSelector.IsAttackable(x));
                    }

                    return false;
                }
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                AllowCollision = (target, collisions) => !collisions.Any(),
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => WHitChance,
                Range = () => 1500,
                Radius = () => 120,
                Speed = () => 3300,
                Delay = () => 0.6f,
                //Damage = (target, spellClass) =>
                //            target != null
                //            ? DamageCalculator.GetArmorMod(UnitManager.MyChampion, target) *
                //            ((10 + spellClass.Level * 40) +
                //            (UnitManager.MyChampion.UnitStats.TotalAttackDamage * (1.15f + 0.15f * spellClass.Level)))
                //            : 0,
                IsEnabled = () => UseW,
                IsSpellReady = (spellClass, minimumMana, minimumCharges) =>
                                spellClass.IsSpellReady &&
                                UnitManager.MyChampion.Mana >= spellClass.SpellData.ResourceCost &&
                                UnitManager.MyChampion.Mana >= minimumMana &&
                                spellClass.Charges >= minimumCharges &&
                                !WOnlyOutsideOfAttackRange || !UnitManager.EnemyChampions.Any(TargetSelector.IsInRange),
                TargetSelect = (mode) => SpellW.GetTargets(mode, x => x.Distance > WMinimumRange).FirstOrDefault()
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                IsTargetted = () => true,
                Range = () => 850,
                MinimumHitChance = () => Prediction.MenuSelected.HitChance.Immobile,
                IsEnabled = () => UseE,
                TargetSelect = (mode) => SpellE.GetTargets(mode).FirstOrDefault()
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                AllowCastOnMap = () => AllowRCastOnMinimap,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => RHitChance,
                Range = () => 30000,
                Radius = () => 280,
                Speed = () => 2000,
                Delay = () => 0.6f,
                //Damage = (target, spellClass) =>
                //            target != null
                //            ? DamageCalculator.GetMagicResistMod(UnitManager.MyChampion, target) *
                //                        ((100 + spellClass.Level * 150) +
                //                        (1.5f*UnitManager.MyChampion.UnitStats.BonusAttackDamage) +
                //                         (target.Health / target.MaxHealth * 100) < 50))
                //            : 0,
                IsEnabled = () => UseR,
                IsSpellReady = (spellClass, minimumMana, minimumCharges) =>
                                spellClass.IsSpellReady &&
                                UnitManager.MyChampion.Mana >= spellClass.SpellData.ResourceCost &&
                                UnitManager.MyChampion.Mana >= minimumMana &&
                                spellClass.Charges >= minimumCharges &&
                                !ROnlyOutsideOfAttackRange || !UnitManager.EnemyChampions.Any(TargetSelector.IsInRange),
                TargetSelect = (mode) => SpellR.GetTargets(mode, x => x.HealthPercent <= 50 && x.Distance > RMinimumRange && x.Distance <= RMaximumRange).FirstOrDefault()
            };
        }

        internal override void OnCoreMainInput()
        {
            if (SpellQ.ExecuteCastSpell() || SpellE.ExecuteCastSpell() || SpellW.ExecuteCastSpell() || SpellR.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void OnCoreLaneClearInput()
        {
            if (SpellQ.ExecuteCastSpell())
            {
                return;
            }
        }

        private bool QPreferRockets
        {
            get => MenuTab.GetItem<Switch>("Q prefer rockets").IsOn;
            set => MenuTab.GetItem<Switch>("Q prefer rockets").IsOn = value;
        }

        private bool WOnlyOutsideOfAttackRange
        {
            get => MenuTab.GetItem<Switch>("W only outside of attack range").IsOn;
            set => MenuTab.GetItem<Switch>("W only outside of attack range").IsOn = value;
        }

        private int WMinimumRange
        {
            get => MenuTab.GetItem<Counter>("W minimum range").Value;
            set => MenuTab.GetItem<Counter>("W minimum range").Value = value;
        }

        private bool ROnlyOutsideOfAttackRange
        {
            get => MenuTab.GetItem<Switch>("R only outside of attack range").IsOn;
            set => MenuTab.GetItem<Switch>("R only outside of attack range").IsOn = value;
        }

        private int RMinimumRange
        {
            get => MenuTab.GetItem<Counter>("R minimum range").Value;
            set => MenuTab.GetItem<Counter>("R minimum range").Value = value;
        }

        private int RMaximumRange
        {
            get => MenuTab.GetItem<Counter>("R maximum range").Value;
            set => MenuTab.GetItem<Counter>("R maximum range").Value = value;
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Jinx)}"));

            MenuTab.AddItem(new InfoDisplay() { Title = "---Q Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            MenuTab.AddItem(new Switch() { Title = "Q prefer rockets", IsOn = false });

            MenuTab.AddItem(new InfoDisplay() { Title = "---W Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use W", IsOn = true });
            MenuTab.AddItem(new ModeDisplay() { Title = "W HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });
            MenuTab.AddItem(new Switch() { Title = "W only outside of attack range", IsOn = false });
            MenuTab.AddItem(new Counter() { Title = "W minimum range", MinValue = 0, MaxValue = 1500, Value = 0, ValueFrequency = 50 });

            MenuTab.AddItem(new InfoDisplay() { Title = "---E Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use E", IsOn = true });

            MenuTab.AddItem(new InfoDisplay() { Title = "---R Settings---" });
            MenuTab.AddItem(new Switch() { Title = "Use R", IsOn = true });
            MenuTab.AddItem(new ModeDisplay() { Title = "R HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });
            MenuTab.AddItem(new Switch() { Title = "Allow R cast on minimap", IsOn = true });
            MenuTab.AddItem(new Switch() { Title = "R only outside of attack range", IsOn = false });
            MenuTab.AddItem(new Counter() { Title = "R minimum range", MinValue = 0, MaxValue = 30_000, Value = 0, ValueFrequency = 50 });
            MenuTab.AddItem(new Counter() { Title = "R maximum range", MinValue = 0, MaxValue = 30_000, Value = 30_000, ValueFrequency = 50 });
        }
    }
}
