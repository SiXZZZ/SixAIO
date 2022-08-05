using Oasys.Common.Enums.GameEnums;
using Oasys.Common.GameObject;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using Oasys.SDK.Tools;
using SixAIO.Enums;
using SixAIO.Models;
using System;
using System.Linq;

namespace SixAIO.Champions
{
    internal class Aatrox : Champion
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
                PredictionMode = () => QVersion switch
                {
                    1 => Prediction.MenuSelected.PredictionType.Line,
                    2 => Prediction.MenuSelected.PredictionType.Line,
                    3 => Prediction.MenuSelected.PredictionType.Circle,
                    _ => Prediction.MenuSelected.PredictionType.Line,
                },
                Delay = () => 0.6f,
                Speed = () => 10000,
                Range = () => QVersion switch
                {
                    1 => 625,
                    2 => 475,
                    3 => 300,
                    _ => 625,
                },
                Radius = () => QVersion switch
                {
                    1 => 180,
                    2 => 350,
                    3 => 300,
                    _ => 180,
                },
                IsEnabled = () => UseQ,
                TargetSelect = (mode) => SpellQ.GetTargets(mode).FirstOrDefault()
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => WHitChance,
                Range = () => 765,
                Radius = () => 160,
                Speed = () => 1800,
                IsEnabled = () => UseW,
                TargetSelect = (mode) => SpellW.GetTargets(mode).FirstOrDefault()
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                IsTargetted = () => true,
                IsEnabled = () => UseE,
                ShouldCast = (mode, target, spellClass, damage) => target is not null && TargetSelector.IsAttackable(target) && !TargetSelector.IsInRange(target),
                TargetSelect = (mode) => UnitManager.EnemyChampions.FirstOrDefault(x => x.Distance <= x.TrueAttackRange + 300 && x.IsAlive && TargetSelector.IsAttackable(x))
            };
        }

        private void Spell_OnSpellCast(SDKSpell spell, GameObjectBase target)
        {
            if (spell.SpellSlot == SpellSlot.E)
            {
                Orbwalker.AttackReset();
            }
        }

        internal override void OnCoreMainInput()
        {
            if (SpellQ.ExecuteCastSpell() || SpellE.ExecuteCastSpell() || SpellW.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Aatrox)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("E Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "VeryHigh" });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });
            WSettings.AddItem(new ModeDisplay() { Title = "W HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "Immobile" });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });
        }
    }
}
