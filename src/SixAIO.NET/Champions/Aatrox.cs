using Oasys.Common.Enums.GameEnums;
using Oasys.Common.GameObject;
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
    internal sealed class Aatrox : Champion
    {
        public int QVersion => SpellQ.SpellClass.SpellData.SpellName switch
        {
            "AatroxQ" => 1,
            "AatroxQ2" => 2,
            "AatroxQ3" => 3,
            _ => 1
        };

        public Aatrox()
        {
            Spell.OnSpellCast += Spell_OnSpellCast;
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                ShouldDraw = () => DrawQRange,
                DrawColor = () => DrawQColor,
                MinimumHitChance = () => QHitChance,
                PredictionMode = () => QVersion switch
                {
                    1 => Prediction.MenuSelected.PredictionType.Line,
                    2 => Prediction.MenuSelected.PredictionType.Line,
                    3 => Prediction.MenuSelected.PredictionType.Circle,
                    _ => Prediction.MenuSelected.PredictionType.Line,
                },
                Delay = () => 0.6f,
                Range = () => QVersion switch
                {
                    1 => 625,
                    2 => 475,
                    3 => 200,
                    _ => 625,
                },
                Radius = () => QVersion switch
                {
                    1 => 180,
                    2 => Math.Min(500, (7700 / 23) + (GetQTarget(Orbwalker.OrbWalkingModeType.Combo).Distance * 8 / 23)), // Equal to: 300 + (100 * 200 / 575) + (distance * 200 / 575)
                    3 => 300,
                    _ => 180,
                },
                Speed = () => 10000,
                IsSpellReady = (spellClass, minMana, minCharges) => spellClass.IsSpellReady,
                IsEnabled = () => UseQ,
                TargetSelect = GetQTarget
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                ShouldDraw = () => DrawWRange,
                DrawColor = () => DrawWColor,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => WHitChance,
                Delay = () => 0.25f,
                Range = () => 765,
                Radius = () => 160,
                Speed = () => 1800,
                IsEnabled = () => UseW,
                IsSpellReady = (spellClass, minMana, minCharges) => spellClass.IsSpellReady,
                TargetSelect = (mode) => SpellW.GetTargets(mode,
                                                    t => !OnlyWOnKnockedUpTargets
                                                    || t.BuffManager.ActiveBuffs.Any(b => b.EntryType == BuffType.Knockup || b.EntryType == BuffType.Knockback))
                                                .FirstOrDefault()
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                IsSpellReady = (spellClass, minMana, minCharges) => spellClass.IsSpellReady,
                IsTargetted = () => true,
                IsEnabled = () => UseE,
                ShouldCast = (mode, target, spellClass, damage) => target is not null && TargetSelector.IsAttackable(target) && !TargetSelector.IsInRange(target),
                TargetSelect = (mode) => UnitManager.EnemyChampions.Find(c => c.IsAlive && c.Distance <= c.TrueAttackRange + 300 && TargetSelector.IsAttackable(c))
            };
        }

        private void Spell_OnSpellCast(SDKSpell spell, GameObjectBase target)
        {
            if (spell.SpellSlot == SpellSlot.E)
            {
                Orbwalker.AttackReset();
            }
        }

        internal override void OnCoreRender()
        {
            SpellQ.DrawRange();
            SpellW.DrawRange();
        }

        internal override void OnCoreMainInput()
        {
            SpellQ.ExecuteCastSpell();
            SpellW.ExecuteCastSpell();
            SpellE.ExecuteCastSpell();
        }

        internal bool OnlyQOnKnockedUpTargets
        {
            get => QSettings.GetItem<Switch>("Only Q on Knocked up targets").IsOn;
            set => QSettings.GetItem<Switch>("Only Q on Knocked up targets").IsOn = value;
        }

        internal bool OnlyWOnKnockedUpTargets
        {
            get => WSettings.GetItem<Switch>("Only W on Knocked up targets").IsOn;
            set => WSettings.GetItem<Switch>("Only W on Knocked up targets").IsOn = value;
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Aatrox)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("E Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "VeryHigh" });
            QSettings.AddItem(new Switch() { Title = "Only Q on Knocked up targets", IsOn = true });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });
            WSettings.AddItem(new ModeDisplay() { Title = "W HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "Immobile" });
            WSettings.AddItem(new Switch() { Title = "Only W on Knocked up targets", IsOn = true });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });


            MenuTab.AddDrawOptions(SpellSlot.Q, SpellSlot.W);
        }

        private GameObjectBase GetQTarget(Orbwalker.OrbWalkingModeType orbWalkingMode) =>
            SpellQ.GetTargets(orbWalkingMode,
                    t => !OnlyQOnKnockedUpTargets
                    || t.BuffManager.ActiveBuffs.Any(b => b.EntryType == BuffType.Knockup || b.EntryType == BuffType.Knockback))
                .FirstOrDefault();
    }
}
