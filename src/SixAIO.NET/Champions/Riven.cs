using Oasys.Common.Enums.GameEnums;
using Oasys.Common.GameObject;
using Oasys.Common.GameObject.Clients.ExtendedInstances;
using Oasys.Common.GameObject.Clients.ExtendedInstances.Spells;
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
    internal sealed class Riven : Champion
    {
        internal Spell SpellQ23;
        internal Spell SpellR2;

        private static int PassiveStacks()
        {
            var buff = UnitManager.MyChampion.BuffManager.GetBuffByName("RivenPassiveAABoost", false, true);
            return buff == null
                ? 0
                : buff.IsActive && buff.Stacks > 0
                    ? (int)buff.Stacks
                    : 0;
        }

        private static int QStacks()
        {
            var buff = UnitManager.MyChampion.BuffManager.GetBuffByName("RivenTriCleave", false, true);
            return buff == null
                ? 0
                : buff.IsActive && buff.Stacks > 0
                    ? (int)buff.Stacks
                    : 0;
        }

        private static BuffEntry GetUltBuff() => UnitManager.MyChampion.BuffManager.GetBuffByName("rivenwindslashready", false, true);

        private static bool IsUltActive()
        {
            var buff = GetUltBuff();
            return buff != null && buff.IsActive;
        }

        private static float UltTimeLeft()
        {
            var buff = GetUltBuff();
            var result = buff != null && buff.IsActive ? buff.RemainingDurationMs : 0;
            //Logger.Log($"ulttime left: {result}");
            return result;
        }

        private static bool IsWindSlashReady()
        {
            var buff = GetUltBuff();
            return buff != null && buff.IsActive && buff.Stacks > 0;
        }

        private static bool AllSpellsOnCooldown()
        {
            var q = UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.Q);
            var w = UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.W);
            var e = UnitManager.MyChampion.GetSpellBook().GetSpellClass(SpellSlot.E);
            return !q.IsSpellReady && !w.IsSpellReady && !e.IsSpellReady;
        }

        private int _lastQCharge = -1;
        private float _lastQChargeTime = 0;
        private float _lastAATime = 0f;
        private float _lastQTime = 0f;

        public Riven()
        {
            SDKSpell.OnSpellCast += Spell_OnSpellCast;
            Orbwalker.OnOrbwalkerAfterBasicAttack += Orbwalker_OnOrbwalkerAfterBasicAttack;
            SpellQ = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                IsTargetted = () => true,
                Delay = () => 0f,
                IsEnabled = () => UseQ,
                IsSpellReady = (spellClass, minMana, minCharges) => spellClass.IsSpellReady,
                ShouldCast = (mode, target, spellClass, damage) =>
                            _lastAATime > _lastQTime + 0.333f &&
                            _lastAATime > _lastQChargeTime + 0.333f &&
                            target != null,
                TargetSelect = (mode) => UnitManager.EnemyChampions.FirstOrDefault(x => x.Distance <= UnitManager.MyChampion.TrueAttackRange + (IsUltActive() ? 250 : 150) && TargetSelector.IsAttackable(x))
            };
            SpellQ23 = new Spell(CastSlot.Q, SpellSlot.Q)
            {
                IsEnabled = () => UseQ,
                IsSpellReady = (spellClass, minMana, minCharges) => spellClass.IsSpellReady,
                ShouldCast = (mode, target, spellClass, damage) => GameEngine.GameTime >= _lastQChargeTime + 3.5f && _lastQCharge <= QStacks() && ((UseQ2BeforeExpire && QStacks() == 1) || (UseQ3BeforeExpire && QStacks() == 2))
            };
            SpellW = new Spell(CastSlot.W, SpellSlot.W)
            {
                IsEnabled = () => UseW,
                IsSpellReady = (spellClass, minMana, minCharges) => spellClass.IsSpellReady,
                ShouldCast = (mode, target, spellClass, damage) =>
                            UnitManager.EnemyChampions.Any(x => x.Distance <= x.BoundingRadius + (IsUltActive() ? 300 : 250) && TargetSelector.IsAttackable(x)),
            };
            SpellE = new Spell(CastSlot.E, SpellSlot.E)
            {
                IsTargetted = () => true,
                Delay = () => 0f,
                IsEnabled = () => UseE,
                IsSpellReady = (spellClass, minMana, minCharges) => spellClass.IsSpellReady,
                TargetSelect = (mode) => UnitManager.EnemyChampions.FirstOrDefault(x => x.Distance <= (IsUltActive() ? 450 : 400) && TargetSelector.IsAttackable(x))
            };
            SpellR = new Spell(CastSlot.R, SpellSlot.R)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Cone,
                MinimumHitChance = () => R2HitChance,
                Range = () => 1100f,
                Speed = () => 1600f,
                Radius = () => 200,
                IsEnabled = () => UseR2,
                IsSpellReady = (spellClass, minMana, minCharges) => spellClass.IsSpellReady && IsWindSlashReady(),
                ShouldCast = (mode, target, spellClass, damage) =>
                            target != null &&
                            (target.Health < GetRDamage(target, spellClass) ||
                            (UltTimeLeft() > 0 && UltTimeLeft() < 1000f) ||
                            (AllSpellsOnCooldown() && PassiveStacks() <= 2) ||
                            (GetMissingHealthPercent(target) < 75.0f)),
                TargetSelect = (mode) => SpellR.GetTargets(mode, x => x.Health < GetRDamage(x, SpellR.SpellClass)).FirstOrDefault()
            };
            SpellR2 = new Spell(CastSlot.R, SpellSlot.R)
            {
                PredictionMode = () => Prediction.MenuSelected.PredictionType.Cone,
                MinimumHitChance = () => Prediction.MenuSelected.HitChance.Impossible,
                Range = () => 5000,
                Speed = () => 1600f,
                Radius = () => 200,
                IsEnabled = () => UseR2,
                IsSpellReady = (spellClass, minMana, minCharges) => spellClass.IsSpellReady && IsWindSlashReady(),
                ShouldCast = (mode, target, spellClass, damage) => target != null && UltTimeLeft() < 1000f,
                TargetSelect = (mode) => SpellR2.GetTargets(mode).FirstOrDefault()
            };
        }

        private float GetMissingHealthPercent(GameObjectBase target)
        {
            var missingHealthPercent = 100f - (target.Health / target.MaxHealth * 100f);
            return missingHealthPercent;
        }

        private float GetRDamage(GameObjectBase target, SpellClass spellClass)
        {
            if (target == null)
            {
                return 0;
            }
            var extraDamagePercent = GetMissingHealthPercent(target) * 2.667f;
            if (extraDamagePercent > 200f)
            {
                extraDamagePercent = 200f;
            }
            return DamageCalculator.GetArmorMod(UnitManager.MyChampion, target) * ((1 + (extraDamagePercent / 100f)) *
                   ((UnitManager.MyChampion.UnitStats.BonusAttackDamage * 0.60f) + 50 + 50 * spellClass.Level));
        }

        private void Spell_OnSpellCast(SDKSpell spell, GameObjectBase target)
        {
            if (spell.SpellSlot == SpellSlot.Q)
            {
                _lastQTime = GameEngine.GameTime;
            }

            if (spell.SpellSlot == SpellSlot.E)
            {
                SpellW.ExecuteCastSpell();
                SpellQ.ExecuteCastSpell();
                SpellR.ExecuteCastSpell();
            }

            if (spell.SpellSlot == SpellSlot.W)
            {
                SpellQ.ExecuteCastSpell();
                SpellR.ExecuteCastSpell();
            }

            if (spell.SpellSlot == SpellSlot.Q)
            {
                SpellR.ExecuteCastSpell();
                SpellW.ExecuteCastSpell();
            }
        }

        private void Orbwalker_OnOrbwalkerAfterBasicAttack(float gameTime, GameObjectBase target)
        {
            _lastAATime = gameTime;
            if (target != null)
            {
                SpellQ.ExecuteCastSpell();
            }
        }

        internal override void OnCoreMainInput()
        {
            if (SpellR.ExecuteCastSpell() || SpellQ.ExecuteCastSpell() || SpellW.ExecuteCastSpell() || SpellE.ExecuteCastSpell() || SpellR2.ExecuteCastSpell() || SpellQ23.ExecuteCastSpell())
            {
                return;
            }
        }

        internal override void OnCoreMainTick()
        {
            if (_lastQCharge != QStacks())
            {
                _lastQCharge = QStacks();
                _lastQChargeTime = GameEngine.GameTime;
            }

            //Logger.Log($"{_lastQCharge} - {_lastQChargeTime}");
        }

        internal bool UseQ2BeforeExpire
        {
            get => QSettings.GetItem<Switch>("Use Q2 Before Expire").IsOn;
            set => QSettings.GetItem<Switch>("Use Q2 Before Expire").IsOn = value;
        }

        internal bool UseQ3BeforeExpire
        {
            get => QSettings.GetItem<Switch>("Use Q3 Before Expire").IsOn;
            set => QSettings.GetItem<Switch>("Use Q3 Before Expire").IsOn = value;
        }

        internal bool UseR2
        {
            get => RSettings.GetItem<Switch>("Use R2").IsOn;
            set => RSettings.GetItem<Switch>("Use R2").IsOn = value;
        }

        internal Prediction.MenuSelected.HitChance R2HitChance
        {
            get => (Prediction.MenuSelected.HitChance)Enum.Parse(typeof(Prediction.MenuSelected.HitChance), RSettings.GetItem<ModeDisplay>("R2 HitChance").SelectedModeName);
            set => RSettings.GetItem<ModeDisplay>("R2 HitChance").SelectedModeName = value.ToString();
        }

        internal override void InitializeMenu()
        {
            MenuManager.AddTab(new Tab($"SIXAIO - {nameof(Riven)}"));

            MenuTab.AddGroup(new Group("Q Settings"));
            MenuTab.AddGroup(new Group("W Settings"));
            MenuTab.AddGroup(new Group("E Settings"));
            MenuTab.AddGroup(new Group("R Settings"));

            QSettings.AddItem(new Switch() { Title = "Use Q", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Use Q2 Before Expire", IsOn = true });
            QSettings.AddItem(new Switch() { Title = "Use Q3 Before Expire", IsOn = true });

            WSettings.AddItem(new Switch() { Title = "Use W", IsOn = true });

            ESettings.AddItem(new Switch() { Title = "Use E", IsOn = true });

            RSettings.AddItem(new Switch() { Title = "Use R2", IsOn = true });
            RSettings.AddItem(new ModeDisplay() { Title = "R2 HitChance", ModeNames = Enum.GetNames(typeof(Prediction.MenuSelected.HitChance)).ToList(), SelectedModeName = "High" });
        }
    }
}