using Oasys.Common.Enums.GameEnums;
using Oasys.Common.GameObject;
using Oasys.Common.GameObject.Clients.ExtendedInstances.Spells;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using Oasys.SDK.Tools;
using SixAIO.Models;
using System;
using System.Linq;

namespace SixAIO.Champions
{
    internal class Pyke : Champion
    {
        private bool IsChargingQ => UnitManager.MyChampion.BuffManager.ActiveBuffs.Any(x => x.Name == "PykeQ" && x.Stacks >= 1);

        private bool IsCastingE => GameEngine.GameTime + SpellE.SpellClass.Cooldown - SpellE.SpellClass.CooldownExpire < 1;

        public Pyke()
        {
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                AllowCollision = (target, collisions) => false,
                IsCharge = () => true,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => QHitChance,
                Range = () => SpellQ.ChargeTimer.IsRunning
                                        ? SpellQ.SpellClass.IsSpellReady
                                            ? 400 + (SpellQ.ChargeTimer.ElapsedMilliseconds - 400) / 1000 / 0.1f * 116.67f
                                            : 0
                                        : 1100,
                Radius = () => 140,
                Speed = () => 1500,
                IsEnabled = () => UseQ && !IsCastingE,
                MinimumMana = () => 90,
                ShouldCast = (mode, target, spellClass, damage) => target != null && target.Distance < SpellQ.Range() && SpellQ.Range() >= QMinimumChargeRange,
                TargetSelect = (mode) => SpellQ.GetTargets(mode).FirstOrDefault()
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => EHitChance,
                Delay = () => 0f,
                Range = () => ERange,
                Radius = () => 110,
                Speed = () => 3000,
                IsEnabled = () => UseE && !IsChargingQ,
                TargetSelect = (mode) => SpellE.GetTargets(mode).FirstOrDefault()
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Circle,
                MinimumHitChance = () => RHitChance,
                Range = () => 750,
                Delay = () => 0.5f,
                Radius = () => 250,
                Speed = () => 2500,
                IsEnabled = () => UseR,
                Damage = (target, spellClass) => GetRDamage(target),
                ShouldCast = (mode, target, spellClass, damage) => target != null && target.Health < damage,
                TargetSelect = (mode) => SpellR.GetTargets(mode).FirstOrDefault()
            };
        }

        private static float GetRDamage(GameObjectBase target)
        {
            if (target == null)
            {
                return 0f;
            }
            var dmg = RBaseDamage(UnitManager.MyChampion.Level);
            dmg += UnitManager.MyChampion.UnitStats.BonusAttackDamage * 0.8f;
            dmg += UnitManager.MyChampion.UnitStats.PhysicalLethality * 1.5f;

            if (target.Health >= dmg)
            {
                return 0f;
            }
            //Logger.Log($"r damge: {dmg}");
            return dmg;
        }

        private static float RBaseDamage(int level) => level switch
        {
            6 => 250,
            7 => 290,
            8 => 330,
            9 => 370,
            10 => 400,
            11 => 430,
            12 => 450,
            13 => 470,
            14 => 490,
            15 => 510,
            16 => 530,
            17 => 540,
            18 => 550,
            _ => 0,
        };

        internal override void OnCoreMainInput()
        {
            if (SpellR.ExecuteCastSpell() || SpellE.ExecuteCastSpell() || SpellQ.ExecuteCastSpell())
            {
                return;
            }
        }

        private int QMinimumChargeRange
        {
            get => QSettings.GetItem<Counter>("Q Minimum Charge Range").Value;
            set => QSettings.GetItem<Counter>("Q Minimum Charge Range").Value = value;
        }

        private int ERange
        {
            get => ESettings.GetItem<Counter>("E range").Value;
            set => ESettings.GetItem<Counter>("E range").Value = value;
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Pyke)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new Counter() { Title = "Q Minimum Charge Range", MinValue = 50, MaxValue = 1000, Value = 400, ValueFrequency = 25 });
            QSettings.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });
            ESettings.AddItem(new ModeDisplay() { Title = "E HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });
            ESettings.AddItem(new Counter() { Title = "E range", MinValue = 50, MaxValue = 1500, Value = 800, ValueFrequency = 50 });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            RSettings.AddItem(new ModeDisplay() { Title = "R HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "Immobile" });

        }
    }
}
