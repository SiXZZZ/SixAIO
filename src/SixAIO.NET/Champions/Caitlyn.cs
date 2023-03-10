using Oasys.Common;
using Oasys.Common.Enums.GameEnums;
using Oasys.Common.GameObject;
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
    internal sealed class Caitlyn : Champion
    {
        internal Spell SpellEQ;
        internal Spell SpellWTargetted;
        private GameObjectBase _eTarget;
        private float _lastWCast;

        public Caitlyn()
        {
            SDKSpell.OnSpellCast += Spell_OnSpellCast;
            SpellEQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => QHitChance,
                Range = () => 1250,
                Radius = () => 120,
                Speed = () => 2200,
                Delay = () => 0.625f,
                IsEnabled = () => UseQ && UseE,
                TargetSelect = (mode) => _eTarget
            };
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => QHitChance,
                Range = () => 1250,
                Radius = () => 120,
                Speed = () => 2200,
                Delay = () => 0.625f,
                Damage = (target, spellClass) =>
                            target != null
                            ? DamageCalculator.GetArmorMod(UnitManager.MyChampion, target) *
                            ((10 + spellClass.Level * 40) +
                            (UnitManager.MyChampion.UnitStats.TotalAttackDamage * (1.15f + 0.15f * spellClass.Level)))
                            : 0,
                IsEnabled = () => UseQ,
                TargetSelect = (mode) => QOnlyOnHeadshotTargets
                        ? SpellQ.GetTargets(mode, x => x.BuffManager.ActiveBuffs.Any(b => b.Stacks >= 1 && b.Name == "CaitlynWSnare" || b.Name == "CaitlynEMissile")).FirstOrDefault()
                        : SpellQ.GetTargets(mode).FirstOrDefault()
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Circle,
                MinimumHitChance = () => WHitChance,
                Range = () => 780,
                Radius = () => 15,
                Speed = () => 5000,
                Delay = () => 0.4f,
                IsEnabled = () => UseW && _lastWCast + 1 <= EngineManager.GameTime,
                MinimumCharges = () => 1,
                TargetSelect = (mode) => SpellW.GetTargets(mode).FirstOrDefault()
            };
            SpellWTargetted = new Spell(CastSlot.W, SpellSlot.W)
            {
                IsTargetted = () => true,
                Range = () => 780,
                IsEnabled = () => UseW && _lastWCast + 1 <= EngineManager.GameTime,
                MinimumCharges = () => 1,
                TargetSelect = (mode) =>
                {
                    GameObjectBase target = null;

                    if (target is null)
                    {
                        target = UnitManager.EnemyChampions.FirstOrDefault(x => x.Distance <= SpellW.Range() && TargetSelector.IsAttackable(x) && x.IsChanneling);
                    }
                    if (target is null)
                    {
                        target = UnitManager.EnemyChampions.FirstOrDefault(x => x.Distance <= SpellW.Range() && x.BuffManager.ActiveBuffs.Any(buff => buff.Name == "ZhonyasRingShield" && buff.Stacks >= 1));
                    }

                    return target;
                }
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                AllowCollision = (target, collisions) => !collisions.Any(),
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => EHitChance,
                Range = () => 750,
                Radius = () => 140,
                Speed = () => 1600,
                IsEnabled = () => UseE && (OnlyEWithQ ? SpellQ.SpellClass.IsSpellReady : true),
                TargetSelect = (mode) => SpellE.GetTargets(mode, x => x.Distance <= EMaximumRange).FirstOrDefault()
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                AllowCastOnMap = () => AllowRCastOnMinimap,
                IsTargetted = () => true,
                Delay = () => 1f,
                Range = () => 3500f,
                Damage = (target, spellClass) =>
                            target != null
                            ? DamageCalculator.GetArmorMod(UnitManager.MyChampion, target) *
                            ((75 + spellClass.Level * 225) +
                            (UnitManager.MyChampion.UnitStats.BonusAttackDamage * 2f))
                            : 0,
                IsEnabled = () => UseR,
                IsSpellReady = (spellClass, minimumMana, minimumCharges) => spellClass.IsSpellReady && UnitManager.MyChampion.Mana >= spellClass.SpellData.ResourceCost && !UnitManager.EnemyChampions.Any(x => x.Distance < RSafeRange),
                ShouldCast = (mode, target, spellClass, damage) => target != null && target.Health < damage,
                TargetSelect = (mode) => SpellR.GetTargets(mode).FirstOrDefault()
            };
        }

        private void Spell_OnSpellCast(SDKSpell spell, GameObjectBase target)
        {
            if (spell.SpellSlot == SpellSlot.E)
            {
                _eTarget = target;
                SpellEQ.ExecuteCastSpell();
            }
        }

        internal override void OnCoreMainInput()
        {
            if (UnitManager.EnemyChampions.Any(x => TargetSelector.IsAttackable(x) && (x.BuffManager.HasActiveBuff("CaitlynWSnare") || x.BuffManager.HasActiveBuff("CaitlynEMissile")) && x.Distance <= 1300))
            {
                Orbwalker.SelectedTarget = UnitManager.EnemyChampions.FirstOrDefault(x => TargetSelector.IsAttackable(x) && (x.BuffManager.HasActiveBuff("CaitlynWSnare") || x.BuffManager.HasActiveBuff("CaitlynEMissile")) && x.Distance <= 1300);
            }

            if (SpellWTargetted.ExecuteCastSpell() || SpellW.ExecuteCastSpell())
            {
                _lastWCast = GameEngine.GameTime;
            }

            if (SpellE.ExecuteCastSpell() || SpellQ.ExecuteCastSpell() || SpellR.ExecuteCastSpell())
            {
                return;
            }
        }

        internal bool QOnlyOnHeadshotTargets
        {
            get => QSettings.GetItem<Switch>("Q Only On Headshot Targets").IsOn;
            set => QSettings.GetItem<Switch>("Q Only On Headshot Targets").IsOn = value;
        }

        internal bool OnlyEWithQ
        {
            get => ESettings.GetItem<Switch>("Only E With Q").IsOn;
            set => ESettings.GetItem<Switch>("Only E With Q").IsOn = value;
        }

        private int EMaximumRange
        {
            get => ESettings.GetItem<Counter>("E Maximum Range").Value;
            set => ESettings.GetItem<Counter>("E Maximum Range").Value = value;
        }

        private int RSafeRange
        {
            get => RSettings.GetItem<Counter>("R Safe Range").Value;
            set => RSettings.GetItem<Counter>("R Safe Range").Value = value;
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Caitlyn)}"));
            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Q Only On Headshot Targets", IsOn = true });
            QSettings.AddItem(new ModeDisplay() { Title = "Q HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });
            WSettings.AddItem(new ModeDisplay() { Title = "W HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "Immobile" });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });
            ESettings.AddItem(new Switch() { Title = "Only E With Q", IsOn = false });
            ESettings.AddItem(new ModeDisplay() { Title = "E HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });
            ESettings.AddItem(new Counter() { Title = "E Maximum Range", MinValue = 0, MaxValue = 800, Value = 750, ValueFrequency = 50 });

            RSettings.AddItem(new Switch() { Title = "Use R", IsOn = true });
            RSettings.AddItem(new Switch() { Title = "Allow R cast on minimap", IsOn = false });
            RSettings.AddItem(new Counter() { Title = "R Safe Range", MinValue = 0, MaxValue = 3500, Value = 1000, ValueFrequency = 50 });
        }
    }
}
