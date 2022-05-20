using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Extensions;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.Common.Tools.Devices;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SixAIO.Models;
using System;
using System.Linq;

namespace SixAIO.Champions
{
    internal class Taliyah : Champion
    {
        public Taliyah()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                AllowCollision = (target, collisions) => target.IsObject(ObjectTypeFlag.AIMinionClient)
                                                        ? QAllowLaneclearMinionCollision
                                                        : !collisions.Any(),
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => QHitChance,
                Range = () => 1000,
                Radius = () => 120,
                Speed = () => 2000,
                IsEnabled = () => UseQ,
                MinimumMana = () => QMinMana,
                TargetSelect = (mode) => SpellQ.GetTargets(mode).FirstOrDefault()
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Circle,
                MinimumHitChance = () => WHitChance,
                Delay = () => 0.55f,
                Range = () => 900,
                Radius = () => 225,
                Speed = () => 1500,
                IsEnabled = () => UseW,
                MinimumMana = () => WMinMana,
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Cone,
                MinimumHitChance = () => EHitChance,
                Range = () => 800,
                Radius = () => 80f,
                Speed = () => 2000,
                IsEnabled = () => UseE,
                TargetSelect = (mode) => SpellE.GetTargets(mode).FirstOrDefault()
            };
        }

        internal override void OnCoreMainInput()
        {
            if (UseWOnlyInComboWithE)
            {
                if (SpellW.IsSpellReady(SpellW.SpellClass, SpellW.MinimumMana(), SpellW.MinimumCharges()) &&
                    SpellE.IsSpellReady(SpellE.SpellClass, SpellE.MinimumMana(), SpellE.MinimumCharges()))
                {
                    SpellE.ExecuteCastSpell();
                    CastW();
                    SpellQ.ExecuteCastSpell();
                }
            }
            else if (SpellW.IsSpellReady(SpellW.SpellClass, SpellW.MinimumMana(), SpellW.MinimumCharges()))
            {
                CastW();
            }

            if (SpellQ.ExecuteCastSpell() || SpellE.ExecuteCastSpell())
            {
                return;
            }
        }

        internal void CastW()
        {
            var target = SpellW.GetTargets(Orbwalker.OrbWalkingModeType.Combo).FirstOrDefault();
            if (target != null)
            {
                var targetPos = target.Position;
                var predictResult = SpellW.GetPrediction(target);
                var castPos = targetPos.Extend(targetPos + (predictResult.CastPosition - targetPos).Normalized(), 50).ToW2S();

                Mouse.ClickAndBounce((int)castPos.X, (int)castPos.Y, 0, false, () => Keyboard.SendKeyDown((short)SpellW.CastSlot));
                var secondCast = UnitManager.MyChampion.W2S;
                Mouse.ClickAndBounce((int)secondCast.X, (int)secondCast.Y, 0, false, () => Keyboard.SendKeyUp((short)SpellW.CastSlot));
            }
        }

        internal override void OnCoreHarassInput()
        {
            if (SpellQ.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.Mixed))
            {
                return;
            }
        }

        internal override void OnCoreLaneClearInput()
        {
            if (SpellQ.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.LaneClear))
            {
                return;
            }
        }

        internal override void OnCoreLastHitInput()
        {
            if (SpellQ.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.LastHit))
            {
                return;
            }
        }

        internal bool QAllowLaneclearMinionCollision
        {
            get => QSettings.GetItem<Switch>("Q Allow Laneclear minion collision").IsOn;
            set => QSettings.GetItem<Switch>("Q Allow Laneclear minion collision").IsOn = value;
        }

        internal bool UseWOnlyInComboWithE
        {
            get => WSettings.GetItem<Switch>("Use W only in combo with E").IsOn;
            set => WSettings.GetItem<Switch>("Use W only in combo with E").IsOn = value;
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Taliyah)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("E Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Q Allow Laneclear minion collision", IsOn = true });
            QSettings.AddItem(new Counter() { Title = "Q Min Mana", MinValue = 0, MaxValue = 500, Value = 40, ValueFrequency = 10 });
            QSettings.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });
            WSettings.AddItem(new Switch() { Title = "Use W only in combo with E", IsOn = true });
            WSettings.AddItem(new Counter() { Title = "W Min Mana", MinValue = 0, MaxValue = 500, Value = 80, ValueFrequency = 10 });
            WSettings.AddItem(new ModeDisplay() { Title = "W HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });
            ESettings.AddItem(new Counter() { Title = "E Min Mana", MinValue = 0, MaxValue = 500, Value = 150, ValueFrequency = 10 });
            ESettings.AddItem(new ModeDisplay() { Title = "E HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });
        }
    }
}
