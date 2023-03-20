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
    internal sealed class Akshan : Champion
    {
        public Akshan()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => QHitChance,
                Range = () => 850,
                Radius = () => 120,
                Speed = () => 1500,
                IsEnabled = () => UseQ,
                TargetSelect = (mode) => SpellQ.GetTargets(mode).FirstOrDefault(),
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                AllowCastOnMap = () => AllowRCastOnMinimap,
                IsTargetted = () => true,
                Delay = () => 3f,
                Range = () => 2500f,
                IsEnabled = () => UseR,
                TargetSelect = (mode) => SpellR.GetTargets(mode, x => x.HealthPercent <= RHealthPercent).FirstOrDefault()
            };
        }

        internal override void OnCoreRender()
        {
            if (UnitManager.MyChampion.IsAlive)
            {
                bool drawQ = DrawSettings.GetItem<Switch>("Draw Q Range").IsOn;
                var qColor = Oasys.Common.Tools.ColorConverter.GetColor(DrawSettings.GetItem<ModeDisplay>("Draw Q Color").SelectedModeName);
                float qRange = 850;

                if (drawQ)
                {
                    Oasys.SDK.Rendering.RenderFactory.DrawNativeCircle(UnitManager.MyChampion.Position, qRange, qColor, 3);
                }
            }
        }

        internal override void OnCoreMainInput()
        {
            if (SpellQ.ExecuteCastSpell() || SpellR.ExecuteCastSpell())
            {
                return;
            }
        }

        private int RHealthPercent
        {
            get => RSettings.GetItem<Counter>("Below health percent").Value;
            set => RSettings.GetItem<Counter>("Below health percent").Value = value;
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Akshan)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("R Settings"));
            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            RSettings.AddItem(new Counter() { Title = "Below health percent", MinValue = 0, MaxValue = 100, Value = 40, ValueFrequency = 5 }); 
            
            MenuTab.AddGroup(new Group("Draw Settings"));
            DrawSettings.AddItem(new Switch() { Title = "Draw Q Range", IsOn = true });
            DrawSettings.AddItem(new ModeDisplay() { Title = "Draw Q Color", ModeNames = Oasys.Common.Tools.ColorConverter.GetColors(), SelectedModeName = "Blue" });
        }
    }
}
