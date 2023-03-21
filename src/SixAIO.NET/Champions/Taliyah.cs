using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Extensions;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.Common.Tools;
using Oasys.Common.Tools.Devices;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SharpDX;
using SixAIO.Extensions;
using SixAIO.Models;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Switch = Oasys.Common.Menu.ItemComponents.Switch;

namespace SixAIO.Champions
{
    internal sealed class Taliyah : Champion
    {
        public Taliyah()
        {
            SDKSpell.OnSpellCast += SDKSpell_OnSpellCast;
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                ShouldDraw = () => DrawQRange,
                DrawColor = () => DrawQColor,
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
                ShouldDraw = () => DrawWRange,
                DrawColor = () => DrawWColor,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Circle,
                MinimumHitChance = () => WHitChance,
                Delay = () => 1f,
                Range = () => 900,
                Radius = () => 200,
                Speed = () => 5000,
                IsEnabled = () => UseW,
                MinimumMana = () => WMinMana,
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                ShouldDraw = () => DrawERange,
                DrawColor = () => DrawEColor,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => EHitChance,
                Range = () => 900,
                Radius = () => 450f,
                Speed = () => 2000,
                IsEnabled = () => UseE,
                TargetSelect = (mode) => SpellE.GetTargets(mode).FirstOrDefault()
            };
        }

        private void SDKSpell_OnSpellCast(SDKSpell spell, Oasys.Common.GameObject.GameObjectBase arg2)
        {
            if (spell.SpellSlot == SpellSlot.E)
            {
                CastW();
            }
            if (spell.SpellSlot == SpellSlot.W)
            {
                SpellQ.ExecuteCastSpell();
            }
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
            if (UseWOnlyInComboWithE)
            {
                if (SpellW.IsSpellReady(SpellW.SpellClass, SpellW.MinimumMana(), SpellW.MinimumCharges()) &&
                    SpellE.IsSpellReady(SpellE.SpellClass, SpellE.MinimumMana(), SpellE.MinimumCharges()))
                {
                    SpellE.ExecuteCastSpell();
                }
            }
            else
            {
                CastW();
                SpellE.ExecuteCastSpell();
            }

            SpellQ.ExecuteCastSpell();
        }

        internal void CastW()
        {
            var target = SpellW.GetTargets(Orbwalker.OrbWalkingModeType.Combo).FirstOrDefault();
            if (target != null)
            {
                var targetPos = target.Position;
                var predictResult = SpellW.GetPrediction(target);
                var castPos = targetPos.Extend(UnitManager.MyChampion.Position + (predictResult.CastPosition - UnitManager.MyChampion.Position).Normalized(), 50).ToW2S();
                var mousePosRestore = Pos.MousePosition;
                MouseAction(castPos, () => Keyboard.SendKeyDown((short)CastSlot.W));
                MouseAction(UnitManager.MyChampion.W2S, () => Keyboard.SendKeyUp((short)CastSlot.W));
                Mouse.SetCursor(mousePosRestore);
            }
        }

        private static bool MouseAction(Vector2 castPos, Action action)
        {
            var clock = Stopwatch.StartNew();
            var inAction = true;
            //parallel calling set cursor to keep the mouse where it should be while calling the callback
            Parallel.Invoke(
            () =>
            {
                action();

                inAction = false;
            },
            () =>
            {
                var current = clock.ElapsedMilliseconds;
                var i = 0;
                do
                {
                    i++;
                    if (clock.ElapsedMilliseconds > current || i % 20 == 0)
                    {
                        Mouse.SetCursor(castPos);
                        current = clock.ElapsedMilliseconds;
                    }
                }
                while (clock.ElapsedMilliseconds <= 50 || inAction);
                //Logger.Log($"SetCursor Called: {i}");
            });
            return inAction;
        }

        internal override void OnCoreLaneClearInput()
        {
            if (UseQLaneclear && SpellQ.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.LaneClear))
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
            QSettings.AddItem(new Switch() { Title = "Use Q Laneclear", IsOn = true });
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


            MenuTab.AddDrawOptions(SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R);
        }
    }
}
