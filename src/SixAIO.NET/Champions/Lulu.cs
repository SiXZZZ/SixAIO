using Oasys.Common.Enums.GameEnums;
using Oasys.Common.GameObject.ObjectClass;
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
    internal sealed class Lulu : Champion
    {
        public Lulu()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => QHitChance,
                Range = () => 950,
                Radius = () => 120,
                Speed = () => 1450,
                IsEnabled = () => UseQ,
                MinimumMana = () => QMinMana,
                TargetSelect = (mode) => SpellQ.GetTargets(mode).FirstOrDefault()
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                IsTargetted = () => true,
                IsEnabled = () => UseW,
                MinimumMana = () => WMinMana,
                TargetSelect = (mode) =>
                {
                    Hero target = null;

                    if (target == null && Polymorph && IsCastingSpellOnAlly())
                    {
                        target = UnitManager.EnemyChampions
                                            .OrderBy(x => x.Health)
                                            .FirstOrDefault(enemy => enemy.IsAlive && enemy.Distance <= 650 &&
                                                                     TargetSelector.IsAttackable(enemy) &&
                                                                     !TargetSelector.IsInvulnerable(enemy, Oasys.Common.Logic.DamageType.Magical, false));
                    }

                    if (target == null && WBuffAlly)
                    {
                        target = UnitManager.AllyChampions
                                            .Where(ally => MenuTab.GetItem<Counter>("Buff Ally Prio- " + ally?.ModelName)?.Value > 0)
                                            .OrderByDescending(ally => MenuTab.GetItem<Counter>("Buff Ally Prio- " + ally?.ModelName)?.Value)
                                            .FirstOrDefault(ally => ally.IsAlive && ally.Distance <= 650 &&
                                                                            TargetSelector.IsAttackable(ally, false) &&
                                                                            IsCastingSpellOnEnemy(ally));
                    }

                    return target;
                }
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                IsTargetted = () => true,
                IsEnabled = () => UseE,
                MinimumMana = () => EMinMana,
                TargetSelect = (mode) =>
                {
                    Hero target = null;

                    if (target == null && EBuffAlly)
                    {
                        target = UnitManager.AllyChampions
                                            .Where(ally => MenuTab.GetItem<Counter>("Buff Ally Prio- " + ally?.ModelName)?.Value > 0)
                                            .OrderByDescending(ally => MenuTab.GetItem<Counter>("Buff Ally Prio- " + ally?.ModelName)?.Value)
                                            .FirstOrDefault(ally => ally.IsAlive && ally.Distance <= 650 && TargetSelector.IsAttackable(ally, false) &&
                                                                            IsCastingSpellOnEnemy(ally));
                    }

                    if (target == null && EShieldAlly)
                    {
                        target = UnitManager.AllyChampions
                                            .Where(ally => MenuTab.GetItem<Counter>("Buff Ally Prio- " + ally?.ModelName)?.Value > 0)
                                            .OrderByDescending(ally => MenuTab.GetItem<Counter>("Buff Ally Prio- " + ally?.ModelName)?.Value)
                                            .FirstOrDefault(ally => ally.IsAlive && ally.Distance <= 650 && TargetSelector.IsAttackable(ally, false) &&
                                                                    AnyEnemyIsCastingSpell() &&
                                                                    ally.HealthPercent < EShieldHealthPercent);
                    }

                    if (target == null && EOnEnemies)
                    {
                        target = UnitManager.EnemyChampions
                                            .OrderBy(x => x.Health)
                                            .FirstOrDefault(enemy => enemy.IsAlive && enemy.Distance <= 650 && TargetSelector.IsAttackable(enemy));
                    }

                    return target;
                }
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                IsTargetted = () => true,
                IsEnabled = () => UseR,
                MinimumMana = () => RMinMana,
                TargetSelect = (mode) =>
                {
                    Hero target = null;

                    if (target == null && RKnockupEnemies)
                    {
                        target = UnitManager.AllyChampions
                                            .FirstOrDefault(allyChamp => allyChamp.IsAlive && allyChamp.Distance <= 900 && TargetSelector.IsAttackable(allyChamp, false) &&
                                                            UnitManager.EnemyChampions.Count(enemyChamp => enemyChamp.IsAlive && enemyChamp.DistanceTo(allyChamp.Position) < 400 && TargetSelector.IsAttackable(enemyChamp))
                                                            >= RSettings.GetItem<Counter>("Knockup - " + allyChamp?.ModelName)?.Value);
                    }

                    if (target == null && RBuffAlly)
                    {
                        target = UnitManager.AllyChampions
                                            .Where(ally => MenuTab.GetItem<Counter>("Buff Ally Prio- " + ally?.ModelName)?.Value > 0)
                                            .OrderByDescending(ally => MenuTab.GetItem<Counter>("Buff Ally Prio- " + ally?.ModelName)?.Value)
                                            .FirstOrDefault(ally => ally.IsAlive && ally.Distance <= 900 &&
                                                                    TargetSelector.IsAttackable(ally, false) &&
                                                                    ally.HealthPercent < RBuffHealthPercent);
                    }

                    return target;
                }
            };
        }

        private bool AnyEnemyIsCastingSpell()
        {
            return UnitManager.EnemyChampions.Any(x => x.IsAlive && x.Distance <= 1000 && x.IsCastingSpell);
        }

        private bool IsCastingSpellOnAlly(Hero ally = null)
        {
            foreach (var enemy in UnitManager.EnemyChampions.Where(x => x.IsAlive && x.IsCastingSpell))
            {
                var spell = enemy.GetCurrentCastingSpell();
                if (spell != null)
                {
                    var target = spell.Targets.FirstOrDefault(x => x.IsAlive && x.IsVisible && x.IsTargetable);
                    if (target != null && target.Distance <= 650 && target.IsAlive &&
                        (ally is null ? UnitManager.AllyChampions.Any(x => x.NetworkID == target.NetworkID) : ally.NetworkID == target.NetworkID))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool IsCastingSpellOnEnemy(Hero ally)
        {
            try
            {
                if (ally.IsAlive && ally.IsCastingSpell)
                {
                    var spell = ally.GetCurrentCastingSpell();
                    if (spell != null)
                    {
                        var target = spell.Targets.FirstOrDefault(x => x.IsAlive && x.IsVisible && x.IsTargetable);
                        if (target != null)
                        {
                            return (spell.IsBasicAttack && target.IsAlive && UnitManager.EnemyChampions.Any(x => x.IsAlive && x.NetworkID == target.NetworkID)) ||
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

        internal override void OnCoreMainInput()
        {
            if (SpellR.ExecuteCastSpell() || SpellE.ExecuteCastSpell() || SpellW.ExecuteCastSpell() || SpellQ.ExecuteCastSpell())
            {
                return;
            }
        }

        private bool WBuffAlly
        {
            get => WSettings.GetItem<Switch>("W Buff ally").IsOn;
            set => WSettings.GetItem<Switch>("W Buff ally").IsOn = value;
        }

        private bool Polymorph
        {
            get => WSettings.GetItem<Switch>("Polymorph").IsOn;
            set => WSettings.GetItem<Switch>("Polymorph").IsOn = value;
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

        private bool EOnEnemies
        {
            get => ESettings.GetItem<Switch>("E for damage on enemies").IsOn;
            set => ESettings.GetItem<Switch>("E for damage on enemies").IsOn = value;
        }

        private bool RBuffAlly
        {
            get => RSettings.GetItem<Switch>("R Buff ally").IsOn;
            set => RSettings.GetItem<Switch>("R Buff ally").IsOn = value;
        }

        private int RBuffHealthPercent
        {
            get => RSettings.GetItem<Counter>("R Buff Health Percent").Value;
            set => RSettings.GetItem<Counter>("R Buff Health Percent").Value = value;
        }

        private bool RKnockupEnemies
        {
            get => RSettings.GetItem<Switch>("R Knockup enemies").IsOn;
            set => RSettings.GetItem<Switch>("R Knockup enemies").IsOn = value;
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Lulu)}"));

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
            QSettings.AddItem(new Counter() { Title = "Q Min Mana", MinValue = 0, MaxValue = 500, Value = 70, ValueFrequency = 10 });
            QSettings.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });
            WSettings.AddItem(new Counter() { Title = "W Min Mana", MinValue = 0, MaxValue = 500, Value = 65, ValueFrequency = 10 });
            WSettings.AddItem(new Switch() { Title = "Polymorph", IsOn = true });
            WSettings.AddItem(new Switch() { Title = "W Buff ally", IsOn = true });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });
            ESettings.AddItem(new Counter() { Title = "E Min Mana", MinValue = 0, MaxValue = 500, Value = 80, ValueFrequency = 10 });
            ESettings.AddItem(new Switch() { Title = "E Buff ally", IsOn = true });
            ESettings.AddItem(new Switch() { Title = "E Shield ally", IsOn = true });
            ESettings.AddItem(new Counter() { Title = "E Shield Health Percent", MinValue = 0, MaxValue = 100, Value = 70, ValueFrequency = 5 });
            ESettings.AddItem(new Switch() { Title = "E for damage on enemies", IsOn = false });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            RSettings.AddItem(new Counter() { Title = "R Min Mana", MinValue = 0, MaxValue = 500, Value = 100, ValueFrequency = 10 });
            RSettings.AddItem(new Switch() { Title = "R Buff ally", IsOn = true });
            RSettings.AddItem(new Counter() { Title = "R Buff Health Percent", MinValue = 0, MaxValue = 100, Value = 20, ValueFrequency = 5 });

            RSettings.AddItem(new Switch() { Title = "R Knockup enemies", IsOn = true });
            RSettings.AddItem(new InfoDisplay() { Title = "-Min champs to knockup-" });
            foreach (var allyChampion in UnitManager.AllyChampions.Where(x => !x.IsTargetDummy))
            {
                RSettings.AddItem(new Counter() { Title = "Knockup - " + allyChampion.ModelName, MinValue = 1, MaxValue = 5, Value = 3, ValueFrequency = 1 });
            }

        }
    }
}
