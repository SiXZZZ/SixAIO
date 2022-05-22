using Oasys.Common.Enums.GameEnums;
using Oasys.Common.GameObject;
using Oasys.Common.GameObject.Clients.ExtendedInstances.Spells;
using Oasys.Common.GameObject.ObjectClass;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SixAIO.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SixAIO.Champions
{
    internal class Aphelios : Champion
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
                ShouldCast = QShouldCast,
                AllowCollision = QAllowCollision,
                PredictionMode = QPredictionMode,
                MinimumHitChance = GetQHitChance,
                Range = QRange,
                Radius = QRadius,
                Speed = QSpeed,
                IsEnabled = QIsEnabled,
                MinimumMana = QMinimumMana,
                TargetSelect = QTargetSelect
            };
            //SpellR = new Spell(CastSlot.R, SpellSlot.R)
            //{
            //    AllowCastOnMap = () => AllowRCastOnMinimap,
            //    PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
            //    MinimumHitChance = () => RHitChance,
            //    Range = () => 30000,
            //    Radius = () => 320,
            //    Speed = () => 2000,
            //    Delay = () => 1f,
            //    Damage = (target, spellClass) =>
            //                target != null
            //                ? DamageCalculator.GetMagicResistMod(UnitManager.MyChampion, target) *
            //                            ((200 + spellClass.Level * 150) +
            //                            (UnitManager.MyChampion.UnitStats.BonusAttackDamage) +
            //                            (UnitManager.MyChampion.UnitStats.TotalAbilityPower))
            //                : 0,
            //    IsEnabled = () => UseR,
            //    MinimumMana = () => RMinMana,
            //    ShouldCast = (mode, target, spellClass, damage) => target != null && target.Health < damage,
            //    TargetSelect = (mode) => SpellR.GetTargets(mode).FirstOrDefault()
            //};
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
                GunType.ApheliosSeverumQ => UseSeverum,
                GunType.ApheliosInfernumQ => UseInfernum,
                GunType.ApheliosCrescendumQ => UseCrescendum,
                GunType.ApheliosCalibrumQ => UseCalibrum,
                GunType.ApheliosGravitumQ => UseGravitum,
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
                GunType.ApheliosSeverumQ => UnitManager.MyChampion.HealthPercent <= SeverumHealthPercentLessThan && UnitManager.EnemyChampions.Any(enemy => enemy.IsAlive && enemy.Distance <= SpellQ.Range()),
                GunType.ApheliosInfernumQ => target != null,
                GunType.ApheliosCrescendumQ => target != null && target.Distance <= CrescendumEnemyIsCloserThan && spellClass.IsSpellReady,
                GunType.ApheliosCalibrumQ => target != null && (!CalibrumOnlyOutsideOfAttackRange || UnitManager.EnemyChampions.All(x => x.Distance >= UnitManager.MyChampion.TrueAttackRange)) && spellClass.IsSpellReady,
                GunType.ApheliosGravitumQ => UnitManager.EnemyChampions.Count(HasGravitum) >= GravitumCanRoot,
                _ => false
            };
        }

        private bool HasGravitum(Hero enemy)
        {
            return enemy.BuffManager.HasActiveBuff("ApheliosGravitumDebuff");
        }

        internal override void OnCoreMainInput()
        {
            if (SpellQ.ExecuteCastSpell())
            {
                return;
            }
        }

        internal bool UseSeverum
        {
            get => SeverumSettings.GetItem<Switch>("Use Severum").IsOn;
            set => SeverumSettings.GetItem<Switch>("Use Severum").IsOn = value;
        }

        private int SeverumHealthPercentLessThan
        {
            get => SeverumSettings.GetItem<Counter>("Severum Health percent less than").Value;
            set => SeverumSettings.GetItem<Counter>("Severum Health percent less than").Value = value;
        }

        internal bool UseInfernum
        {
            get => InfernumSettings.GetItem<Switch>("Use Infernum").IsOn;
            set => InfernumSettings.GetItem<Switch>("Use Infernum").IsOn = value;
        }

        internal Prediction.MenuSelected.HitChance InfernumHitChance
        {
            get => (Prediction.MenuSelected.HitChance)Enum.Parse(typeof(Prediction.MenuSelected.HitChance), InfernumSettings.GetItem<ModeDisplay>("Infernum HitChance").SelectedModeName);
            set => InfernumSettings.GetItem<ModeDisplay>("Infernum HitChance").SelectedModeName = value.ToString();
        }

        internal bool UseCrescendum
        {
            get => CrescendumSettings.GetItem<Switch>("Use Crescendum").IsOn;
            set => CrescendumSettings.GetItem<Switch>("Use Crescendum").IsOn = value;
        }

        private int CrescendumEnemyIsCloserThan
        {
            get => CrescendumSettings.GetItem<Counter>("Crescendum enemy is closer than").Value;
            set => CrescendumSettings.GetItem<Counter>("Crescendum enemy is closer than").Value = value;
        }

        internal bool UseCalibrum
        {
            get => CalibrumSettings.GetItem<Switch>("Use Calibrum").IsOn;
            set => CalibrumSettings.GetItem<Switch>("Use Calibrum").IsOn = value;
        }

        internal Prediction.MenuSelected.HitChance CalibrumHitChance
        {
            get => (Prediction.MenuSelected.HitChance)Enum.Parse(typeof(Prediction.MenuSelected.HitChance), CalibrumSettings.GetItem<ModeDisplay>("Calibrum HitChance").SelectedModeName);
            set => CalibrumSettings.GetItem<ModeDisplay>("Calibrum HitChance").SelectedModeName = value.ToString();
        }

        private bool CalibrumOnlyOutsideOfAttackRange
        {
            get => CalibrumSettings.GetItem<Switch>("Calibrum only outside of attack range").IsOn;
            set => CalibrumSettings.GetItem<Switch>("Calibrum only outside of attack range").IsOn = value;
        }

        internal bool UseGravitum
        {
            get => GravitumSettings.GetItem<Switch>("Use Gravitum").IsOn;
            set => GravitumSettings.GetItem<Switch>("Use Gravitum").IsOn = value;
        }

        private int GravitumCanRoot
        {
            get => GravitumSettings.GetItem<Counter>("Gravitum can root").Value;
            set => GravitumSettings.GetItem<Counter>("Gravitum can root").Value = value;
        }

        internal Group SeverumSettings => MenuTab.GetGroup("Severum Settings");
        internal Group InfernumSettings => MenuTab.GetGroup("Infernum Settings");
        internal Group CrescendumSettings => MenuTab.GetGroup("Crescendum Settings");
        internal Group CalibrumSettings => MenuTab.GetGroup("Calibrum Settings");
        internal Group GravitumSettings => MenuTab.GetGroup("Gravitum Settings");

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Aphelios)}"));
            MenuTab.AddGroup(new Group("Severum Settings"));
            MenuTab.AddGroup(new Group("Infernum Settings"));
            MenuTab.AddGroup(new Group("Crescendum Settings"));
            MenuTab.AddGroup(new Group("Calibrum Settings"));
            MenuTab.AddGroup(new Group("Gravitum Settings"));

            SeverumSettings.AddItem(new Switch() { Title = "Use Severum", IsOn = true });
            SeverumSettings.AddItem(new Counter() { Title = "Severum Health percent less than", MinValue = 0, MaxValue = 100, Value = 50, ValueFrequency = 5 });


            InfernumSettings.AddItem(new Switch() { Title = "Use Infernum", IsOn = true });
            InfernumSettings.AddItem(new ModeDisplay() { Title = "Infernum HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });


            CrescendumSettings.AddItem(new Switch() { Title = "Use Crescendum", IsOn = true });
            CrescendumSettings.AddItem(new Counter() { Title = "Crescendum enemy is closer than", MinValue = 0, MaxValue = 900, Value = 500, ValueFrequency = 50 });


            CalibrumSettings.AddItem(new Switch() { Title = "Use Calibrum", IsOn = true });
            CalibrumSettings.AddItem(new ModeDisplay() { Title = "Calibrum HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });
            CalibrumSettings.AddItem(new Switch() { Title = "Calibrum only outside of attack range", IsOn = false });


            GravitumSettings.AddItem(new Switch() { Title = "Use Gravitum", IsOn = true });
            GravitumSettings.AddItem(new Counter() { Title = "Gravitum can root", MinValue = 0, MaxValue = 5, Value = 2, ValueFrequency = 1 });
        }
    }
}
