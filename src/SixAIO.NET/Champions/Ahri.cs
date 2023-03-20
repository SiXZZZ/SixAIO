using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SixAIO.Enums;
using SixAIO.Models;
using System;
using System.Linq;

namespace SixAIO.Champions
{
    internal sealed class Ahri : Champion
    {
        public Ahri()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => QHitChance,
                Range = () => 900,
                Radius = () => 200,
                Speed = () => 1550,
                IsEnabled = () => UseQ,
                TargetSelect = (mode) => QPrioTargetsWithCharm
                                            ? SpellQ.GetTargets(mode)
                                                    .OrderByDescending(x => x.BuffManager.ActiveBuffs.Any(buff => buff.IsActive && buff.Name == "AhriSeduce"))
                                                    .FirstOrDefault()
                                            : SpellQ.GetTargets(mode)
                                                    .FirstOrDefault(),
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                IsEnabled = () => UseW,
                ShouldCast = (mode, target, spellClass, damage) => UnitManager.EnemyChampions.Any(x => x.Distance <= 700 && x.IsAlive && TargetSelector.IsAttackable(x)),
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                AllowCollision = (target, collisions) => !collisions.Any(),
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => EHitChance,
                Range = () => 1000,
                Speed = () => 1550,
                Radius = () => 120,
                IsEnabled = () => UseE,
                TargetSelect = (mode) => SpellE.GetTargets(mode, x => !TargetSelector.IsInvulnerable(x, Oasys.Common.Logic.DamageType.Magical, false)).FirstOrDefault()
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                Delay = () => 0f,
                IsEnabled = () => UseR,
                ShouldCast = (mode, target, spellClass, damage) =>
                            DashModeSelected == DashMode.ToMouse &&
                            UnitManager.EnemyChampions.Any(x => x.Distance <= x.TrueAttackRange + 500 && x.IsAlive && TargetSelector.IsAttackable(x)),
            };
        }
        internal override void OnCoreRender()
        {
            if (UnitManager.MyChampion.IsAlive)
            {
                bool drawQ = DrawSettings.GetItem<Switch>("Draw Q Range").IsOn;
                var qColor = Oasys.Common.Tools.ColorConverter.GetColor(DrawSettings.GetItem<ModeDisplay>("Draw Q Color").SelectedModeName);
                float qRange = 900;

                if (drawQ)
                {
                    Oasys.SDK.Rendering.RenderFactory.DrawNativeCircle(UnitManager.MyChampion.Position, qRange, qColor, 3);
                }

                bool drawW = DrawSettings.GetItem<Switch>("Draw W Range").IsOn;
                var wColor = Oasys.Common.Tools.ColorConverter.GetColor(DrawSettings.GetItem<ModeDisplay>("Draw W Color").SelectedModeName);
                float wRange = 700;

                if (drawW)
                {
                    Oasys.SDK.Rendering.RenderFactory.DrawNativeCircle(UnitManager.MyChampion.Position, wRange, wColor, 3);
                }

                bool drawE = DrawSettings.GetItem<Switch>("Draw E Range").IsOn;
                var eColor = Oasys.Common.Tools.ColorConverter.GetColor(DrawSettings.GetItem<ModeDisplay>("Draw E Color").SelectedModeName);
                float eRange = 1000;

                if (drawE)
                {
                    Oasys.SDK.Rendering.RenderFactory.DrawNativeCircle(UnitManager.MyChampion.Position, eRange, eColor, 3);
                }

                bool drawR = DrawSettings.GetItem<Switch>("Draw R Range").IsOn;
                var rColor = Oasys.Common.Tools.ColorConverter.GetColor(DrawSettings.GetItem<ModeDisplay>("Draw R Color").SelectedModeName);
                float rRange = 500;

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

        internal override void OnCoreMainTick()
        {
            if (QPrioTargetsWithCharm)
            {
                Orbwalker.SelectedTarget = UnitManager.EnemyChampions.FirstOrDefault(x => x.BuffManager.ActiveBuffs.Any(buff => buff.IsActive && buff.Name == "AhriSeduce"));
            }
        }

        internal bool QPrioTargetsWithCharm
        {
            get => QSettings.GetItem<Switch>("Q Prio Targets With Charm").IsOn;
            set => QSettings.GetItem<Switch>("Q Prio Targets With Charm").IsOn = value;
        }

        private DashMode DashModeSelected
        {
            get => (DashMode)Enum.Parse(typeof(DashMode), RSettings.GetItem<ModeDisplay>("R Dash Mode").SelectedModeName);
            set => RSettings.GetItem<ModeDisplay>("R Dash Mode").SelectedModeName = value.ToString();
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Ahri)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });
            QSettings.AddItem(new Switch() { Title = "Q Prio Targets With Charm", IsOn = true });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });
            ESettings.AddItem(new ModeDisplay() { Title = "E HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            RSettings.AddItem(new ModeDisplay() { Title = "R Dash Mode", ModeNames = DashHelper.ConstructDashModeTable(), SelectedModeName = "ToMouse" });

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
