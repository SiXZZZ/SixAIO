using Oasys.Common;
using Oasys.Common.Enums.GameEnums;
using Oasys.Common.Extensions;
using Oasys.Common.GameObject;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SixAIO.Enums;
using SixAIO.Extensions;
using SixAIO.Models;
using System;
using System.Linq;

namespace SixAIO.Champions
{
    internal sealed class Graves : Champion
    {
        private float _lastAATime;

        public Graves()
        {
            Orbwalker.OnOrbwalkerAfterBasicAttack += Orbwalker_OnOrbwalkerAfterBasicAttack;
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                ShouldDraw = () => DrawQRange,
                DrawColor = () => DrawQColor,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => QHitChance,
                Range = () => 800,
                Radius = () => 80,
                Speed = () => 2000,
                IsEnabled = () => UseQ,
                TargetSelect = (mode) => QOnlyOnWallBang
                                        ? SpellQ.GetTargets(mode, x => mode != Orbwalker.OrbWalkingModeType.LaneClear || x.IsJungle && CanWallBang(x)).OrderBy(x => x.Health).FirstOrDefault()
                                        : SpellQ.GetTargets(mode, x => mode != Orbwalker.OrbWalkingModeType.LaneClear || x.IsJungle).OrderBy(x => x.Health).ThenBy(CanWallBang).FirstOrDefault()
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                ShouldDraw = () => DrawWRange,
                DrawColor = () => DrawWColor,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Circle,
                MinimumHitChance = () => WHitChance,
                Range = () => WMaximumRange,
                Speed = () => 1500,
                Radius = () => 200,
                IsEnabled = () => UseW,
                TargetSelect = (mode) => SpellW.GetTargets(mode).FirstOrDefault()
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                IsEnabled = () => UseE,
                ShouldCast = (mode, target, spellClass, damage) =>
                            DashModeSelected == DashMode.ToMouse &&
                            !Orbwalker.CanBasicAttack &&
                            !UnitManager.MyChampion.BuffManager.ActiveBuffs.Any(x => x.Name == "gravesbasicattackammo2" && x.Stacks >= 1) &&
                            (mode != Orbwalker.OrbWalkingModeType.Combo || TargetSelector.IsAttackable(Orbwalker.TargetHero) && TargetSelector.IsInRange(Orbwalker.TargetHero)) &&
                            (mode != Orbwalker.OrbWalkingModeType.LaneClear || UnitManager.EnemyJungleMobs.Any(x => TargetSelector.IsInRange(x) && TargetSelector.IsAttackable(x))),
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                ShouldDraw = () => DrawRRange,
                DrawColor = () => DrawRColor,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => RHitChance,
                Range = () => RMaximumRange,
                Radius = () => 200,
                Speed = () => 2100,
                IsEnabled = () => UseR,
                TargetSelect = (mode) => SpellR.GetTargets(mode, x => x.Distance > 1100
                                                    ? GetRDamage(x) * 0.8f >= x.Health
                                                    : GetRDamage(x) >= x.Health)
                                                .FirstOrDefault()
            };
        }

        private float GetRDamage(GameObjectBase target)
        {
            if (target is null)
            {
                return 0;
            }

            return target != null
                            ? DamageCalculator.GetArmorMod(UnitManager.MyChampion, target) *
                                ((100 + SpellR.SpellClass.Level * 150) + UnitManager.MyChampion.UnitStats.BonusAttackDamage * 1.5f)
                            : 0;
        }

        private int DistanceToWall(GameObjectBase target)
        {
            if (target == null)
            {
                return int.MaxValue;
            }

            for (var i = 0; i < 800; i += 5)
            {
                var pos = UnitManager.MyChampion.Position.Extend(target.Position, target.Distance + i);
                if (pos.IsValid() && EngineManager.IsWall(pos))
                {
                    return i;
                }
            }

            return int.MaxValue;
        }

        private bool CanWallBang(GameObjectBase target)
        {
            if (target == null)
            {
                return false;
            }
            var distance = DistanceToWall(target);
            return distance <= 800 && distance > 0;
        }

        private void Orbwalker_OnOrbwalkerAfterBasicAttack(float gameTime, GameObjectBase target)
        {
            _lastAATime = gameTime;
            if (target != null)
            {
                SpellE.ExecuteCastSpell();
            }
        }

        internal override void OnCoreRender()
        {
            SpellQ.DrawRange();
            SpellW.DrawRange();
            SpellR.DrawRange();
        }

        internal override void OnCoreMainInput()
        {
            if (SpellW.ExecuteCastSpell() || SpellQ.ExecuteCastSpell() || SpellR.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void OnCoreLaneClearInput()
        {
            if (UseQLaneclear && SpellQ.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.LaneClear))
            {
                return;
            }
            if (UseELaneclear && SpellE.ExecuteCastSpell(Orbwalker.OrbWalkingModeType.LaneClear))
            {
                return;
            }
        }

        internal bool QOnlyOnWallBang
        {
            get => QSettings.GetItem<Switch>("Q Only on wall bang").IsOn;
            set => QSettings.GetItem<Switch>("Q Only on wall bang").IsOn = value;
        }

        private DashMode DashModeSelected
        {
            get => (DashMode)Enum.Parse(typeof(DashMode), ESettings.GetItem<ModeDisplay>("Dash Mode").SelectedModeName);
            set => ESettings.GetItem<ModeDisplay>("Dash Mode").SelectedModeName = value.ToString();
        }

        private int WMaximumRange
        {
            get => WSettings.GetItem<Counter>("W maximum range").Value;
            set => WSettings.GetItem<Counter>("W maximum range").Value = value;
        }

        private int RMaximumRange
        {
            get => RSettings.GetItem<Counter>("R maximum range").Value;
            set => RSettings.GetItem<Counter>("R maximum range").Value = value;
        }

        internal bool ROnlyWhenCanKill
        {
            get => RSettings.GetItem<Switch>("R only when can kill").IsOn;
            set => RSettings.GetItem<Switch>("R only when can kill").IsOn = value;
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Graves)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Use Q Laneclear", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Q Only on wall bang", IsOn = false });
            QSettings.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });
            WSettings.AddItem(new ModeDisplay() { Title = "W HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });
            WSettings.AddItem(new Counter() { Title = "W maximum range", MinValue = 0, MaxValue = 950, Value = 950, ValueFrequency = 50 });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });
            ESettings.AddItem(new ModeDisplay() { Title = "Dash Mode", ModeNames = DashHelper.ConstructDashModeTable(), SelectedModeName = "ToMouse" });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            RSettings.AddItem(new Switch() { Title = "R only when can kill", IsOn = false });
            RSettings.AddItem(new ModeDisplay() { Title = "R HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });
            RSettings.AddItem(new Counter() { Title = "R maximum range", MinValue = 0, MaxValue = 1600, Value = 1600, ValueFrequency = 50 });


            MenuTab.AddDrawOptions(SpellSlot.Q, SpellSlot.W, SpellSlot.R);
        }
    }
}
