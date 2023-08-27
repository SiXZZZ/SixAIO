using Oasys.Common.Enums.GameEnums;
using Oasys.Common.GameObject;
using Oasys.Common.GameObject.ObjectClass;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SixAIO.Extensions;
using SixAIO.Models;
using System;
using System.Linq;

namespace SixAIO.Champions
{
    internal sealed class Janna : Champion
    {
        public Janna()
        {
            Spell.OnSpellCast += Spell_OnSpellCast;
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                ShouldDraw = () => DrawQRange,
                DrawColor = () => DrawQColor,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => QHitChance,
                Range = () => 1100,
                Radius = () => 240,
                Speed = () => 1050,
                IsEnabled = () => UseQ,
                TargetSelect = (mode) => SpellQ.GetTargets(mode).FirstOrDefault()
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                ShouldDraw = () => DrawWRange,
                DrawColor = () => DrawWColor,
                IsTargetted = () => true,
                Range = () => 650,
                IsEnabled = () => UseW,
                TargetSelect = (mode) => SpellW.GetTargets(mode).FirstOrDefault()
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
                                            .FirstOrDefault(ally => ally.IsAlive && ally.Distance <= 650 && TargetSelector.IsAttackable(ally, false) &&
                                                                            IsCastingSpellOnEnemy(ally));
                    }

                    if (target == null && EShieldAlly)
                    {
                        target = UnitManager.AllyChampions
                                            .OrderByDescending(ally => MenuTab.GetItem<Counter>("Buff Ally Prio- " + ally?.ModelName)?.Value)
                                            .FirstOrDefault(ally => ally.IsAlive && ally.Distance <= 650 && TargetSelector.IsAttackable(ally, false) &&
                                                                    AnyEnemyIsCastingSpell(ally) &&
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
                Range = () => REnemiesCloserThan,
                ShouldCast = (mode, target, spellClass, damage) => UnitManager.EnemyChampions.Count(x => TargetSelector.IsAttackable(x) && x.Distance <= REnemiesCloserThan) >= RIfMoreThanEnemiesNear,
            };
        }

        private void Spell_OnSpellCast(SDKSpell spell, GameObjectBase target)
        {
            if (spell.CastSlot == CastSlot.Q)
            {
                SpellCastProvider.CastSpell(CastSlot.Q);
            }
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
                            return (target.IsAlive && UnitManager.EnemyChampions.Any(x => x.IsAlive && x.NetworkID == target.NetworkID)) ||
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

        private bool AnyEnemyIsCastingSpell(Hero ally)
        {
            return UnitManager.EnemyChampions.Any(x => x.IsAlive && IsCastingSpellOnAlly(x, ally));
        }

        private bool IsCastingSpellOnAlly(Hero enemy, Hero ally)
        {
            if (enemy.IsCastingSpell)
            {
                var spell = enemy.GetCurrentCastingSpell();
                if (spell != null && spell.Targets.Any(x => ally.NetworkID == x.NetworkID))
                {
                    return true;
                }
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


        private int RIfMoreThanEnemiesNear
        {
            get => RSettings.GetItem<Counter>("R X >= Enemies Near").Value;
            set => RSettings.GetItem<Counter>("R X >= Enemies Near").Value = value;
        }

        private int REnemiesCloserThan
        {
            get => RSettings.GetItem<Counter>("R Enemies Closer Than").Value;
            set => RSettings.GetItem<Counter>("R Enemies Closer Than").Value = value;
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Janna)}"));
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
            QSettings.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });
            ESettings.AddItem(new Switch() { Title = "E Buff ally", IsOn = true });
            ESettings.AddItem(new Switch() { Title = "E Shield ally", IsOn = true });
            ESettings.AddItem(new Counter() { Title = "E Shield Health Percent", MinValue = 0, MaxValue = 100, Value = 30, ValueFrequency = 5 });


            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            RSettings.AddItem(new Counter() { Title = "R X >= Enemies Near", MinValue = 1, MaxValue = 5, Value = 2, ValueFrequency = 1 });
            RSettings.AddItem(new Counter() { Title = "R Enemies Closer Than", MinValue = 50, MaxValue = 700, Value = 400, ValueFrequency = 50 });


            MenuTab.AddDrawOptions(SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R);

        }
    }
}
