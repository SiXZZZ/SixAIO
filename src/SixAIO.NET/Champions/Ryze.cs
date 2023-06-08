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
    internal sealed class Ryze : Champion
    {
        public Ryze()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                ShouldDraw = () => DrawQRange,
                DrawColor = () => DrawQColor,
                AllowCollision = (target, collisions) => target.IsObject(ObjectTypeFlag.AIMinionClient)
                                                        ? QAllowLaneclearMinionCollision
                                                        : !collisions.Any(),
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => QHitChance,
                Range = () => 975,
                Radius = () => 120,
                Speed = () => 1700,
                IsEnabled = () => UseQ,
                MinimumMana = () => QMinMana,
                TargetSelect = (mode) => SpellQ.GetTargets(mode, x => !OnlyQonTargetswithE || HasRyzeEBuffActive(x)).OrderByDescending(HasRyzeEBuffActive).FirstOrDefault()
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                ShouldDraw = () => DrawWRange,
                DrawColor = () => DrawWColor,
                IsTargetted = () => true,
                Range = () => 550,
                IsEnabled = () => UseW,
                TargetSelect = (mode) => SpellW.GetTargets(mode, x => !OnlyWonTargetswithE || HasRyzeEBuffActive(x)).OrderByDescending(HasRyzeEBuffActive).FirstOrDefault()
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                ShouldDraw = () => DrawERange,
                DrawColor = () => DrawEColor,
                IsTargetted = () => true,
                Range = () => 550,
                IsEnabled = () => UseE,
                TargetSelect = (mode) => SpellE.GetTargets(mode).OrderByDescending(HasRyzeEBuffActive).FirstOrDefault()
            };
        }

        private static bool HasRyzeEBuffActive(GameObjectBase target)
        {
            return target.BuffManager.ActiveBuffs.Any(buff => buff.Stacks >= 1 && buff.Name == "RyzeE");
        }

        internal override void OnCoreRender()
        {
            SpellQ.DrawRange();
            SpellW.DrawRange();
            SpellE.DrawRange();
        }

        internal override void OnCoreMainInput()
        {
            SpellE.ExecuteCastSpell();
            SpellW.ExecuteCastSpell();
            SpellQ.ExecuteCastSpell();
        }

        internal override void OnCoreLaneClearInput()
        {
            if (UseELaneclear && SpellE.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.LaneClear))
            {
                return;
            }
            if (UseQLaneclear && SpellQ.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.LaneClear))
            {
                return;
            }
        }

        internal bool QAllowLaneclearMinionCollision => QSettings.GetItem<Switch>("Q Allow Laneclear minion collision").IsOn;

        internal bool OnlyQonTargetswithE => QSettings.GetItem<Switch>("Only Q on Targets with E").IsOn;

        internal bool OnlyWonTargetswithE => WSettings.GetItem<Switch>("Only W on Targets with E").IsOn;

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Ryze)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("E Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Use Q Laneclear", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Q Allow Laneclear minion collision", IsOn = true });
            QSettings.AddItem(new Counter() { Title = "Q Min Mana", MinValue = 0, MaxValue = 500, Value = 40, ValueFrequency = 10 });
            QSettings.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });
            QSettings.AddItem(new Switch() { Title = "Only Q on Targets with E", IsOn = false });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });
            WSettings.AddItem(new Counter() { Title = "W Min Mana", MinValue = 0, MaxValue = 500, Value = 80, ValueFrequency = 10 });
            WSettings.AddItem(new Switch() { Title = "Only W on Targets with E", IsOn = false });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });
            ESettings.AddItem(new Switch() { Title = "Use E Laneclear", IsOn = true });
            ESettings.AddItem(new Counter() { Title = "E Min Mana", MinValue = 0, MaxValue = 500, Value = 150, ValueFrequency = 10 });


            MenuTab.AddDrawOptions(SpellSlot.Q, SpellSlot.W, SpellSlot.E);


        }
    }
}
