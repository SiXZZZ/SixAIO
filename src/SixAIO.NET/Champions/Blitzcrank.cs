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
    internal class Blitzcrank : Champion
    {
        public Blitzcrank()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                AllowCollision = (target, collisions) => !collisions.Any(),
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => QHitChance,
                MinimumMana = () => 100f,
                Range = () => 1100,
                Radius = () => 140,
                Speed = () => 1800,
                IsEnabled = () => UseQ,
                TargetSelect = (mode) => SpellQ.GetTargets(mode, x => !TargetSelector.IsInvulnerable(x, Oasys.Common.Logic.DamageType.Magical, false)).FirstOrDefault(),
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                IsEnabled = () => UseE,
                MinimumMana = () => 25f,
                ShouldCast = (mode, target, spellClass, damage) => TargetSelector.IsAttackable(Orbwalker.TargetHero) && TargetSelector.IsInRange(Orbwalker.TargetHero),
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                IsEnabled = () => UseR,
                MinimumMana = () => RMinMana,
                ShouldCast = (mode, target, spellClass, damage) => UnitManager.EnemyChampions.Count(x => TargetSelector.IsAttackable(x) && x.Distance < REnemiesCloserThan) > RIfMoreThanEnemiesNear,
            };
        }

        internal override void OnCoreMainInput()
        {
            if (SpellQ.ExecuteCastSpell() || SpellE.ExecuteCastSpell() || SpellR.ExecuteCastSpell())
            {
                return;
            }
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
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Blitzcrank)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            RSettings.AddItem(new Counter() { Title = "R Min Mana", MinValue = 0, MaxValue = 500, Value = 150, ValueFrequency = 10 });
            RSettings.AddItem(new Counter() { Title = "R If More Than Enemies Near", MinValue = 0, MaxValue = 5, Value = 1, ValueFrequency = 1 });
            RSettings.AddItem(new Counter() { Title = "R Enemies Closer Than", MinValue = 50, MaxValue = 600, Value = 600, ValueFrequency = 50 });
        }
    }
}
