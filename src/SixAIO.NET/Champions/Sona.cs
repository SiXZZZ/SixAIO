using Oasys.Common.Enums.GameEnums;
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
    internal sealed class Sona : Champion
    {
        public Sona()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                ShouldDraw = () => DrawQRange,
                DrawColor = () => DrawQColor,
                Delay = () => 0f,
                IsEnabled = () => UseQ,
                Range = () => 800,
                ShouldCast = (mode, target, spellClass, damage) => UnitManager.EnemyChampions.Any(x => x.Distance <= 800 && x.IsAlive && TargetSelector.IsAttackable(x)),
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                ShouldDraw = () => DrawWRange,
                DrawColor = () => DrawWColor,
                Delay = () => 0f,
                IsEnabled = () => UseW,
                MinimumMana = () => WMinMana,
                Range = () => 800,
                ShouldCast = (mode, target, spellClass, damage) => UnitManager.AllyChampions.Any(x => x.Distance <= 800 && x.IsAlive && x.HealthPercent < WBuffHealthPercent),
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                ShouldDraw = () => DrawERange,
                DrawColor = () => DrawEColor,
                Delay = () => 0f,
                IsEnabled = () => UseE,
                Range = () => 400,
                ShouldCast = (mode, target, spellClass, damage) => UnitManager.AllyChampions.Any(x => x.Distance <= 400 && x.IsAlive),
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                ShouldDraw = () => DrawRRange,
                DrawColor = () => DrawRColor,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => RHitChance,
                Range = () => 1000,
                Radius = () => 280,
                Speed = () => 2400,
                IsEnabled = () => UseR,
                TargetSelect = (mode) => SpellR.GetTargets(mode, x => !TargetSelector.IsInvulnerable(x, Oasys.Common.Logic.DamageType.Magical, false)).FirstOrDefault()
            };
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
            if (SpellQ.ExecuteCastSpell() || SpellW.ExecuteCastSpell() || SpellE.ExecuteCastSpell() || SpellR.ExecuteCastSpell())
            {
                return;
            }
        }

        private int WBuffHealthPercent
        {
            get => WSettings.GetItem<Counter>("W Buff Health Percent").Value;
            set => WSettings.GetItem<Counter>("W Buff Health Percent").Value = value;
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Sona)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });
            WSettings.AddItem(new Counter() { Title = "W Min Mana", MinValue = 0, MaxValue = 500, Value = 100, ValueFrequency = 10 });
            WSettings.AddItem(new Counter() { Title = "W Buff Health Percent", MinValue = 0, MaxValue = 100, Value = 50, ValueFrequency = 5 });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            RSettings.AddItem(new ModeDisplay() { Title = "R HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });


            MenuTab.AddDrawOptions(SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R);

        }
    }
}
