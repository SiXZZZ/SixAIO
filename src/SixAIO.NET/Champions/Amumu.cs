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
    internal sealed class Amumu : Champion
    {
        private static bool IsWActive()
        {
            var buff = UnitManager.MyChampion.BuffManager.GetBuffByName("AuraofDespair", false, true);
            return buff != null && buff.Stacks >= 1;
        }

        public Amumu()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                AllowCollision = (target, collisions) => !collisions.Any(),
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => QHitChance,
                Range = () => 1100,
                Radius = () => 160,
                Speed = () => 2000,
                IsEnabled = () => UseQ,
                MinimumMana = () => QMinMana,
                MinimumCharges = () => 1,
                TargetSelect = (mode) => SpellQ.GetTargets(mode).FirstOrDefault()
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                IsEnabled = () => UseW,
                MinimumMana = () => WMinMana,
                ShouldCast = (mode, target, spellClass, damage) =>
                {
                    var enemyIsNear = UnitManager.EnemyChampions.Any(x => TargetSelector.IsAttackable(x) && x.Distance <= 350 && x.IsAlive);
                    return IsWActive()
                    ? !enemyIsNear
                    : enemyIsNear;
                },
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                IsEnabled = () => UseE,
                ShouldCast = (mode, target, spellClass, damage) => UnitManager.EnemyChampions.Any(x => TargetSelector.IsAttackable(x) && x.Distance <= 350 && x.IsAlive),
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                IsEnabled = () => UseR,
                MinimumMana = () => RMinMana,
                ShouldCast = (mode, target, spellClass, damage) => UnitManager.EnemyChampions.Count(x => TargetSelector.IsAttackable(x) && x.Distance < REnemiesCloserThan) > RIfMoreThanEnemiesNear,
            };
        }

        internal override void OnCoreRender()
        {
            if (UnitManager.MyChampion.IsAlive)
            {
                bool drawQ = DrawSettings.GetItem<Switch>("Draw Q Range").IsOn;
                var qColor = Oasys.Common.Tools.ColorConverter.GetColor(DrawSettings.GetItem<ModeDisplay>("Draw Q Color").SelectedModeName);
                float qRange = 1100;

                if (drawQ)
                {
                    Oasys.SDK.Rendering.RenderFactory.DrawNativeCircle(UnitManager.MyChampion.Position, qRange, qColor, 3);
                }

                bool drawW = DrawSettings.GetItem<Switch>("Draw W Range").IsOn;
                var wColor = Oasys.Common.Tools.ColorConverter.GetColor(DrawSettings.GetItem<ModeDisplay>("Draw W Color").SelectedModeName);
                float wRange = 350;

                if (drawW)
                {
                    Oasys.SDK.Rendering.RenderFactory.DrawNativeCircle(UnitManager.MyChampion.Position, wRange, wColor, 3);
                }

                bool drawE = DrawSettings.GetItem<Switch>("Draw E Range").IsOn;
                var eColor = Oasys.Common.Tools.ColorConverter.GetColor(DrawSettings.GetItem<ModeDisplay>("Draw E Color").SelectedModeName);
                float eRange = 350;

                if (drawE)
                {
                    Oasys.SDK.Rendering.RenderFactory.DrawNativeCircle(UnitManager.MyChampion.Position, eRange, eColor, 3);
                }

                bool drawR = DrawSettings.GetItem<Switch>("Draw R Range").IsOn;
                var rColor = Oasys.Common.Tools.ColorConverter.GetColor(DrawSettings.GetItem<ModeDisplay>("Draw R Color").SelectedModeName);
                float rRange = 550;

                if (drawR)
                {
                    Oasys.SDK.Rendering.RenderFactory.DrawNativeCircle(UnitManager.MyChampion.Position, rRange, rColor, 3);
                }
            }
        }

        internal override void OnCoreMainInput()
        {
            if (SpellE.ExecuteCastSpell() || SpellQ.ExecuteCastSpell() || SpellW.ExecuteCastSpell() || SpellR.ExecuteCastSpell())
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
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Amumu)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new Counter() { Title = "Q Min Mana", MinValue = 0, MaxValue = 500, Value = 150, ValueFrequency = 10 });
            QSettings.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });
            WSettings.AddItem(new Counter() { Title = "W Min Mana", MinValue = 0, MaxValue = 500, Value = 150, ValueFrequency = 10 });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            RSettings.AddItem(new Counter() { Title = "R Min Mana", MinValue = 0, MaxValue = 500, Value = 150, ValueFrequency = 10 });
            RSettings.AddItem(new Counter() { Title = "R If More Than Enemies Near", MinValue = 0, MaxValue = 5, Value = 2, ValueFrequency = 1 });
            RSettings.AddItem(new Counter() { Title = "R Enemies Closer Than", MinValue = 50, MaxValue = 600, Value = 350, ValueFrequency = 50 });

            MenuTab.AddGroup(new Group("Draw Settings"));
            DrawSettings.AddItem(new Switch() { Title = "Draw Q Range", IsOn = true });
            DrawSettings.AddItem(new ModeDisplay() { Title = "Draw Q Color", ModeNames = Oasys.Common.Tools.ColorConverter.GetColors(), SelectedModeName = "Blue" });
            DrawSettings.AddItem(new Switch() { Title = "Draw W Range", IsOn = true });
            DrawSettings.AddItem(new ModeDisplay() { Title = "Draw W Color", ModeNames = Oasys.Common.Tools.ColorConverter.GetColors(), SelectedModeName = "Orange" });
            DrawSettings.AddItem(new Switch() { Title = "Draw E Range", IsOn = true });
            DrawSettings.AddItem(new ModeDisplay() { Title = "Draw E Color", ModeNames = Oasys.Common.Tools.ColorConverter.GetColors(), SelectedModeName = "Green" });
            DrawSettings.AddItem(new Switch() { Title = "Draw R Range", IsOn = true });
            DrawSettings.AddItem(new ModeDisplay() { Title = "Draw R Color", ModeNames = Oasys.Common.Tools.ColorConverter.GetColors(), SelectedModeName = "White" });
        }
    }
}
