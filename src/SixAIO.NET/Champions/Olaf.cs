using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Extensions;
using Oasys.Common.GameObject;
using Oasys.Common.GameObject.Clients;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.Rendering;
using Oasys.SDK.SpellCasting;
using SixAIO.Extensions;
using SixAIO.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SixAIO.Champions
{
    internal sealed class Olaf : Champion
    {
        private static List<GameObjectBase> _axes = new();
        private static IEnumerable<GameObjectBase> Axes() => _axes.Where(x => x.Distance <= 2000).Where(IsAxe);

        private static bool IsAxe(GameObjectBase x)
        {
            return x.Name.StartsWith("Olaf", StringComparison.OrdinalIgnoreCase) && x.Name.Contains("_Q_Axe_Ally", StringComparison.OrdinalIgnoreCase);
        }

        public Olaf()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                ShouldDraw = () => DrawQRange,
                DrawColor = () => DrawQColor,
                CastPosition = (castPos) => SpellQ.From().ToW2S().Extend(castPos, SpellQ.From().ToW2S().Distance(castPos) + QExtraRange),
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => QHitChance,
                Range = () => 975,
                Radius = () => 180,
                Speed = () => 1600,
                IsEnabled = () => UseQ,
                TargetSelect = (mode) => SpellQ.GetTargets(mode).FirstOrDefault()
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                IsEnabled = () => UseW,
                ShouldCast = (mode, target, spellClass, damage) => UnitManager.MyChampion.HealthPercent <= WBelowHPPercent,
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                IsTargetted = () => true,
                IsEnabled = () => UseE,
                TargetSelect = (mode) => Orbwalker.TargetHero
            };
        }

        internal override void OnCoreRender()
        {
            SpellQ.DrawRange();
            
            try
            {
                if (DrawAxes)
                {
                    foreach (var item in Axes())
                    {
                        try
                        {
                            var color = Oasys.Common.Tools.ColorConverter.GetColor(DrawAxesColor, 255);
                            RenderFactory.DrawNativeCircle(item.Position, 120, color, 2);
                            //RenderFactory.DrawText(item.Name, 18, item.W2S, Color.White);
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        internal override void OnCoreMainInput()
        {
            if (SpellE.ExecuteCastSpell() || SpellQ.ExecuteCastSpell() || SpellW.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void OnCreateObject(AIBaseClient obj)
        {
            if (IsAxe(obj))
            {
                _axes.Add(obj);
            }
        }

        internal override void OnDeleteObject(AIBaseClient obj)
        {
            _axes.Remove(obj);
        }

        internal override void OnCoreMainTick()
        {
            foreach (var axe in _axes)
            {
                if (!IsAxe(axe))
                {
                    _axes.Remove(axe);
                }
            }
        }

        private int QExtraRange
        {
            get => QSettings.GetItem<Counter>("Q Extra Range").Value;
            set => QSettings.GetItem<Counter>("Q Extra Range").Value = value;
        }

        private bool DrawAxes
        {
            get => QSettings.GetItem<Switch>("Draw Axes").IsOn;
            set => QSettings.GetItem<Switch>("Draw Axes").IsOn = value;
        }

        private string DrawAxesColor
        {
            get => QSettings.GetItem<ModeDisplay>("Draw Axes Color").SelectedModeName;
            set => QSettings.GetItem<ModeDisplay>("Draw Axes Color").SelectedModeName = value;
        }

        private int WBelowHPPercent
        {
            get => WSettings.GetItem<Counter>("W Below HP Percent").Value;
            set => WSettings.GetItem<Counter>("W Below HP Percent").Value = value;
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Olaf)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("E Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Draw Axes", IsOn = true });
            QSettings.AddItem(new ModeDisplay() { Title = "Draw Axes Color", ModeNames = Oasys.Common.Tools.ColorConverter.GetColors(), SelectedModeName = "Blue" });
            QSettings.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });
            QSettings.AddItem(new Counter() { Title = "Q Extra Range", MinValue = 0, MaxValue = 500, Value = 50, ValueFrequency = 25 });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });
            WSettings.AddItem(new Counter() { Title = "W Below HP Percent", MinValue = 0, MaxValue = 100, Value = 30, ValueFrequency = 5 });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });


            MenuTab.AddDrawOptions(SpellSlot.Q);
        }
    }
}
