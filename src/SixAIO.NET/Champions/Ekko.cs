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
using Oasys.SDK.Tools;
using SixAIO.Enums;
using SixAIO.Models;
using System;
using System.Linq;

namespace SixAIO.Champions
{
    internal sealed class Ekko : Champion
    {
        private GameObjectBase _ekkoRTrailEnd;

        private bool IsEkkoUltValid(GameObjectBase obj) => obj is not null && obj.IsAlive && obj.Name.Contains("Ekko_", StringComparison.OrdinalIgnoreCase) && obj.Name.Contains("_R_TrailEnd", StringComparison.OrdinalIgnoreCase);

        private bool IsEkkoUltReady => IsEkkoUltValid(_ekkoRTrailEnd);

        public Ekko()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => QHitChance,
                Range = () => 1050,
                Radius = () => 120,
                Speed = () => 1650,
                IsEnabled = () => UseQ,
                TargetSelect = (mode) => SpellQ.GetTargets(mode).FirstOrDefault()
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Circle,
                MinimumHitChance = () => WHitChance,
                Range = () => 1600,
                Speed = () => 1500,
                Radius = () => 375,
                Delay = () => 2f,
                IsEnabled = () => UseW,
                TargetSelect = (mode) => SpellW.GetTargets(mode).FirstOrDefault()
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                IsEnabled = () => UseE,
                ShouldCast = (mode, target, spellClass, damage) =>
                            DashModeSelected == DashMode.ToMouse &&
                            !TargetSelector.IsAttackable(Orbwalker.TargetHero) &&
                            !TargetSelector.IsInRange(Orbwalker.TargetHero) &&
                            UnitManager.EnemyChampions.Any(x => TargetSelector.IsAttackable(x) && x.DistanceTo(GameEngine.WorldMousePosition) <= UnitManager.MyChampion.TrueAttackRange + 300)
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                IsEnabled = () => UseR && IsEkkoUltReady,
                ShouldCast = (mode, target, spellClass, damage) => UnitManager.EnemyChampions.Count(x => x.IsAlive && x.IsTargetable && x.DistanceTo(_ekkoRTrailEnd.Position) < REnemiesCloserThan) > RIfMoreThanEnemiesNear,
            };
        }

        //internal override void OnCoreRender()
        //{
        //    if (IsEkkoUltReady && _ekkoRTrailEnd.W2S.IsValid())
        //    {
        //        RenderFactory.DrawText(_ekkoRTrailEnd.Name, 18, _ekkoRTrailEnd.W2S, SharpDX.Color.AliceBlue);
        //    }
        //}

        internal override void OnCoreMainTick()
        {
            if (!IsEkkoUltReady)
            {
                _ekkoRTrailEnd = UnitManager.AllNativeObjects.FirstOrDefault(IsEkkoUltValid);
            }
        }

        internal override void OnCoreMainInput()
        {
            if (SpellE.ExecuteCastSpell() || SpellQ.ExecuteCastSpell() || SpellW.ExecuteCastSpell() || SpellR.ExecuteCastSpell())
            {
                return;
            }
        }
        private DashMode DashModeSelected
        {
            get => (DashMode)Enum.Parse(typeof(DashMode), ESettings.GetItem<ModeDisplay>("Dash Mode").SelectedModeName);
            set => ESettings.GetItem<ModeDisplay>("Dash Mode").SelectedModeName = value.ToString();
        }

        private int RIfMoreThanEnemiesNear
        {
            get => RSettings.GetItem<Counter>("R If More Than Enemies Near Clone").Value;
            set => RSettings.GetItem<Counter>("R If More Than Enemies Near Clone").Value = value;
        }

        private int REnemiesCloserThan
        {
            get => RSettings.GetItem<Counter>("R Enemies Closer To Clone Than").Value;
            set => RSettings.GetItem<Counter>("R Enemies Closer To Clone Than").Value = value;
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Ekko)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });
            WSettings.AddItem(new ModeDisplay() { Title = "W HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });
            ESettings.AddItem(new ModeDisplay() { Title = "Dash Mode", ModeNames = DashHelper.ConstructDashModeTable(), SelectedModeName = "ToMouse" });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            RSettings.AddItem(new Counter() { Title = "R If More Than Enemies Near Clone", MinValue = 0, MaxValue = 5, Value = 0, ValueFrequency = 1 });
            RSettings.AddItem(new Counter() { Title = "R Enemies Closer To Clone Than", MinValue = 50, MaxValue = 400, Value = 150, ValueFrequency = 25 });
        }
    }
}
