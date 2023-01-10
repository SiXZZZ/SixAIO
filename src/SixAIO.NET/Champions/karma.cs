using Oasys.Common.Enums.GameEnums;
using Oasys.Common.GameObject;
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
    internal sealed class Karma : Champion
    {
        public Karma()
        {
            Spell.OnSpellCast += Spell_OnSpellCast;
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                AllowCollision = (target, collisions) => !collisions.Any(),
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => QHitChance,
                Range = () => 900,
                Radius = () => 120,
                Speed = () => 1700,
                IsEnabled = () => UseQ,
                TargetSelect = (mode) => SpellQ.GetTargets(mode).FirstOrDefault()
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                IsTargetted = () => true,
                IsEnabled = () => UseW,
                Range = () => 675,
                TargetSelect = (mode) => SpellW.GetTargets(mode).FirstOrDefault()
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                IsTargetted = () => true,
                IsEnabled = () => UseE,
                TargetSelect = (mode) => UnitManager.AllyChampions
                                            .FirstOrDefault(ally => ally.IsAlive && ally.Distance <= 800 && TargetSelector.IsAttackable(ally, false) &&
                                                                    AnyEnemyIsCastingSpell(ally) &&
                                                                    ally.HealthPercent < EShieldHealthPercent)
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                IsEnabled = () => UseR,
                ShouldCast = (mode, target, spellClass, damage) => SpellQ.GetTargets(mode).Any()
            };
        }

        private void Spell_OnSpellCast(SDKSpell spell, GameObjectBase target)
        {
            if (spell.CastSlot == CastSlot.R)
            {
                SpellQ.ExecuteCastSpell();
            }
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

        internal override void OnCoreMainInput()
        {
            if (SpellR.ExecuteCastSpell() || SpellE.ExecuteCastSpell() || SpellW.ExecuteCastSpell() || SpellQ.ExecuteCastSpell())
            {
                return;
            }
        }

        private int EShieldHealthPercent
        {
            get => ESettings.GetItem<Counter>("E Shield Health Percent").Value;
            set => ESettings.GetItem<Counter>("E Shield Health Percent").Value = value;
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Karma)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });
            ESettings.AddItem(new Counter() { Title = "E Shield Health Percent", MinValue = 0, MaxValue = 100, Value = 70, ValueFrequency = 5 });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });

        }
    }
}
