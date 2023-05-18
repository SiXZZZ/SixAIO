using Oasys.Common.Enums.GameEnums;
using Oasys.Common.GameObject;
using Oasys.Common.GameObject.Clients.ExtendedInstances.Spells;
using Oasys.Common.GameObject.ObjectClass;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using Oasys.SDK.Tools;
using SixAIO.Extensions;
using SixAIO.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SixAIO.Champions
{
    internal sealed class Aphelios : Champion
    {
        public enum GunType
        {
            ApheliosSeverumQ,
            ApheliosInfernumQ,
            ApheliosCrescendumQ,
            ApheliosCalibrumQ,
            ApheliosGravitumQ,
        }

        private static GunType GetGunType() => UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.Q).SpellData.SpellName switch
        {
            "ApheliosSeverumQ" => GunType.ApheliosSeverumQ,
            "ApheliosInfernumQ" => GunType.ApheliosInfernumQ,
            "ApheliosCrescendumQ" => GunType.ApheliosCrescendumQ,
            "ApheliosCalibrumQ" => GunType.ApheliosCalibrumQ,
            "ApheliosGravitumQ" => GunType.ApheliosGravitumQ,
            _ => GunType.ApheliosSeverumQ
        };

        public Aphelios()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                ShouldDraw = () => DrawQRange,
                DrawColor = () => DrawQColor,
                AllowCollision = QAllowCollision,
                PredictionMode = QPredictionMode,
                MinimumHitChance = GetQHitChance,
                Range = QRange,
                Radius = QRadius,
                Speed = QSpeed,
                IsEnabled = QIsEnabled,
                MinimumMana = QMinimumMana,
                ShouldCast = QShouldCast,
                TargetSelect = QTargetSelect
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                ShouldDraw = () => DrawRRange,
                DrawColor = () => DrawRColor,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => GetRHitChance(),
                Range = () => 1300,
                Radius = () => 200,
                Speed = () => 1000,
                Delay = () => 0.6f,
                IsEnabled = RIsEnabled,
                MinimumMana = () => 100,
                TargetSelect = RTargetSelect
            };
        }

        private GameObjectBase QTargetSelect(Orbwalker.OrbWalkingModeType mode)
        {
            if (mode != Orbwalker.OrbWalkingModeType.Combo)
            {
                return null;
            }

            return GetGunType() switch
            {
                GunType.ApheliosInfernumQ => SpellQ.GetTargets(mode).FirstOrDefault(),
                GunType.ApheliosCrescendumQ => SpellQ.GetTargets(mode).FirstOrDefault(),
                GunType.ApheliosCalibrumQ => SpellQ.GetTargets(mode).FirstOrDefault(),
                _ => null
            };
        }

        private GameObjectBase RTargetSelect(Orbwalker.OrbWalkingModeType mode)
        {
            if (mode != Orbwalker.OrbWalkingModeType.Combo)
            {
                return null;
            }

            var moonlightVigilCanHit = GetGunType() switch
            {
                GunType.ApheliosSeverumQ => RSeverumSettings.GetItem<Counter>("Moonlight Vigil Severum Can Hit").Value,
                GunType.ApheliosInfernumQ => RInfernumSettings.GetItem<Counter>("Moonlight Vigil Infernum Can Hit").Value,
                GunType.ApheliosCrescendumQ => RCrescendumSettings.GetItem<Counter>("Moonlight Vigil Crescendum Can Hit").Value,
                GunType.ApheliosCalibrumQ => RCalibrumSettings.GetItem<Counter>("Moonlight Vigil Calibrum Can Hit").Value,
                GunType.ApheliosGravitumQ => RGravitumSettings.GetItem<Counter>("Moonlight Vigil Gravitum Can Hit").Value,
                _ => 5,
            };

            return SpellR.GetTargets(mode, target => moonlightVigilCanHit <= UnitManager.EnemyChampions.Count(enemy => target.DistanceTo(enemy.Position) <= SpellR.Radius()))
                         .FirstOrDefault();
        }

        private float QMinimumMana()
        {
            return GetGunType() switch
            {
                GunType.ApheliosSeverumQ => 60f,
                GunType.ApheliosInfernumQ => 60f,
                GunType.ApheliosCrescendumQ => 60f,
                GunType.ApheliosCalibrumQ => 60f,
                GunType.ApheliosGravitumQ => 60f,
                _ => 60f
            };
        }

        private bool QIsEnabled()
        {
            return GetGunType() switch
            {
                GunType.ApheliosSeverumQ => UseSeverum && UnitManager.MyChampion.HealthPercent <= SeverumHealthPercentLessThan,
                GunType.ApheliosInfernumQ => UseInfernum,
                GunType.ApheliosCrescendumQ => UseCrescendum,
                GunType.ApheliosCalibrumQ => UseCalibrum,
                GunType.ApheliosGravitumQ => UseGravitum,
                _ => false
            };
        }

        private bool RIsEnabled()
        {
            return GetGunType() switch
            {
                GunType.ApheliosSeverumQ => RSeverumSettings.GetItem<Switch>("Use Moonlight Vigil Severum").IsOn &&
                                            UnitManager.MyChampion.HealthPercent <= RSeverumSettings.GetItem<Counter>("Moonlight Vigil Severum Health Percent Less Than").Value,
                GunType.ApheliosInfernumQ => RInfernumSettings.GetItem<Switch>("Use Moonlight Vigil Infernum").IsOn,
                GunType.ApheliosCrescendumQ => RCrescendumSettings.GetItem<Switch>("Use Moonlight Vigil Crescendum").IsOn,
                GunType.ApheliosCalibrumQ => RCalibrumSettings.GetItem<Switch>("Use Moonlight Vigil Calibrum").IsOn,
                GunType.ApheliosGravitumQ => RGravitumSettings.GetItem<Switch>("Use Moonlight Vigil Gravitum").IsOn,
                _ => false
            };
        }

        private float QSpeed()
        {
            return GetGunType() switch
            {
                GunType.ApheliosSeverumQ => 0f,
                GunType.ApheliosInfernumQ => 1000f,
                GunType.ApheliosCrescendumQ => 0f,
                GunType.ApheliosCalibrumQ => 1850f,
                GunType.ApheliosGravitumQ => 0f,
                _ => 0f
            };
        }

        private float QRadius()
        {
            return GetGunType() switch
            {
                GunType.ApheliosSeverumQ => 0f,
                GunType.ApheliosInfernumQ => 40f,
                GunType.ApheliosCrescendumQ => 475f,
                GunType.ApheliosCalibrumQ => 120f,
                GunType.ApheliosGravitumQ => 0f,
                _ => 0f
            };
        }

        private float QRange()
        {
            return GetGunType() switch
            {
                GunType.ApheliosSeverumQ => 550f,
                GunType.ApheliosInfernumQ => 650f,
                GunType.ApheliosCrescendumQ => 475f,
                GunType.ApheliosCalibrumQ => 1450f,
                GunType.ApheliosGravitumQ => 30_000f,
                _ => 0f
            };
        }

        private Prediction.MenuSelected.HitChance GetQHitChance()
        {
            return GetGunType() switch
            {
                GunType.ApheliosInfernumQ => InfernumHitChance,
                GunType.ApheliosCalibrumQ => CalibrumHitChance,
                _ => Prediction.MenuSelected.HitChance.Low
            };
        }

        private Prediction.MenuSelected.HitChance GetRHitChance()
        {
            var setting = GetGunType() switch
            {
                GunType.ApheliosSeverumQ => RSeverumSettings.GetItem<ModeDisplay>("Moonlight Vigil Severum HitChance").SelectedModeName,
                GunType.ApheliosInfernumQ => RInfernumSettings.GetItem<ModeDisplay>("Moonlight Vigil Infernum HitChance").SelectedModeName,
                GunType.ApheliosCrescendumQ => RCrescendumSettings.GetItem<ModeDisplay>("Moonlight Vigil Crescendum HitChance").SelectedModeName,
                GunType.ApheliosCalibrumQ => RCalibrumSettings.GetItem<ModeDisplay>("Moonlight Vigil Calibrum HitChance").SelectedModeName,
                GunType.ApheliosGravitumQ => RGravitumSettings.GetItem<ModeDisplay>("Moonlight Vigil Gravitum HitChance").SelectedModeName,
                _ => Prediction.MenuSelected.HitChance.Immobile.ToString(),
            };

            return (Prediction.MenuSelected.HitChance)Enum.Parse(typeof(Prediction.MenuSelected.HitChance), setting);
        }

        private Prediction.MenuSelected.PredictionType QPredictionMode()
        {
            return GetGunType() switch
            {
                GunType.ApheliosSeverumQ => Prediction.MenuSelected.PredictionType.Circle,
                GunType.ApheliosInfernumQ => Prediction.MenuSelected.PredictionType.Cone,
                GunType.ApheliosCrescendumQ => Prediction.MenuSelected.PredictionType.Circle,
                GunType.ApheliosCalibrumQ => Prediction.MenuSelected.PredictionType.Line,
                GunType.ApheliosGravitumQ => Prediction.MenuSelected.PredictionType.Circle,
                _ => Prediction.MenuSelected.PredictionType.Circle
            };
        }

        private bool QAllowCollision(GameObjectBase target, IEnumerable<GameObjectBase> collisions)
        {
            return GetGunType() switch
            {
                GunType.ApheliosSeverumQ => true,
                GunType.ApheliosInfernumQ => true,
                GunType.ApheliosCrescendumQ => true,
                GunType.ApheliosCalibrumQ => false,
                GunType.ApheliosGravitumQ => true,
                _ => false
            };
        }

        private bool QShouldCast(Orbwalker.OrbWalkingModeType mode, GameObjectBase target, SpellClass spellClass, float damage)
        {
            if (mode != Orbwalker.OrbWalkingModeType.Combo)
            {
                return false;
            }

            return GetGunType() switch
            {
                GunType.ApheliosSeverumQ => UnitManager.EnemyChampions.Any(enemy => enemy.IsAlive && enemy.Distance <= SpellQ.Range()),
                GunType.ApheliosInfernumQ => target != null,
                GunType.ApheliosCrescendumQ => target != null && target.Distance <= CrescendumEnemyIsCloserThan && spellClass.IsSpellReady,
                GunType.ApheliosCalibrumQ => target != null && (!CalibrumOnlyOutsideOfAttackRange || UnitManager.EnemyChampions.All(x => x.Distance >= UnitManager.MyChampion.TrueAttackRange)) && spellClass.IsSpellReady,
                GunType.ApheliosGravitumQ => UnitManager.EnemyChampions.Count(HasGravitum) >= GravitumCanRoot,
                _ => false
            };
        }

        private bool HasGravitum(Hero enemy)
        {
            return enemy.BuffManager.ActiveBuffs.Any(x => x.Name == "ApheliosGravitumDebuff" && x.Stacks >= 1);
        }

        internal override void OnCoreRender()
        {
            SpellQ.DrawRange();
            SpellR.DrawRange();
        }

        internal override void OnCoreMainInput()
        {
            Orbwalker.SelectedTarget = UnitManager.EnemyChampions
                                                  .Where(TargetSelector.IsAttackable)
                                                  .FirstOrDefault(enemy => enemy.Distance <= 1450 &&
                                                                           enemy.BuffManager.ActiveBuffs.Any(x => x.Name == "aphelioscalibrumbonusrangedebuff" &&
                                                                                                                  x.Stacks == 1));
            SpellQ.ExecuteCastSpell();
            SpellR.ExecuteCastSpell();
        }

        //internal override void OnCoreMainTick()
        //{
        //    Logger.Log($"MainHand: {MainWeapon} - OffHand: {OffHand}");
        //}

        internal bool UseSeverum
        {
            get => QSeverumSettings.GetItem<Switch>("Use Severum").IsOn;
            set => QSeverumSettings.GetItem<Switch>("Use Severum").IsOn = value;
        }

        private int SeverumHealthPercentLessThan
        {
            get => QSeverumSettings.GetItem<Counter>("Severum Health percent less than").Value;
            set => QSeverumSettings.GetItem<Counter>("Severum Health percent less than").Value = value;
        }

        internal bool UseInfernum
        {
            get => QInfernumSettings.GetItem<Switch>("Use Infernum").IsOn;
            set => QInfernumSettings.GetItem<Switch>("Use Infernum").IsOn = value;
        }

        internal Prediction.MenuSelected.HitChance InfernumHitChance
        {
            get => (Prediction.MenuSelected.HitChance)Enum.Parse(typeof(Prediction.MenuSelected.HitChance), QInfernumSettings.GetItem<ModeDisplay>("Infernum HitChance").SelectedModeName);
            set => QInfernumSettings.GetItem<ModeDisplay>("Infernum HitChance").SelectedModeName = value.ToString();
        }

        internal bool UseCrescendum
        {
            get => QCrescendumSettings.GetItem<Switch>("Use Crescendum").IsOn;
            set => QCrescendumSettings.GetItem<Switch>("Use Crescendum").IsOn = value;
        }

        private int CrescendumEnemyIsCloserThan
        {
            get => QCrescendumSettings.GetItem<Counter>("Crescendum enemy is closer than").Value;
            set => QCrescendumSettings.GetItem<Counter>("Crescendum enemy is closer than").Value = value;
        }

        internal bool UseCalibrum
        {
            get => QCalibrumSettings.GetItem<Switch>("Use Calibrum").IsOn;
            set => QCalibrumSettings.GetItem<Switch>("Use Calibrum").IsOn = value;
        }

        internal Prediction.MenuSelected.HitChance CalibrumHitChance
        {
            get => (Prediction.MenuSelected.HitChance)Enum.Parse(typeof(Prediction.MenuSelected.HitChance), QCalibrumSettings.GetItem<ModeDisplay>("Calibrum HitChance").SelectedModeName);
            set => QCalibrumSettings.GetItem<ModeDisplay>("Calibrum HitChance").SelectedModeName = value.ToString();
        }

        private bool CalibrumOnlyOutsideOfAttackRange
        {
            get => QCalibrumSettings.GetItem<Switch>("Calibrum only outside of attack range").IsOn;
            set => QCalibrumSettings.GetItem<Switch>("Calibrum only outside of attack range").IsOn = value;
        }

        internal bool UseGravitum
        {
            get => QGravitumSettings.GetItem<Switch>("Use Gravitum").IsOn;
            set => QGravitumSettings.GetItem<Switch>("Use Gravitum").IsOn = value;
        }

        private int GravitumCanRoot
        {
            get => QGravitumSettings.GetItem<Counter>("Gravitum can root").Value;
            set => QGravitumSettings.GetItem<Counter>("Gravitum can root").Value = value;
        }

        internal Group QSeverumSettings => QSettings.GetItem<Group>("Severum Settings");
        internal Group QInfernumSettings => QSettings.GetItem<Group>("Infernum Settings");
        internal Group QCrescendumSettings => QSettings.GetItem<Group>("Crescendum Settings");
        internal Group QCalibrumSettings => QSettings.GetItem<Group>("Calibrum Settings");
        internal Group QGravitumSettings => QSettings.GetItem<Group>("Gravitum Settings");

        internal Group RSeverumSettings => RSettings.GetItem<Group>("Severum Settings");
        internal Group RInfernumSettings => RSettings.GetItem<Group>("Infernum Settings");
        internal Group RCrescendumSettings => RSettings.GetItem<Group>("Crescendum Settings");
        internal Group RCalibrumSettings => RSettings.GetItem<Group>("Calibrum Settings");
        internal Group RGravitumSettings => RSettings.GetItem<Group>("Gravitum Settings");

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Aphelios)}"));
            MenuTab.AddItem(new Group("Q Settings"));
            QSettings.AddItem(new Group("Severum Settings"));
            QSettings.AddItem(new Group("Infernum Settings"));
            QSettings.AddItem(new Group("Crescendum Settings"));
            QSettings.AddItem(new Group("Calibrum Settings"));
            QSettings.AddItem(new Group("Gravitum Settings"));

            MenuTab.AddItem(new Group("R Settings"));
            RSettings.AddItem(new Group("Severum Settings"));
            RSettings.AddItem(new Group("Infernum Settings"));
            RSettings.AddItem(new Group("Crescendum Settings"));
            RSettings.AddItem(new Group("Calibrum Settings"));
            RSettings.AddItem(new Group("Gravitum Settings"));

            QSeverumSettings.AddItem(new Switch() { Title = "Use Severum", IsOn = true });
            QSeverumSettings.AddItem(new Counter() { Title = "Severum Health percent less than", MinValue = 0, MaxValue = 100, Value = 50, ValueFrequency = 5 });


            QInfernumSettings.AddItem(new Switch() { Title = "Use Infernum", IsOn = true });
            QInfernumSettings.AddItem(new ModeDisplay() { Title = "Infernum HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "VeryHigh" });


            QCrescendumSettings.AddItem(new Switch() { Title = "Use Crescendum", IsOn = true });
            QCrescendumSettings.AddItem(new Counter() { Title = "Crescendum enemy is closer than", MinValue = 0, MaxValue = 900, Value = 500, ValueFrequency = 50 });


            QCalibrumSettings.AddItem(new Switch() { Title = "Use Calibrum", IsOn = true });
            QCalibrumSettings.AddItem(new ModeDisplay() { Title = "Calibrum HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "VeryHigh" });
            QCalibrumSettings.AddItem(new Switch() { Title = "Calibrum only outside of attack range", IsOn = false });


            QGravitumSettings.AddItem(new Switch() { Title = "Use Gravitum", IsOn = true });
            QGravitumSettings.AddItem(new Counter() { Title = "Gravitum can root", MinValue = 0, MaxValue = 5, Value = 2, ValueFrequency = 1 });




            RSeverumSettings.AddItem(new Switch() { Title = "Use Moonlight Vigil Severum", IsOn = true });
            RSeverumSettings.AddItem(new ModeDisplay() { Title = "Moonlight Vigil Severum HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "VeryHigh" });
            RSeverumSettings.AddItem(new Counter() { Title = "Moonlight Vigil Severum Health Percent Less Than", MinValue = 0, MaxValue = 100, Value = 40, ValueFrequency = 5 });
            RSeverumSettings.AddItem(new Counter() { Title = "Moonlight Vigil Severum Can Hit", MinValue = 0, MaxValue = 5, Value = 2, ValueFrequency = 1 });

            RInfernumSettings.AddItem(new Switch() { Title = "Use Moonlight Vigil Infernum", IsOn = true });
            RInfernumSettings.AddItem(new ModeDisplay() { Title = "Moonlight Vigil Infernum HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "VeryHigh" });
            RInfernumSettings.AddItem(new Counter() { Title = "Moonlight Vigil Infernum Can Hit", MinValue = 0, MaxValue = 5, Value = 2, ValueFrequency = 1 });

            RCrescendumSettings.AddItem(new Switch() { Title = "Use Moonlight Vigil Crescendum", IsOn = true });
            RCrescendumSettings.AddItem(new ModeDisplay() { Title = "Moonlight Vigil Crescendum HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "VeryHigh" });
            RCrescendumSettings.AddItem(new Counter() { Title = "Moonlight Vigil Crescendum Can Hit", MinValue = 0, MaxValue = 5, Value = 2, ValueFrequency = 1 });

            RCalibrumSettings.AddItem(new Switch() { Title = "Use Moonlight Vigil Calibrum", IsOn = true });
            RCalibrumSettings.AddItem(new ModeDisplay() { Title = "Moonlight Vigil Calibrum HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "VeryHigh" });
            RCalibrumSettings.AddItem(new Counter() { Title = "Moonlight Vigil Calibrum Can Hit", MinValue = 0, MaxValue = 5, Value = 2, ValueFrequency = 1 });

            RGravitumSettings.AddItem(new Switch() { Title = "Use Moonlight Vigil Gravitum", IsOn = true });
            RGravitumSettings.AddItem(new ModeDisplay() { Title = "Moonlight Vigil Gravitum HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "VeryHigh" });
            RGravitumSettings.AddItem(new Counter() { Title = "Moonlight Vigil Gravitum Can Hit", MinValue = 0, MaxValue = 5, Value = 2, ValueFrequency = 1 });


            MenuTab.AddDrawOptions(SpellSlot.Q, SpellSlot.R);
        }
    }
}
