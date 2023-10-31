using Oasys.Common;
using Oasys.Common.Enums.GameEnums;
using Oasys.Common.GameObject;
using Oasys.Common.Menu;
using Oasys.Common.Menu.ItemComponents;
using Oasys.SDK;
using Oasys.SDK.Menu;
using Oasys.SDK.SpellCasting;
using SixAIO.Extensions;
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
                ShouldDraw = () => DrawQRange,
                DrawColor = () => DrawQColor,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => QHitChance,
                Range = () => 1250,
                Radius = () => 120,
                Speed = () => 2200,
                Delay = () => 0.625f,
                IsEnabled = () => UseQ,
                TargetSelect = (mode) => QOnlyOnHeadshotTargets
                        ? SpellQ.GetTargets(mode, t => t.BuffManager.ActiveBuffs.Any(b => b.Name == "CaitlynWSnare" || b.Name == "CaitlynEMissile" && b.Stacks >= 1)).FirstOrDefault()
                        : SpellQ.GetTargets(mode).FirstOrDefault()
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                ShouldDraw = () => DrawWRange,
                DrawColor = () => DrawWColor,
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Circle,
                MinimumHitChance = () => WHitChance,
                Range = () => 785,
                Radius = () => 15,
                Speed = () => 2200,
                Delay = () => 0.25f,
                IsEnabled = () => UseW && _lastWCast + 1 <= EngineManager.GameTime,
                MinimumCharges = () => 1,
                TargetSelect = (mode) => SpellW.GetTargets(mode).FirstOrDefault()
            };
            SpellWTargetted = new Spell(CastSlot.W, SpellSlot.W)
            {
                IsTargetted = () => true,
                Range = () => 785,
                Radius = () => 15,
                Speed = () => 2200,
                Delay = () => 0.25f,
                IsEnabled = () => UseW && _lastWCast + 1 <= EngineManager.GameTime,
                MinimumCharges = () => 1,
                TargetSelect = (mode) => UnitManager.EnemyChampions.FirstOrDefault(c => c.Distance <= SpellW.Range() && (c.IsChanneling || c.BuffManager.ActiveBuffs.Any(b => b.Name == "ZhonyasRingShield" && b.Stacks >= 1)))
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                ShouldDraw = () => DrawERange,
                DrawColor = () => DrawEColor,
                AllowCollision = (target, collisions) => !collisions.Any(),
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Line,
                MinimumHitChance = () => EHitChance,
                Range = () => 740,
                Radius = () => 140,
                Speed = () => 1600,
                Delay = () => 0.15f,
                IsEnabled = () => UseE && (!OnlyEWithQ || SpellQ.SpellClass.IsSpellReady),
                TargetSelect = (mode) => SpellE.GetTargets(mode, t => t.Distance <= EMaximumRange).FirstOrDefault()
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                ShouldDraw = () => DrawRRange,
                DrawColor = () => DrawRColor,
                AllowCastOnMap = () => AllowRCastOnMinimap,
                IsTargetted = () => true,
                Range = () => 3500f,
                Delay = () => 1.375f,
                IsEnabled = () => UseR,
                IsSpellReady = (spellClass, minimumMana, minimumCharges) => spellClass.IsSpellReady && UnitManager.MyChampion.Mana >= spellClass.SpellData.ResourceCost && !UnitManager.EnemyChampions.Any(x => x.Distance < RSafeRange),
                TargetSelect = (mode) => SpellR.GetTargets(mode, t => t.Health <= GetRDamage(t)).FirstOrDefault()
            };
        }

        private float GetRDamage(GameObjectBase target)
        {
            return target != null
                ? DamageCalculator.GetArmorMod(UnitManager.MyChampion, target) *
                (75 + SpellR.SpellClass.Level * 225 +
                (UnitManager.MyChampion.UnitStats.BonusAttackDamage * 2f))
                : 0;
        }

        private void Spell_OnSpellCast(SDKSpell spell, GameObjectBase target)
        {
            if (spell.SpellSlot == SpellSlot.E)
            {
                _eTarget = target;
                SpellEQ.ExecuteCastSpell();
            }
        }

        internal override void OnCoreRender()
        {
            SpellQ.DrawRange();
            SpellW.DrawRange();
            SpellE.DrawRange();
            SpellR.DrawRange();
        }

        internal override void OnCoreMainInput()
        {
            Orbwalker.SelectedTarget = UnitManager.EnemyChampions.FirstOrDefault(
                c => TargetSelector.IsAttackable(c) && c.BuffManager.ActiveBuffs.Any(
                    b => (b.Name == "CaitlynWSnare" || b.Name == "CaitlynEMissile") && b.Stacks >= 1) &&
                    c.Distance <= 1300
                );

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


            MenuTab.AddDrawOptions(SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R);
        }
    }
}
