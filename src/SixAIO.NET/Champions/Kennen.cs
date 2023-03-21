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
    internal sealed class Kennen : Champion
    {
        public Kennen()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                ShouldDraw = () => DrawQRange,
                DrawColor = () => DrawQColor,
                AllowCollision = (target, collisions) => !collisions.Any(),
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => QHitChance,
                Range = () => 1000,
                Radius = () => 100,
                Speed = () => 1700,
                IsEnabled = () => UseQ,
                Delay = () => 0.175f,
                TargetSelect = (mode) => SpellQ.GetTargets(mode).FirstOrDefault()
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                ShouldDraw = () => DrawWRange,
                DrawColor = () => DrawWColor,
                IsEnabled = () => UseW,
                Range = () => 775,
                ShouldCast = (mode, target, spellClass, damage) =>
                    WIfMoreThanEnemiesNear < UnitManager.EnemyChampions.Count(x =>
                        TargetSelector.IsAttackable(x) && x.Distance <= 775 && x.IsAlive && x.BuffManager.ActiveBuffs.Any(buff =>
                            buff.IsActive && buff.Stacks >= WMinimumStacks && buff.Name == "kennenmarkofstorm"))
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                IsEnabled = () => UseE,
                ShouldCast = (mode, target, spellClass, damage) => spellClass.SpellData.SpellName == "KennenLRCancel" &&
                UnitManager.MyChampion.AttackSpeed >= ECancelAboveAttackSpeed &&
                UnitManager.EnemyChampions.Any(enemy => TargetSelector.IsAttackable(enemy) && enemy.Distance <= ECancelRange)
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                ShouldDraw = () => DrawRRange,
                DrawColor = () => DrawRColor,
                IsEnabled = () => UseR,
                Range = () => REnemiesCloserThan,
                ShouldCast = (mode, target, spellClass, damage) => UnitManager.EnemyChampions.Count(x => TargetSelector.IsAttackable(x) && x.Distance < REnemiesCloserThan) > RIfMoreThanEnemiesNear,
            };
        }

        internal override void OnCoreRender()
        {
            SpellQ.DrawRange();
            SpellW.DrawRange();
            SpellR.DrawRange();
        }

        internal override void OnCoreMainInput()
        {
            SpellE.ExecuteCastSpell();

            if (SpellQ.ExecuteCastSpell() || SpellW.ExecuteCastSpell() || SpellR.ExecuteCastSpell())
            {
                return;
            }
        }

        private int WIfMoreThanEnemiesNear
        {
            get => WSettings.GetItem<Counter>("W If More Than Enemies Near").Value;
            set => WSettings.GetItem<Counter>("W If More Than Enemies Near").Value = value;
        }

        private int WMinimumStacks
        {
            get => WSettings.GetItem<Counter>("W Minimum Stacks").Value;
            set => WSettings.GetItem<Counter>("W Minimum Stacks").Value = value;
        }

        private float ECancelAboveAttackSpeed
        {
            get => ESettings.GetItem<FloatCounter>("E Cancel Above Attack Speed").Value;
            set => ESettings.GetItem<FloatCounter>("E Cancel Above Attack Speed").Value = value;
        }

        private int ECancelRange
        {
            get => ESettings.GetItem<Counter>("E Cancel Range").Value;
            set => ESettings.GetItem<Counter>("E Cancel Range").Value = value;
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
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Kennen)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });
            WSettings.AddItem(new Counter() { Title = "W If More Than Enemies Near", MinValue = 0, MaxValue = 5, Value = 1, ValueFrequency = 1 });
            WSettings.AddItem(new Counter() { Title = "W Minimum Stacks", MinValue = 0, MaxValue = 2, Value = 1, ValueFrequency = 1 });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });
            ESettings.AddItem(new FloatCounter() { Title = "E Cancel Above Attack Speed", MinValue = 0.5f, MaxValue = 5.0f, Value = 1.5f, ValueFrequency = 0.1f });
            ESettings.AddItem(new Counter() { Title = "E Cancel Range", MinValue = 100, MaxValue = 1000, Value = 550, ValueFrequency = 50 });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            RSettings.AddItem(new Counter() { Title = "R If More Than Enemies Near", MinValue = 0, MaxValue = 5, Value = 1, ValueFrequency = 1 });
            RSettings.AddItem(new Counter() { Title = "R Enemies Closer Than", MinValue = 50, MaxValue = 550, Value = 550, ValueFrequency = 50 });


            MenuTab.AddDrawOptions(SpellSlot.Q, SpellSlot.W, SpellSlot.R);
        }
    }
}
