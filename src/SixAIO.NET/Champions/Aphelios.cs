using Oasys.Common;
using Oasys.Common.Enums.GameEnums;
using Oasys.Common.EventsProvider;
using Oasys.Common.GameObject;
using Oasys.Common.GameObject.Clients;
using Oasys.Common.GameObject.Clients.ExtendedInstances.Spells;
using Oasys.Common.GameObject.ObjectClass;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.Common.Tools;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SixAIO.Extensions;
using SixAIO.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SixAIO.Champions
{
    internal sealed class Aphelios : Champion
    {
        internal Spell SpellWR;

        private Storage<GunType, float> QCooldownTracked = new Storage<GunType, float>();

        public GunType MainWeapon => UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.Q).SpellData.SpellName switch
        {
            "ApheliosSeverumQ" => GunType.ApheliosSeverum,
            "ApheliosInfernumQ" => GunType.ApheliosInfernum,
            "ApheliosCrescendumQ" => GunType.ApheliosCrescendum,
            "ApheliosCalibrumQ" => GunType.ApheliosCalibrum,
            "ApheliosGravitumQ" => GunType.ApheliosGravitum,
            _ => GunType.Unknown
        };

        public GunType OffHand
        {
            get
            {
                var offHandGunBuff = UnitManager.MyChampion.BuffManager.ActiveBuffs.FirstOrDefault(x => x.Name.Contains("ApheliosOffHandBuff") && x.Stacks >= 1);
                if (offHandGunBuff is not null)
                {
                    return offHandGunBuff.Name switch
                    {
                        "ApheliosOffHandBuffCalibrum" => GunType.ApheliosCalibrum,
                        "ApheliosOffHandBuffSeverum" => GunType.ApheliosSeverum,
                        "ApheliosOffHandBuffGravitum" => GunType.ApheliosGravitum,
                        "ApheliosOffHandBuffInfernum" => GunType.ApheliosInfernum,
                        "ApheliosOffHandBuffCrescendum" => GunType.ApheliosCrescendum,
                        _ => GunType.Unknown
                    };
                }

                return GunType.Unknown;
            }
        }

        public enum GunType
        {
            Unknown,
            ApheliosSeverum,
            ApheliosInfernum,
            ApheliosCrescendum,
            ApheliosCalibrum,
            ApheliosGravitum,
        }

        public Aphelios()
        {
            QCooldownTracked[GunType.ApheliosSeverum] = 0;
            QCooldownTracked[GunType.ApheliosInfernum] = 0;
            QCooldownTracked[GunType.ApheliosCalibrum] = 0;
            QCooldownTracked[GunType.ApheliosGravitum] = 0;
            QCooldownTracked[GunType.ApheliosCrescendum] = 0;

            GameEvents.OnGameProcessSpell += GameEvents_OnGameProcessSpell;
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                ShouldDraw = () => DrawQRange,
                DrawColor = () => DrawQColor,
                AllowCollision = (target, collisions) => QAllowCollision(target, collisions, MainWeapon),
                PredictionMode = () => QPredictionMode(MainWeapon),
                MinimumHitChance = () => GetQHitChance(MainWeapon),
                Range = () => QRange(MainWeapon),
                Radius = () => QRadius(MainWeapon),
                Speed = () => QSpeed(MainWeapon),
                IsEnabled = () => QIsEnabled(MainWeapon),
                MinimumMana = () => QMinimumMana(MainWeapon),
                ShouldCast = (mode, target, spellClass, damage) => QShouldCast(mode, target, spellClass, damage, MainWeapon, SpellQ.Range()),
                TargetSelect = (mode) => QTargetSelect(mode, SpellQ.GetTargets(mode), MainWeapon)
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                AllowCollision = (target, collisions) => QAllowCollision(target, collisions, OffHand),
                PredictionMode = () => QPredictionMode(OffHand),
                MinimumHitChance = () => GetQHitChance(OffHand),
                Range = () => QRange(OffHand),
                Radius = () => QRadius(OffHand),
                Speed = () => QSpeed(OffHand),
                IsEnabled = () => WIsEnabled(OffHand) && QIsEnabled(OffHand),
                MinimumMana = () => QMinimumMana(OffHand),
                ShouldCast = (mode, target, spellClass, damage) => EngineManager.GameTime >= QCooldownTracked[OffHand] && QShouldCast(mode, target, spellClass, damage, OffHand, SpellW.Range()),
                TargetSelect = (mode) => QTargetSelect(mode, SpellW.GetTargets(mode), OffHand)
            };
            SpellWR = new Spell(CastSlot.W, SpellSlot.W)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => GetRHitChance(OffHand),
                Range = () => 1300,
                Radius = () => 200,
                Speed = () => 1000,
                Delay = () => 0.6f,
                IsEnabled = () => RIsEnabled(OffHand),
                MinimumMana = () => 100,
                TargetSelect = (mode) => RTargetSelect(mode, OffHand)
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                ShouldDraw = () => DrawRRange,
                DrawColor = () => DrawRColor,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => GetRHitChance(MainWeapon),
                Range = () => 1300,
                Radius = () => 200,
                Speed = () => 1000,
                Delay = () => 0.6f,
                IsEnabled = () => RIsEnabled(MainWeapon),
                MinimumMana = () => 100,
                TargetSelect = (mode) => RTargetSelect(mode, MainWeapon)
            };
        }

        private bool WIsEnabled(GunType gun)
        {
            return gun switch
            {
                GunType.ApheliosSeverum => CanSwapToSeverum,
                GunType.ApheliosInfernum => CanSwapToInfernum,
                GunType.ApheliosCrescendum => CanSwapToCrescendum,
                GunType.ApheliosCalibrum => CanSwapToCalibrum,
                GunType.ApheliosGravitum => CanSwapToGravitum,
                _ => false
            };
        }

        private float _lastSpellCast = 0f;
        private Task GameEvents_OnGameProcessSpell(AIBaseClient caster, SpellActiveEntry spell)
        {
            if (caster is not null &&
                spell is not null &&
                caster.IsMe &&
                spell.SpellSlot == SpellSlot.Q &&
                spell.CastStartTime > _lastSpellCast)
            {
                _lastSpellCast = spell.CastStartTime;
                QCooldownTracked[MainWeapon] = spell.CastEndTime + GetCooldown(MainWeapon);
            }

            return Task.CompletedTask;
        }

        private float GetCooldown(GunType gun)
        {
            var level = UnitManager.MyChampion.Level;
            return gun switch
            {
                GunType.ApheliosSeverum => Math.Max(8, 10f - (0.3f * (level / 2))),
                GunType.ApheliosInfernum => Math.Max(6, 9f - (0.5f * (level / 2))),
                GunType.ApheliosCrescendum => Math.Max(6, 9f - (0.5f * (level / 2))),
                GunType.ApheliosCalibrum => Math.Max(8, 10f - (0.3f * (level / 2))),
                GunType.ApheliosGravitum => Math.Max(10, 12f - (0.3f * (level / 2))),
                _ => Math.Max(8, 10f - (0.3f * (level / 2))),
            };
        }

        private GameObjectBase QTargetSelect(Orbwalker.OrbWalkingModeType mode, IEnumerable<GameObjectBase> targets, GunType gun)
        {
            if (mode != Orbwalker.OrbWalkingModeType.Combo)
            {
                return null;
            }

            return gun switch
            {
                GunType.ApheliosInfernum => targets.FirstOrDefault(),
                GunType.ApheliosCrescendum => targets.FirstOrDefault(),
                GunType.ApheliosCalibrum => targets.FirstOrDefault(),
                _ => null
            };
        }

        private GameObjectBase RTargetSelect(Orbwalker.OrbWalkingModeType mode, GunType gun)
        {
            if (mode != Orbwalker.OrbWalkingModeType.Combo)
            {
                return null;
            }

            var moonlightVigilCanHit = gun switch
            {
                GunType.ApheliosSeverum => RSeverumSettings.GetItem<Counter>("Moonlight Vigil Severum Can Hit").Value,
                GunType.ApheliosInfernum => RInfernumSettings.GetItem<Counter>("Moonlight Vigil Infernum Can Hit").Value,
                GunType.ApheliosCrescendum => RCrescendumSettings.GetItem<Counter>("Moonlight Vigil Crescendum Can Hit").Value,
                GunType.ApheliosCalibrum => RCalibrumSettings.GetItem<Counter>("Moonlight Vigil Calibrum Can Hit").Value,
                GunType.ApheliosGravitum => RGravitumSettings.GetItem<Counter>("Moonlight Vigil Gravitum Can Hit").Value,
                _ => 5,
            };

            return SpellR.GetTargets(mode, target => moonlightVigilCanHit <= UnitManager.EnemyChampions.Count(enemy => target.DistanceTo(enemy.Position) <= SpellR.Radius()))
                         .FirstOrDefault();
        }

        private float QMinimumMana(GunType gun)
        {
            return gun switch
            {
                GunType.ApheliosSeverum => 60f,
                GunType.ApheliosInfernum => 60f,
                GunType.ApheliosCrescendum => 60f,
                GunType.ApheliosCalibrum => 60f,
                GunType.ApheliosGravitum => 60f,
                _ => 60f
            };
        }

        private bool QIsEnabled(GunType gun)
        {
            return gun switch
            {
                GunType.ApheliosSeverum => UseSeverum && UnitManager.MyChampion.HealthPercent <= SeverumHealthPercentLessThan,
                GunType.ApheliosInfernum => UseInfernum,
                GunType.ApheliosCrescendum => UseCrescendum,
                GunType.ApheliosCalibrum => UseCalibrum,
                GunType.ApheliosGravitum => UseGravitum,
                _ => false
            };
        }

        private bool RIsEnabled(GunType gun)
        {
            return gun switch
            {
                GunType.ApheliosSeverum => RSeverumSettings.GetItem<Switch>("Use Moonlight Vigil Severum").IsOn &&
                                            UnitManager.MyChampion.HealthPercent <= RSeverumSettings.GetItem<Counter>("Moonlight Vigil Severum Health Percent Less Than").Value,
                GunType.ApheliosInfernum => RInfernumSettings.GetItem<Switch>("Use Moonlight Vigil Infernum").IsOn,
                GunType.ApheliosCrescendum => RCrescendumSettings.GetItem<Switch>("Use Moonlight Vigil Crescendum").IsOn,
                GunType.ApheliosCalibrum => RCalibrumSettings.GetItem<Switch>("Use Moonlight Vigil Calibrum").IsOn,
                GunType.ApheliosGravitum => RGravitumSettings.GetItem<Switch>("Use Moonlight Vigil Gravitum").IsOn,
                _ => false
            };
        }

        private float QSpeed(GunType gun)
        {
            return gun switch
            {
                GunType.ApheliosSeverum => 0f,
                GunType.ApheliosInfernum => 1000f,
                GunType.ApheliosCrescendum => 0f,
                GunType.ApheliosCalibrum => 1850f,
                GunType.ApheliosGravitum => 0f,
                _ => 0f
            };
        }

        private float QRadius(GunType gun)
        {
            return gun switch
            {
                GunType.ApheliosSeverum => 0f,
                GunType.ApheliosInfernum => 40f,
                GunType.ApheliosCrescendum => 475f,
                GunType.ApheliosCalibrum => 120f,
                GunType.ApheliosGravitum => 0f,
                _ => 0f
            };
        }

        private float QRange(GunType gun)
        {
            return gun switch
            {
                GunType.ApheliosSeverum => 550f,
                GunType.ApheliosInfernum => 650f,
                GunType.ApheliosCrescendum => 475f,
                GunType.ApheliosCalibrum => 1450f,
                GunType.ApheliosGravitum => 30_000f,
                _ => 0f
            };
        }

        private Prediction.MenuSelected.HitChance GetQHitChance(GunType gun)
        {
            return gun switch
            {
                GunType.ApheliosInfernum => InfernumHitChance,
                GunType.ApheliosCalibrum => CalibrumHitChance,
                _ => Prediction.MenuSelected.HitChance.Low
            };
        }

        private Prediction.MenuSelected.HitChance GetRHitChance(GunType gun)
        {
            var setting = gun switch
            {
                GunType.ApheliosSeverum => RSeverumSettings.GetItem<ModeDisplay>("Moonlight Vigil Severum HitChance").SelectedModeName,
                GunType.ApheliosInfernum => RInfernumSettings.GetItem<ModeDisplay>("Moonlight Vigil Infernum HitChance").SelectedModeName,
                GunType.ApheliosCrescendum => RCrescendumSettings.GetItem<ModeDisplay>("Moonlight Vigil Crescendum HitChance").SelectedModeName,
                GunType.ApheliosCalibrum => RCalibrumSettings.GetItem<ModeDisplay>("Moonlight Vigil Calibrum HitChance").SelectedModeName,
                GunType.ApheliosGravitum => RGravitumSettings.GetItem<ModeDisplay>("Moonlight Vigil Gravitum HitChance").SelectedModeName,
                _ => Prediction.MenuSelected.HitChance.Immobile.ToString(),
            };

            return (Prediction.MenuSelected.HitChance)Enum.Parse(typeof(Prediction.MenuSelected.HitChance), setting);
        }

        private Prediction.MenuSelected.PredictionType QPredictionMode(GunType gun)
        {
            return gun switch
            {
                GunType.ApheliosSeverum => Prediction.MenuSelected.PredictionType.Circle,
                GunType.ApheliosInfernum => Prediction.MenuSelected.PredictionType.Cone,
                GunType.ApheliosCrescendum => Prediction.MenuSelected.PredictionType.Circle,
                GunType.ApheliosCalibrum => Prediction.MenuSelected.PredictionType.Line,
                GunType.ApheliosGravitum => Prediction.MenuSelected.PredictionType.Circle,
                _ => Prediction.MenuSelected.PredictionType.Circle
            };
        }

        private bool QAllowCollision(GameObjectBase target, IEnumerable<GameObjectBase> collisions, GunType gun)
        {
            return gun switch
            {
                GunType.ApheliosSeverum => true,
                GunType.ApheliosInfernum => true,
                GunType.ApheliosCrescendum => true,
                GunType.ApheliosCalibrum => false,
                GunType.ApheliosGravitum => true,
                _ => false
            };
        }

        private bool QShouldCast(Orbwalker.OrbWalkingModeType mode, GameObjectBase target, SpellClass spellClass, float damage, GunType gun, float range)
        {
            if (mode != Orbwalker.OrbWalkingModeType.Combo)
            {
                return false;
            }

            return gun switch
            {
                GunType.ApheliosSeverum => UnitManager.EnemyChampions.Any(enemy => enemy.IsAlive && enemy.Distance <= range),
                GunType.ApheliosInfernum => target != null,
                GunType.ApheliosCrescendum => target != null && target.Distance <= CrescendumEnemyIsCloserThan && spellClass.IsSpellReady,
                GunType.ApheliosCalibrum => target != null && (!CalibrumOnlyOutsideOfAttackRange || UnitManager.EnemyChampions.All(x => x.Distance >= UnitManager.MyChampion.TrueAttackRange)) && spellClass.IsSpellReady,
                GunType.ApheliosGravitum => UnitManager.EnemyChampions.Count(HasGravitum) >= GravitumCanRoot,
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

            //for (int i = 0; i < Enum.GetValues<GunType>().Length; i++)
            //{
            //    var gunType = Enum.GetValues<GunType>()[i];
            //    var w2s = UnitManager.MyChampion.W2S;
            //    w2s.X += 100;
            //    w2s.Y += i * 25;
            //    RenderFactoryProvider.DrawText($"Gun: {gunType} - Ready: {QCooldownTracked[gunType]} - IsReady:{EngineManager.GameTime > QCooldownTracked[gunType]}", w2s, Color.White, false);
            //}
            //var pos = UnitManager.MyChampion.W2S;
            //pos.X += 100;
            //pos.Y += 6 * 25;
            //RenderFactoryProvider.DrawText($"Time: {EngineManager.GameTime} - Main:{MainWeapon} - OffHand:{OffHand}", pos, Color.White, false);
            //pos.Y += 25;
            //RenderFactoryProvider.DrawText($"Main: {MainWeapon} - CanCast:{SpellQ.CanExecuteCastSpell()} - Enabled:{SpellQ.IsEnabled()} - ShouldCast:{SpellQ.ShouldCast(Orbwalker.OrbWalkingModeType.Combo, SpellQ.TargetSelect(Orbwalker.OrbWalkingModeType.Combo), SpellQ.SpellClass, 0)} - Target:{SpellQ.TargetSelect(Orbwalker.OrbWalkingModeType.Combo)}", pos, Color.White, false);
            //pos.Y += 25;
            //RenderFactoryProvider.DrawText($"OffHand: {OffHand} - CanCast:{SpellW.CanExecuteCastSpell()} - Enabled:{SpellW.IsEnabled()} - ShouldCast:{SpellW.ShouldCast(Orbwalker.OrbWalkingModeType.Combo, SpellW.TargetSelect(Orbwalker.OrbWalkingModeType.Combo), SpellW.SpellClass, 0)} - Target:{SpellW.TargetSelect(Orbwalker.OrbWalkingModeType.Combo)}", pos, Color.White, false);
        }

        internal override void OnCoreMainInput()
        {
            Orbwalker.SelectedTarget = UnitManager.EnemyChampions
                                                  .Where(TargetSelector.IsAttackable)
                                                  .FirstOrDefault(enemy => enemy.Distance <= 1450 &&
                                                                           enemy.BuffManager.ActiveBuffs.Any(x => x.Name == "aphelioscalibrumbonusrangedebuff" &&
                                                                                                                  x.Stacks == 1));
            SpellQ.ExecuteCastSpell();
            SpellW.ExecuteCastSpell();
            //SpellWR.ExecuteCastSpell();
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

        internal bool CanSwapToSeverum => WSettings.GetItem<Switch>("Can Swap To Severum").IsOn;
        internal bool CanSwapToInfernum => WSettings.GetItem<Switch>("Can Swap To Infernum").IsOn;
        internal bool CanSwapToCrescendum => WSettings.GetItem<Switch>("Can Swap To Crescendum").IsOn;
        internal bool CanSwapToCalibrum => WSettings.GetItem<Switch>("Can Swap To Calibrum").IsOn;
        internal bool CanSwapToGravitum => WSettings.GetItem<Switch>("Can Swap To Gravitum").IsOn;

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

            MenuTab.AddItem(new Group("W Settings"));

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


            WSettings.AddItem(new Switch() { Title = "Can Swap To Severum", IsOn = true });
            WSettings.AddItem(new Switch() { Title = "Can Swap To Infernum", IsOn = true });
            WSettings.AddItem(new Switch() { Title = "Can Swap To Crescendum", IsOn = true });
            WSettings.AddItem(new Switch() { Title = "Can Swap To Calibrum", IsOn = true });
            WSettings.AddItem(new Switch() { Title = "Can Swap To Gravitum", IsOn = true });


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
