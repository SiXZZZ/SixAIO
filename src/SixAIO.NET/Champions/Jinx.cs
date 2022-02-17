using Oasys.Common.Enums.GameEnums;
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
                ShouldCast = (target, spellClass, damage) =>
                {
                    if (UseQ && spellClass.IsSpellReady && UnitManager.MyChampion.Mana > 40)
                    {
                        var usingRockets = IsQActive();
                        var extraRange = 75 + (25 * UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.Q).Level);
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
                            return UnitManager.EnemyChampions.Any(x => x.Distance < rocketRange && TargetSelector.IsAttackable(x));
                        }
                    }

                    return false;
                }
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                AllowCollision = (target, collisions) => !collisions.Any(),
                PredictionType = Prediction.MenuSelected.PredictionType.Line,
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
                ShouldCast = (target, spellClass, damage) =>
                            UseW &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 90 &&
                            target != null,
                TargetSelect = (mode) => SpellW.GetTargets(mode).FirstOrDefault()
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                ShouldCast = (target, spellClass, damage) =>
                            UseE &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 90 &&
                            target != null,
                TargetSelect = (mode) =>
                            UnitManager.EnemyChampions
                            .FirstOrDefault(x => x.Distance <= 850 && x.IsAlive && BuffChecker.IsCrowdControlled(x))
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                PredictionType = Prediction.MenuSelected.PredictionType.Line,
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
                ShouldCast = (target, spellClass, damage) =>
                            UseR &&
                            spellClass.IsSpellReady &&
                            UnitManager.MyChampion.Mana > 100 &&
                            target != null,
                TargetSelect = (mode) => SpellR.GetTargets(mode).FirstOrDefault(x => (x.Health / x.MaxHealth * 100) < 50)
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

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Jinx)}"));
            MenuTab.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            MenuTab.AddItem(new Switch() { Title = "Use W", IsOn = true });
            MenuTab.AddItem(new ModeDisplay() { Title = "W HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            MenuTab.AddItem(new Switch() { Title = "Use E", IsOn = true });
            MenuTab.AddItem(new Switch() { Title = "Use R", IsOn = true });
            MenuTab.AddItem(new ModeDisplay() { Title = "R HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

        }
    }
}
