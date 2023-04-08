using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Extensions;
using Oasys.Common.GameObject.ObjectClass;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SixAIO.Extensions;
using SixAIO.Helpers;
using SixAIO.Models;
using System;
using System.Linq;
using static Oasys.Common.Logic.Orbwalker;

namespace SixAIO.Champions
{
    internal sealed class Milio : Champion
    {
        public Milio()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                ShouldDraw = () => DrawQRange,
                DrawColor = () => DrawQColor,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => QHitChance,
                Range = () => 1000,
                Radius = () => 60,
                Speed = () => 1200,
                IsEnabled = () => UseQ,
                TargetSelect = (mode) => SpellQ.GetTargets(mode).FirstOrDefault()
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                ShouldDraw = () => DrawWRange,
                DrawColor = () => DrawWColor,
                IsTargetted = () => true,
                IsEnabled = () => UseW,
                Range = () => 650,
                TargetSelect = (mode) =>
                {
                    var target = UnitManager.AllyChampions
                                            .Where(ally => MenuTab.GetItem<Counter>("Buff Ally Prio- " + ally?.ModelName)?.Value > 0)
                                            .OrderByDescending(ally => MenuTab.GetItem<Counter>("Buff Ally Prio- " + ally?.ModelName)?.Value)
                                            .FirstOrDefault(ally => ally.IsAlive && ally.Distance <= SpellW.Range() &&
                                                                            TargetSelector.IsAttackable(ally, false) &&
                                                                            IsCastingSpellOnEnemy(ally));

                    return target;
                }
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                ShouldDraw = () => DrawERange,
                DrawColor = () => DrawEColor,
                IsTargetted = () => true,
                IsEnabled = () => UseE,
                Range = () => 650,
                TargetSelect = (mode) =>
                {
                    Hero target = null;

                    if (target == null && EBuffAlly)
                    {
                        target = UnitManager.AllyChampions
                                            .Where(ally => MenuTab.GetItem<Counter>("Buff Ally Prio- " + ally?.ModelName)?.Value > 0)
                                            .OrderByDescending(ally => MenuTab.GetItem<Counter>("Buff Ally Prio- " + ally?.ModelName)?.Value)
                                            .FirstOrDefault(ally => ally.IsAlive && ally.Distance <= SpellE.Range() && TargetSelector.IsAttackable(ally, false) &&
                                                                            IsCastingSpellOnEnemy(ally));
                    }

                    if (target == null && EShieldAlly)
                    {
                        target = UnitManager.AllyChampions
                                            .Where(ally => MenuTab.GetItem<Counter>("Buff Ally Prio- " + ally?.ModelName)?.Value > 0)
                                            .OrderByDescending(ally => MenuTab.GetItem<Counter>("Buff Ally Prio- " + ally?.ModelName)?.Value)
                                            .FirstOrDefault(ally => ally.IsAlive && ally.Distance <= SpellE.Range() && TargetSelector.IsAttackable(ally, false) &&
                                                                    UnitManager.EnemyChampions.Any(x => x.IsAlive && x.Distance <= 1000 && x.IsCastingSpell && x.GetCurrentCastingSpell()?.TargetIndexes?.Any() == true) &&
                                                                    ally.HealthPercent < EShieldHealthPercent);
                    }

                    if (target == null && EShieldAlly)
                    {
                        target = UnitManager.AllyChampions
                                            .Where(ally => MenuTab.GetItem<Counter>("Buff Ally Prio- " + ally?.ModelName)?.Value > 0)
                                            .OrderByDescending(ally => MenuTab.GetItem<Counter>("Buff Ally Prio- " + ally?.ModelName)?.Value)
                                            .FirstOrDefault(ally => ally.IsAlive && ally.Distance <= SpellE.Range() && TargetSelector.IsAttackable(ally, false) &&
                                                                    UnitManager.EnemyChampions.Any(x => x.IsAlive && x.Distance <= 1000 && x.IsCastingSpell) &&
                        ally.HealthPercent < EShieldHealthPercent);
                    }

                    return target;
                }
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                ShouldDraw = () => DrawRRange,
                DrawColor = () => DrawRColor,
                IsEnabled = () => UseR,
                Range = () => 700,
                ShouldCast = (mode, target, spellClass, damage) =>
                {
                    return UnitManager.AllyChampions
                                            .Where(ally => MenuTab.GetItem<Counter>("Buff Ally Prio- " + ally?.ModelName)?.Value > 0)
                                            .Count(ally => ally.IsAlive && ally.Distance <= SpellR.Range() && TargetSelector.IsAttackable(ally, false) &&
                                                                    UnitManager.EnemyChampions.Any(x => x.IsAlive && x.Distance <= 2000) &&
                                                                    (ally.HealthPercent < RHealHealthPercent) || BuffChecker.IsCrowdControlledButCanQss(ally, false))
                                            >= RIfMoreThanAlliesNear;
                },
            };
        }

        private bool IsCastingSpellOnEnemy(Hero ally)
        {
            try
            {
                if (ally.IsAlive && ally.IsCastingSpell)
                {
                    var spell = ally.GetCurrentCastingSpell();
                    if (spell != null && (spell.SpellSlot == SpellSlot.BasicAttack))
                    {
                        var target = spell.Targets.FirstOrDefault(x => x.IsAlive && x.IsVisible && x.IsTargetable);
                        if (target != null)
                        {
                            return (spell.SpellSlot == SpellSlot.BasicAttack && target.IsAlive && UnitManager.EnemyChampions.Any(x => x.IsAlive && x.NetworkID == target.NetworkID)) ||
                                   (ally.ModelName == "Zeri" && spell.SpellSlot == SpellSlot.Q);
                        }
                    }
                }
            }
            catch (Exception)
            {
            }

            return false;
        }

        internal override void OnCoreRender()
        {
            SpellQ.DrawRange();
            SpellW.DrawRange();
            SpellE.DrawRange();
            SpellR.DrawRange();
        }

        internal override void OnCoreMainInput()
        {
            SpellQ.ExecuteCastSpell();
            SpellW.ExecuteCastSpell();
            SpellE.ExecuteCastSpell();
            SpellR.ExecuteCastSpell();
        }

        private bool EBuffAlly
        {
            get => ESettings.GetItem<Switch>("E Buff ally").IsOn;
            set => ESettings.GetItem<Switch>("E Buff ally").IsOn = value;
        }

        private bool EShieldAlly
        {
            get => ESettings.GetItem<Switch>("E Shield ally").IsOn;
            set => ESettings.GetItem<Switch>("E Shield ally").IsOn = value;
        }

        private int EShieldHealthPercent
        {
            get => ESettings.GetItem<Counter>("E Shield Health Percent").Value;
            set => ESettings.GetItem<Counter>("E Shield Health Percent").Value = value;
        }

        private int RIfMoreThanAlliesNear
        {
            get => RSettings.GetItem<Counter>("R If More Than Allies Near").Value;
            set => RSettings.GetItem<Counter>("R If More Than Allies Near").Value = value;
        }

        private int RHealHealthPercent
        {
            get => RSettings.GetItem<Counter>("R Heal Health Percent").Value;
            set => RSettings.GetItem<Counter>("R Heal Health Percent").Value = value;
        }


        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Milio)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            MenuTab.AddItem(new InfoDisplay() { Title = "---Allies to buff - 0 to disable---" });
            foreach (var allyChampion in UnitManager.AllyChampions)
            {
                MenuTab.AddItem(new Counter() { Title = "Buff Ally Prio- " + allyChampion.ModelName, MinValue = 0, MaxValue = 5, Value = 0 });
            }


            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "VeryHigh" });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });
            ESettings.AddItem(new Switch() { Title = "E Buff ally", IsOn = true });
            ESettings.AddItem(new Switch() { Title = "E Shield ally", IsOn = true });
            ESettings.AddItem(new Counter() { Title = "E Shield Health Percent", MinValue = 0, MaxValue = 100, Value = 30, ValueFrequency = 5 });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            RSettings.AddItem(new Counter() { Title = "R If More Than Allies Near", MinValue = 0, MaxValue = 5, Value = 2, ValueFrequency = 1 });
            RSettings.AddItem(new Counter() { Title = "R Heal Health Percent", MinValue = 0, MaxValue = 100, Value = 30, ValueFrequency = 5 });


            MenuTab.AddDrawOptions(SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R);
        }
    }
}
